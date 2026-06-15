using HotChocolate;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Configurations;
using Microsoft.Extensions.DependencyInjection;

namespace Dryv.HotChocolate;

/// <summary>
/// A HotChocolate type interceptor that automatically injects Dryv validation middleware
/// into any field whose arguments are decorated with <see cref="DryvValidationAttribute"/>.
/// </summary>
internal sealed class DryvValidationTypeInterceptor : TypeInterceptor
{
    private static readonly FieldMiddleware Middleware = next => context => ExecuteValidationAsync(next, context);

    public override void OnBeforeCompleteType(ITypeCompletionContext completionContext, TypeSystemConfiguration configuration)
    {
        if (configuration is not ObjectTypeConfiguration objectTypeDef)
        {
            return;
        }

        foreach (var field in objectTypeDef.Fields)
        {
            if (HasDryvValidatableArgument(field))
            {
                field.MiddlewareConfigurations.Insert(0, new FieldMiddlewareConfiguration(Middleware, isRepeatable: false, key: "Dryv.Validation"));
            }
        }
    }

    private static async ValueTask ExecuteValidationAsync(FieldDelegate next, IMiddlewareContext context)
    {
        var validator = context.Services.GetRequiredService<DryvValidator>();
        var hasErrors = false;

        foreach (var argument in context.Selection.Field.Arguments)
        {
            var runtimeType = argument.RuntimeType;

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (runtimeType is null)
            {
                continue;
            }

            if (!runtimeType.GetCustomAttributes(typeof(DryvValidationAttribute), true).Any())
            {
                continue;
            }

            var value = context.ArgumentValue<object>(argument.Name);
            
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (value is null)
            {
                continue;
            }

            var results = await validator.Validate(value, context.Services.GetService);

            foreach (var (path, result) in results)
            {
                if (result.Type != DryvResultType.Error)
                {
                    continue;
                }

                hasErrors = true;
                context.ReportError(
                    ErrorBuilder.New()
                        .SetMessage(result.Text)
                        .SetCode("DRYV_VALIDATION_ERROR")
                        .SetPath(context.Path)
                        .SetExtension("field", path)
                        .SetExtension("resultType", result.Type.ToString())
                        .Build());
            }
        }

        if (hasErrors)
        {
            context.Result = null;
            return;
        }

        await next(context);
    }

    private static bool HasDryvValidatableArgument(ObjectFieldConfiguration field)
    {
        return field.HasArguments && field.Arguments.Any(arg =>
        {
            var type = arg.RuntimeType;
            return type is not null &&
                   type.GetCustomAttributes(typeof(DryvValidationAttribute), true).Length > 0;
        });
    }
}
