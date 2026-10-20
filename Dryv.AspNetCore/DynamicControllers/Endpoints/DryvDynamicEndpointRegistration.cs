using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Dryv.AspNetCore.Internal;
using Dryv.Configuration;
using Dryv.Rules;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dryv.AspNetCore.DynamicControllers.Endpoints
{
    internal class DryvDynamicEndpointRegistration(
        DryvDynamicEndpointDataSource dataSource,
        IOptions<DryvDynamicControllerOptions> options)
    {
        public string Register(Expression expression, Type modelType, string action, DryvRuleDefinition rule)
        {
            var parameters = DryvExpressionHelper.FindParameters(expression);

            var lambda = expression is LambdaExpression l
                ? Expression.Lambda(l.Body, parameters)
                : Expression.Lambda(expression, parameters);

            var compiledDelegate = lambda.Compile();

            var endpointName = "DryvEndpoint" + Md5Helper.CreateMd5(ExpressionStructuralHasher.GetStructuralHash(expression));
            var context = new DryvControllerGenerationContext(endpointName, action, rule);

            var route = options.Value.GetRoute != null
                ? options.Value.GetRoute(context)
                : CustomizationDefaults.DefaultRoute(context);

            var httpMethod = options.Value.GetHttpMethod != null
                ? options.Value.GetHttpMethod(context)
                : DryvDynamicControllerMethods.Post;

            var httpMethodString = httpMethod.ToString().ToUpper();

            var modelParameter = parameters.Find(p => p.Type == modelType);

            RequestDelegate handler;

            if (httpMethod == DryvDynamicControllerMethods.Post)
            {
                handler = CreatePostHandler(compiledDelegate, modelType, parameters);
            }
            else
            {
                handler = CreateGetHandler(compiledDelegate, expression, modelType, parameters);
            }

            if (options.Value.HandlerWrapper != null)
            {
                handler = options.Value.HandlerWrapper(context, handler);
            }

            Action<Microsoft.AspNetCore.Routing.RouteEndpointBuilder> configure = null;
            if (options.Value.EndpointConfiguration != null)
            {
                var ctx = context;
                configure = builder => options.Value.EndpointConfiguration(ctx, builder);
            }

            dataSource.AddEndpoint(route, handler, httpMethodString, configure);

            return route.StartsWith("/") ? route : "/" + route;
        }

        private static RequestDelegate CreatePostHandler(
            Delegate compiledDelegate,
            Type modelType,
            List<ParameterExpression> allParams)
        {
            return async ctx =>
            {
                object model;
                try
                {
                    model = await ctx.Request.ReadFromJsonAsync(modelType);
                }
                catch (Exception ex)
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json; charset=utf-8";
                    var dryvOpts = ctx.RequestServices.GetRequiredService<DryvOptions>();
                    var errorJson = dryvOpts.JsonConversion(new { text = ex.Message, type = "error" });
                    await ctx.Response.WriteAsync(errorJson, Encoding.UTF8);
                    return;
                }

                var args = BuildArgs(allParams, modelType, model, ctx);
                await InvokeAndWriteResult(compiledDelegate, args, ctx);
            };
        }

        private static RequestDelegate CreateGetHandler(
            Delegate compiledDelegate,
            Expression expression,
            Type modelType,
            List<ParameterExpression> allParams)
        {
            var finder = new DryvExpressionHelper.MemberFinder(modelType);
            finder.Visit(expression);
            var properties = finder.FoundMemberExpressions
                .Select(e => e.Member)
                .Distinct()
                .OfType<PropertyInfo>()
                .ToList();

            return async ctx =>
            {
                var model = Activator.CreateInstance(modelType);

                foreach (var prop in properties)
                {
                    if (ctx.Request.Query.TryGetValue(prop.Name, out var val))
                    {
                        var converted = Convert.ChangeType(val.ToString(), prop.PropertyType);
                        prop.SetValue(model, converted);
                    }
                }

                var args = BuildArgs(allParams, modelType, model, ctx);
                await InvokeAndWriteResult(compiledDelegate, args, ctx);
            };
        }

        private static object[] BuildArgs(
            List<ParameterExpression> allParams,
            Type modelType,
            object model,
            HttpContext ctx)
        {
            var args = new object[allParams.Count];

            for (var i = 0; i < allParams.Count; i++)
            {
                args[i] = allParams[i].Type == modelType
                    ? model
                    : ctx.RequestServices.GetRequiredService(allParams[i].Type);
            }

            return args;
        }

        private static async Task InvokeAndWriteResult(Delegate compiledDelegate, object[] args, HttpContext ctx)
        {
            var result = compiledDelegate.DynamicInvoke(args);

            if (result is Task task)
            {
                await task;
                var taskType = task.GetType();
                result = taskType.IsGenericType
                    ? taskType.GetProperty("Result")?.GetValue(task)
                    : null;
            }

            var dryvOptions = ctx.RequestServices.GetRequiredService<DryvOptions>();
            var json = dryvOptions.JsonConversion(result);
            ctx.Response.ContentType = "application/json; charset=utf-8";
            await ctx.Response.WriteAsync(json, Encoding.UTF8);
        }
    }
}
