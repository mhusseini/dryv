using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Dryv.AspNetCore.DynamicControllers.Endpoints;
using Dryv.Translation;
using Dryv.Translation.Visitors;
using Microsoft.Extensions.Options;

namespace Dryv.AspNetCore.DynamicControllers.Translation
{
    internal abstract class DryvDynamicControllerTranslatorBase
    {
        private static readonly ConcurrentDictionary<string, Lazy<string>> EndpointUrls = new();
        private readonly IDryvClientServerCallWriter controllerCallWriter;
        private readonly DryvDynamicEndpointRegistration endpointRegistration;
        private readonly IOptions<DryvDynamicControllerOptions> options;

        protected DryvDynamicControllerTranslatorBase(DryvDynamicEndpointRegistration endpointRegistration, IDryvClientServerCallWriter controllerCallWriter, IOptions<DryvDynamicControllerOptions> options)
        {
            this.endpointRegistration = endpointRegistration;
            this.controllerCallWriter = controllerCallWriter;
            this.options = options;
        }

        protected void TranslateToServerCall(TranslationContext context, ITranslator translator, Expression expression, string action)
        {
            context.IsAsync = true;

            if (context.WhatIfMode)
            {
                return;
            }

            var key = Internal.ExpressionStructuralHasher.GetStructuralHash(expression);
            var url = EndpointUrls.GetOrAdd(key, _ => new Lazy<string>(() =>
                this.endpointRegistration.Register(expression, context.ModelType, action, context.Rule)
            )).Value;

            var endpointName = "DryvEndpoint" + Internal.Md5Helper.CreateMd5(key);
            var httpMethod = this.options.Value.GetHttpMethod(new DryvControllerGenerationContext(endpointName, action, context.Rule)).ToString().ToUpper();
            var modelProperties = FindModelPropertiesInExpression(context, expression);

            if (modelProperties.Count > 0)
            {
                this.controllerCallWriter.Write(context, translator, url, httpMethod, modelProperties);
            }
            else
            {
                var modelParameter = ExpressionNodeFinder<ParameterExpression>.FindChildrenStatic(expression)
                    .FirstOrDefault(p => p.Type == context.ModelType);

                if (modelParameter != null)
                {
                    this.controllerCallWriter.Write(context, translator, url, httpMethod, modelParameter);
                }
                else
                {
                    this.controllerCallWriter.Write(context, translator, url, httpMethod);
                }
            }

        }

        private static List<MemberExpression> FindModelPropertiesInExpression(TranslationContext context, Expression expression)
        {
            var f = new ExpressionNodeFinder<MemberExpression>();

            f.Visit(expression);

            return f.FoundChildren
                .Where(e => e.Member is PropertyInfo)
                .Where(e => ExpressionNodeFinder<ParameterExpression>.FindChildrenStatic(e).Any(p => p.Type == context.ModelType))
                .Distinct(MemberExpressionComparer.Default)
                .ToList();
        }

    }
}