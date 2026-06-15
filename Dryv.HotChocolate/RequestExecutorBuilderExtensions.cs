using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dryv.HotChocolate;

/// <summary>
/// Extension methods for integrating Dryv validation into HotChocolate.
/// </summary>
public static class RequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds automatic Dryv validation to the HotChocolate execution pipeline.
    /// Any mutation or query argument decorated with <see cref="DryvValidationAttribute"/>
    /// will be validated before the resolver executes. Validation errors are returned
    /// as GraphQL errors with code "DRYV_VALIDATION_ERROR".
    /// </summary>
    /// <param name="builder">The HotChocolate request executor builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IRequestExecutorBuilder AddDryvValidation(this IRequestExecutorBuilder builder)
    {
        return builder.TryAddTypeInterceptor(typeof(DryvValidationTypeInterceptor));
    }
}
