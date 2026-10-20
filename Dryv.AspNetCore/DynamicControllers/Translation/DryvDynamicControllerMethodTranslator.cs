using System;
using System.Linq;
using System.Threading.Tasks;
using Dryv.AspNetCore.DynamicControllers.Endpoints;
using Dryv.Translation;
using Microsoft.Extensions.Options;

namespace Dryv.AspNetCore.DynamicControllers.Translation
{
    internal class DryvDynamicControllerMethodTranslator : DryvDynamicControllerTranslatorBase, IDryvMethodCallTranslator
    {

        public bool SupportsType(Type type)
        {
            return true;
        }
        public DryvDynamicControllerMethodTranslator(DryvDynamicEndpointRegistration endpointRegistration, IDryvClientServerCallWriter controllerCallWriter, IOptions<DryvDynamicControllerOptions> options)
            : base(endpointRegistration, controllerCallWriter, options) { }

        public bool Translate(MethodTranslationContext context)
        {
            context.IsAsync = true;

            if (context.WhatIfMode)
            {
                return true;
            }

            var methodCallExpression = context.Expression;
            var isAsync = typeof(Task).IsAssignableFrom(methodCallExpression.Method.DeclaringType);

            if (isAsync && methodCallExpression.Method.Name == nameof(Task.FromResult))
            {
                context.Translator.Translate(methodCallExpression.Arguments.First(), context);
            }
            else
            {
                var action = methodCallExpression.Method.IsHideBySig
                    ? string.IsNullOrWhiteSpace(context.Rule.Name) ? "auto" : context.Rule.Name 
                    : methodCallExpression.Method.Name;
                this.TranslateToServerCall(context, context.Translator, methodCallExpression, action);
            }

            return true;
        }
    }
}