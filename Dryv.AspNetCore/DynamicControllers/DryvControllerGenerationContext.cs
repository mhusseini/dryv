using Dryv.Rules;

namespace Dryv.AspNetCore.DynamicControllers
{
    public class DryvControllerGenerationContext
    {
        internal DryvControllerGenerationContext(string endpointName, string action, DryvRuleDefinition rule)
        {
            this.ControllerFullName = endpointName;
            this.Controller = endpointName;
            this.Action = action;
            this.Rule = rule;
        }

        public DryvControllerGenerationContext()
        {
        }

        /// <summary>
        /// The name of the generated action.
        /// </summary>
        public string Action { get; set; }

        public DryvRuleDefinition Rule { get; set; }

        /// <summary>
        /// The short name of the generated controller.
        /// </summary>
        public string Controller { get; set; }

        /// <summary>
        /// The full name of the generated controller.
        /// </summary>
        public string ControllerFullName { get; set; }
    }
}