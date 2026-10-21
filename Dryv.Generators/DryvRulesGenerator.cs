using System.Text;
using Microsoft.CodeAnalysis;

namespace Dryv.Generators
{
    [Generator]
    public class DryvRulesGenerator : IIncrementalGenerator
    {
        private const int MaxOptionCount = 6; // 0..5
        private const int MaxPropertyCount = 6; // 1..5

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(ctx =>
            {
                ctx.AddSource("DryvRules.g.cs", GenerateRuleMethods());
                ctx.AddSource("DryvRules.server.g.cs", GenerateServerMethods());
                ctx.AddSource("DryvRules.parameters.g.cs", GenerateParameterMethods());
                ctx.AddSource("DryvRuleBuilder.g.cs", GenerateBuilderMethods());
            });
        }

        // ──────────────────────────────────────────────────
        //  DryvRules.g.cs — Rule / ClientRule / DisableRules
        // ──────────────────────────────────────────────────

        private static string GenerateRuleMethods()
        {
            var sb = new StringBuilder();
            AppendFileHeader(sb);
            sb.AppendLine("    partial class DryvRules<TModel>");
            sb.AppendLine("    {");

            var prefixes = new[] { "", "Client" };
            foreach (var prefix in prefixes)
            {
                for (int o = 0; o < MaxOptionCount; o++)
                {
                    for (int p = 1; p < MaxPropertyCount; p++)
                    {
                        WriteRuleOverloads(sb, p, o, prefix);
                    }
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static void WriteRuleOverloads(StringBuilder sb, int propCount, int optionCount, string methodPrefix)
        {
            var gen = GenericParams(optionCount);
            var optPfx = OptionsPrefix(optionCount);
            var typeOfArgs = TypeOfArgs(optionCount);
            var propParams = PropertyParams(propCount);
            var propArray = PropertyArray(propCount);
            var hasOptions = optionCount > 0;

            var addMethod = string.IsNullOrEmpty(methodPrefix) ? "Add" : "Add" + methodPrefix;
            var ruleMethodName = string.IsNullOrEmpty(methodPrefix) ? "Rule" : methodPrefix + "Rule";

            // ── DisableRules (only for default prefix) ──
            if (string.IsNullOrEmpty(methodPrefix))
            {
                if (!hasOptions)
                {
                    // Single overload: no ruleSwitch
                    sb.AppendLine($"        /// <summary>Disables all validation rules for the specified properties when the <paramref name=\"rule\"/> expression returns <c>true</c>.</summary>");
                    sb.AppendLine($"        /// <param name=\"rule\">An expression that returns <c>true</c> to disable rules.</param>");
                    sb.AppendLine($"        /// <returns>This rules instance for further chaining.</returns>");
                    sb.AppendLine($"        public DryvRules<TModel> DisableRules(");
                    sb.Append(propParams);
                    sb.AppendLine($"            Expression<Func<TModel, bool>> rule)");
                    sb.AppendLine( "        {");
                    sb.AppendLine($"            this.Disable(rule, new[] {{ {propArray}}}, null);");
                    sb.AppendLine( "            return this;");
                    sb.AppendLine( "        }");
                    sb.AppendLine();
                }
                else
                {
                    var syncSwitch = $"Func<{optPfx}bool>";
                    var asyncSwitch = $"Func<{optPfx}Task<bool>>";
                    var ruleType = $"Expression<Func<TModel, {optPfx}bool>>";

                    // With sync ruleSwitch
                    sb.AppendLine($"        /// <summary>Disables all validation rules for the specified properties when the <paramref name=\"rule\"/> expression returns <c>true</c>.</summary>");
                    sb.AppendLine($"        /// <returns>This rules instance for further chaining.</returns>");
                    sb.AppendLine($"        public DryvRules<TModel> DisableRules{gen}(");
                    sb.Append(propParams);
                    sb.AppendLine($"            {ruleType} rule,");
                    sb.AppendLine($"            {syncSwitch} ruleSwitch)");
                    sb.AppendLine( "        {");
                    sb.AppendLine($"            this.Disable(rule, new[] {{ {propArray}}}, ruleSwitch{typeOfArgs});");
                    sb.AppendLine( "            return this;");
                    sb.AppendLine( "        }");
                    sb.AppendLine();

                    // Without ruleSwitch
                    sb.AppendLine($"        /// <summary>Disables all validation rules for the specified properties when the <paramref name=\"rule\"/> expression returns <c>true</c>.</summary>");
                    sb.AppendLine($"        /// <returns>This rules instance for further chaining.</returns>");
                    sb.AppendLine($"        public DryvRules<TModel> DisableRules{gen}(");
                    sb.Append(propParams);
                    sb.AppendLine($"            {ruleType} rule)");
                    sb.AppendLine( "        {");
                    sb.AppendLine($"            this.Disable(rule, new[] {{ {propArray}}}, null{typeOfArgs});");
                    sb.AppendLine( "            return this;");
                    sb.AppendLine( "        }");
                    sb.AppendLine();

                    // With async ruleSwitch
                    sb.AppendLine($"        /// <summary>Disables all validation rules for the specified properties when the <paramref name=\"rule\"/> expression returns <c>true</c>.</summary>");
                    sb.AppendLine($"        /// <returns>This rules instance for further chaining.</returns>");
                    sb.AppendLine($"        public DryvRules<TModel> DisableRules{gen}(");
                    sb.Append(propParams);
                    sb.AppendLine($"            {ruleType} rule,");
                    sb.AppendLine($"            {asyncSwitch} ruleSwitch)");
                    sb.AppendLine( "        {");
                    sb.AppendLine($"            this.Disable(rule, new[] {{ {propArray}}}, ruleSwitch{typeOfArgs});");
                    sb.AppendLine( "            return this;");
                    sb.AppendLine( "        }");
                    sb.AppendLine();
                }
            }

            // ── Rule / ClientRule ──
            if (!hasOptions)
            {
                // Sync rule, no ruleSwitch
                WriteExpressionRuleMethod(sb, ruleMethodName, "", addMethod,
                    propParams, propArray, optPfx, typeOfArgs,
                    $"Expression<Func<TModel, DryvValidationResult>>", "null");

                // Async rule, no ruleSwitch
                WriteExpressionRuleMethod(sb, ruleMethodName, "", addMethod,
                    propParams, propArray, optPfx, typeOfArgs,
                    $"Expression<Func<TModel, Task<DryvValidationResult>>>", "null");
            }
            else
            {
                var syncSwitch = $"Func<{optPfx}bool>";
                var asyncSwitch = $"Func<{optPfx}Task<bool>>";
                var syncRule = $"Expression<Func<TModel, {optPfx}DryvValidationResult>>";
                var asyncRule = $"Expression<Func<TModel, {optPfx}Task<DryvValidationResult>>>";

                // 1. Sync rule + sync ruleSwitch
                WriteExpressionRuleMethod(sb, ruleMethodName, gen, addMethod,
                    propParams, propArray, optPfx, typeOfArgs,
                    syncRule, "ruleSwitch", syncSwitch);

                // 2. Async rule + sync ruleSwitch
                WriteExpressionRuleMethod(sb, ruleMethodName, gen, addMethod,
                    propParams, propArray, optPfx, typeOfArgs,
                    asyncRule, "ruleSwitch", syncSwitch);

                // 3. Async rule, no switch
                WriteExpressionRuleMethod(sb, ruleMethodName, gen, addMethod,
                    propParams, propArray, optPfx, typeOfArgs,
                    asyncRule, "null");

                // 4. Sync rule, no switch
                WriteExpressionRuleMethod(sb, ruleMethodName, gen, addMethod,
                    propParams, propArray, optPfx, typeOfArgs,
                    syncRule, "null");

                // 5. Sync rule + async ruleSwitch
                WriteExpressionRuleMethod(sb, ruleMethodName, gen, addMethod,
                    propParams, propArray, optPfx, typeOfArgs,
                    syncRule, "ruleSwitch", asyncSwitch);

                // 6. Async rule + async ruleSwitch
                WriteExpressionRuleMethod(sb, ruleMethodName, gen, addMethod,
                    propParams, propArray, optPfx, typeOfArgs,
                    asyncRule, "ruleSwitch", asyncSwitch);
            }
        }

        private static void WriteExpressionRuleMethod(
            StringBuilder sb,
            string methodName,
            string genericParams,
            string addMethod,
            string propParams,
            string propArray,
            string optionsPrefix,
            string typeOfArgs,
            string ruleType,
            string switchArg,
            string switchType = null)
        {
            var isClient = methodName.StartsWith("Client");
            var desc = isClient ? "client-only " : "";
            sb.AppendLine($"        /// <summary>Registers a {desc}validation rule expression for the selected properties.</summary>");
            sb.AppendLine($"        /// <param name=\"rule\">The validation expression that returns a <see cref=\"DryvValidationResult\"/>.</param>");
            sb.AppendLine($"        /// <returns>This rules instance for further chaining.</returns>");
            sb.AppendLine($"        public DryvRules<TModel> {methodName}{genericParams}(");
            sb.Append(propParams);
            if (switchType != null)
            {
                sb.AppendLine($"            {ruleType} rule,");
                sb.AppendLine($"            {switchType} ruleSwitch,");
            }
            else
            {
                sb.AppendLine($"            {ruleType} rule,");
            }
            sb.AppendLine( "            DryvRuleSettings settings = null)");
            sb.AppendLine( "        {");
            sb.AppendLine($"            this.{addMethod}(rule, new[] {{ {propArray}}}, {switchArg}, settings{typeOfArgs});");
            sb.AppendLine( "            return this;");
            sb.AppendLine( "        }");
            sb.AppendLine();
        }

        // ──────────────────────────────────────
        //  DryvRules.server.g.cs — ServerRule
        // ──────────────────────────────────────

        private static string GenerateServerMethods()
        {
            var sb = new StringBuilder();
            AppendFileHeader(sb);
            sb.AppendLine("    partial class DryvRules<TModel>");
            sb.AppendLine("    {");

            for (int o = 0; o < MaxOptionCount; o++)
            {
                for (int p = 1; p < MaxPropertyCount; p++)
                {
                    WriteServerOverloads(sb, p, o);
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static void WriteServerOverloads(StringBuilder sb, int propCount, int optionCount)
        {
            var gen = GenericParams(optionCount);
            var optPfx = OptionsPrefix(optionCount);
            var typeOfArgs = TypeOfArgs(optionCount);
            var propParams = PropertyParams(propCount);
            var propArray = PropertyArray(propCount);
            var hasOptions = optionCount > 0;

            if (!hasOptions)
            {
                // Sync rule, no ruleSwitch
                WriteServerRuleMethod(sb, gen, propParams, propArray, typeOfArgs,
                    $"Func<TModel, DryvValidationResult>", null);

                // Async rule, no ruleSwitch
                WriteServerRuleMethod(sb, gen, propParams, propArray, typeOfArgs,
                    $"Func<TModel, Task<DryvValidationResult>>", null);
            }
            else
            {
                var switchType = $"Func<{optPfx}bool>";

                // Sync rule + optional ruleSwitch
                WriteServerRuleMethod(sb, gen, propParams, propArray, typeOfArgs,
                    $"Func<TModel, {optPfx}DryvValidationResult>", switchType);

                // Async rule + optional ruleSwitch
                WriteServerRuleMethod(sb, gen, propParams, propArray, typeOfArgs,
                    $"Func<TModel, {optPfx}Task<DryvValidationResult>>", switchType);
            }
        }

        private static void WriteServerRuleMethod(
            StringBuilder sb,
            string genericParams,
            string propParams,
            string propArray,
            string typeOfArgs,
            string ruleType,
            string switchType)
        {
            sb.AppendLine($"        /// <summary>Registers a server-only validation rule using a <c>Func</c> delegate for the selected properties. Server rules are never translated to JavaScript.</summary>");
            sb.AppendLine($"        /// <param name=\"rule\">The server-side validation delegate.</param>");
            sb.AppendLine($"        /// <returns>This rules instance for further chaining.</returns>");
            sb.AppendLine($"        public DryvRules<TModel> ServerRule{genericParams}(");
            sb.Append(propParams);
            if (switchType != null)
            {
                sb.AppendLine($"            {ruleType} rule,");
                sb.AppendLine($"            {switchType} ruleSwitch = null,");
            }
            else
            {
                sb.AppendLine($"            {ruleType} rule,");
            }
            sb.AppendLine( "            DryvRuleSettings settings = null)");
            sb.AppendLine( "        {");
            var switchArg = switchType != null ? "ruleSwitch" : "null";
            sb.AppendLine($"            this.AddServer(rule, new[] {{ {propArray}}}, {switchArg}, settings{typeOfArgs});");
            sb.AppendLine( "            return this;");
            sb.AppendLine( "        }");
            sb.AppendLine();
        }

        // ──────────────────────────────────────────
        //  DryvRules.parameters.g.cs — Parameter
        // ──────────────────────────────────────────

        private static string GenerateParameterMethods()
        {
            var sb = new StringBuilder();
            AppendFileHeader(sb);
            sb.AppendLine("    partial class DryvRules<TModel>");
            sb.AppendLine("    {");

            for (int o = 0; o < MaxOptionCount; o++)
            {
                WriteParameterOverloads(sb, o);
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static void WriteParameterOverloads(StringBuilder sb, int optionCount)
        {
            var optPfx = OptionsPrefix(optionCount);
            var typeOfArgs = TypeOfArgs(optionCount);

            // Sync
            sb.AppendLine($"        /// <summary>Registers a named parameter whose value is computed at runtime and made available to client-side rules via <c>$ctx.parameter(name)</c>.</summary>");
            sb.AppendLine($"        /// <param name=\"name\">The parameter name.</param>");
            sb.AppendLine($"        /// <param name=\"parameter\">A factory that produces the parameter value.</param>");
            sb.AppendLine($"        /// <returns>This rules instance for further chaining.</returns>");
            sb.AppendLine($"        public DryvRules<TModel> Parameter<{optPfx}TResult>(string name, Func<{optPfx}TResult> parameter)");
            sb.AppendLine( "        {");
            sb.AppendLine($"            this.AddParameter(name, parameter{typeOfArgs});");
            sb.AppendLine( "            return this;");
            sb.AppendLine( "        }");
            sb.AppendLine();

            // Async
            sb.AppendLine($"        /// <summary>Registers a named parameter whose value is computed asynchronously at runtime and made available to client-side rules via <c>$ctx.parameter(name)</c>.</summary>");
            sb.AppendLine($"        /// <param name=\"name\">The parameter name.</param>");
            sb.AppendLine($"        /// <param name=\"parameter\">An async factory that produces the parameter value.</param>");
            sb.AppendLine($"        /// <returns>This rules instance for further chaining.</returns>");
            sb.AppendLine($"        public DryvRules<TModel> Parameter<{optPfx}TResult>(string name, Func<{optPfx}Task<TResult>> parameter)");
            sb.AppendLine( "        {");
            sb.AppendLine($"            this.AddParameter(name, parameter{typeOfArgs});");
            sb.AppendLine( "            return this;");
            sb.AppendLine( "        }");
            sb.AppendLine();
        }

        // ──────────────────────────────────────────────────────────
        //  DryvRuleBuilder.g.cs — Rule entry points, Validate, Disable
        // ──────────────────────────────────────────────────────────────

        private static string GenerateBuilderMethods()
        {
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Linq.Expressions;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine();
            sb.AppendLine("namespace Dryv");
            sb.AppendLine("{");

            // ── Rule entry points on DryvRules<TModel> ──
            sb.AppendLine("    partial class DryvRules<TModel>");
            sb.AppendLine("    {");
            for (int p = 1; p < MaxPropertyCount; p++)
            {
                WriteBuilderEntryPoint(sb, p);
            }

            // ── ServerRule entry points on DryvRules<TModel> ──
            for (int p = 1; p < MaxPropertyCount; p++)
            {
                WriteServerBuilderEntryPoint(sb, p);
            }
            sb.AppendLine("    }");
            sb.AppendLine();

            // ── DryvRuleBuilder<TModel> generated members ──
            sb.AppendLine("    partial class DryvRuleBuilder<TModel>");
            sb.AppendLine("    {");

            // Validate, ServerValidate, and Disable methods
            for (int o = 0; o < MaxOptionCount; o++)
            {
                WriteBuilderValidateMethod(sb, o, sync: true);
                WriteBuilderValidateMethod(sb, o, sync: false);
                WriteBuilderServerValidateMethod(sb, o, sync: true);
                WriteBuilderServerValidateMethod(sb, o, sync: false);
                WriteBuilderDisableMethod(sb, o);
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static void WriteBuilderEntryPoint(StringBuilder sb, int propCount)
        {
            sb.AppendLine($"        /// <summary>Selects {(propCount == 1 ? "a property" : propCount + " properties")} and configures one or more validation rules via the <paramref name=\"configure\"/> callback.</summary>");
            for (int k = 1; k <= propCount; k++)
                sb.AppendLine($"        /// <param name=\"property{k}\">A property selector expression.</param>");
            sb.AppendLine($"        /// <param name=\"configure\">A callback that receives a <see cref=\"DryvRuleBuilder{{TModel}}\"/> for fluent rule configuration.</param>");
            sb.AppendLine($"        /// <returns>This rules instance for further chaining.</returns>");
            sb.AppendLine($"        public DryvRules<TModel> Rule(");
            for (int j = 1; j <= propCount; j++)
            {
                sb.AppendLine($"            Expression<Func<TModel, object>> property{j},");
            }
            sb.AppendLine($"            Action<DryvRuleBuilder<TModel>> configure)");
            sb.AppendLine("        {");
            sb.Append("            var builder = new DryvRuleBuilder<TModel>(this, ");
            for (int j = 1; j <= propCount; j++)
            {
                if (j > 1) sb.Append(", ");
                sb.Append($"property{j}");
            }
            sb.AppendLine(");");
            sb.AppendLine("            configure(builder);");
            sb.AppendLine("            return this;");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        private static void WriteBuilderValidateMethod(StringBuilder sb, int optionCount, bool sync)
        {
            var gen = GenericParams(optionCount);
            var optPfx = OptionsPrefix(optionCount);
            var typeOfArgs = TypeOfArgs(optionCount);
            var returnType = sync ? "DryvValidationResult" : "Task<DryvValidationResult>";
            var asyncNote = sync ? "" : " asynchronous";
            var diNote = optionCount > 0 ? $" Injected service types are resolved from the DI container at validation time." : "";

            sb.AppendLine($"        /// <summary>Registers an{asyncNote} expression-based validation rule for the selected properties.{diNote} The expression is translatable to JavaScript for client-side validation.</summary>");
            sb.AppendLine($"        /// <param name=\"rule\">The validation expression.</param>");
            sb.AppendLine($"        /// <returns>This builder instance for further chaining.</returns>");
            sb.AppendLine($"        public DryvRuleBuilder<TModel> Validate{gen}(");
            sb.AppendLine($"            Expression<Func<TModel, {optPfx}{returnType}>> rule)");
            sb.AppendLine("        {");
            sb.AppendLine($"            this.AddRule(rule{typeOfArgs});");
            sb.AppendLine("            return this;");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        private static void WriteServerBuilderEntryPoint(StringBuilder sb, int propCount)
        {
            sb.AppendLine($"        /// <summary>Selects {(propCount == 1 ? "a property" : propCount + " properties")} and configures one or more server-only validation rules via the <paramref name=\"configure\"/> callback. Rules registered here are never translated to JavaScript.</summary>");
            for (int k = 1; k <= propCount; k++)
                sb.AppendLine($"        /// <param name=\"property{k}\">A property selector expression.</param>");
            sb.AppendLine($"        /// <param name=\"configure\">A callback that receives a <see cref=\"DryvRuleBuilder{{TModel}}\"/> for fluent rule configuration.</param>");
            sb.AppendLine($"        /// <returns>This rules instance for further chaining.</returns>");
            sb.AppendLine($"        public DryvRules<TModel> ServerRule(");
            for (int j = 1; j <= propCount; j++)
            {
                sb.AppendLine($"            Expression<Func<TModel, object>> property{j},");
            }
            sb.AppendLine($"            Action<DryvRuleBuilder<TModel>> configure)");
            sb.AppendLine("        {");
            sb.Append("            var builder = new DryvRuleBuilder<TModel>(this, ");
            for (int j = 1; j <= propCount; j++)
            {
                if (j > 1) sb.Append(", ");
                sb.Append($"property{j}");
            }
            sb.AppendLine(");");
            sb.AppendLine("            configure(builder);");
            sb.AppendLine("            return this;");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        private static void WriteBuilderServerValidateMethod(StringBuilder sb, int optionCount, bool sync)
        {
            var gen = GenericParams(optionCount);
            var optPfx = OptionsPrefix(optionCount);
            var typeOfArgs = TypeOfArgs(optionCount);
            var returnType = sync ? "DryvValidationResult" : "Task<DryvValidationResult>";
            var asyncNote = sync ? "" : " asynchronous";
            var diNote = optionCount > 0 ? $" Injected service types are resolved from the DI container at validation time." : "";

            sb.AppendLine($"        /// <summary>Registers an{asyncNote} server-only validation rule using a <c>Func</c> delegate.{diNote} Unlike <c>Validate</c>, the delegate is not an expression tree and is never translated to JavaScript.</summary>");
            sb.AppendLine($"        /// <param name=\"rule\">The server-side validation delegate.</param>");
            sb.AppendLine($"        /// <returns>This builder instance for further chaining.</returns>");
            sb.AppendLine($"        public DryvRuleBuilder<TModel> ServerValidate{gen}(");
            sb.AppendLine($"            Func<TModel, {optPfx}{returnType}> rule)");
            sb.AppendLine("        {");
            sb.AppendLine($"            this.AddServerRule(rule{typeOfArgs});");
            sb.AppendLine("            return this;");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        private static void WriteBuilderDisableMethod(StringBuilder sb, int optionCount)
        {
            var gen = GenericParams(optionCount);
            var optPfx = OptionsPrefix(optionCount);
            var diNote = optionCount > 0 ? $" Injected service types are resolved from the DI container at validation time." : "";

            sb.AppendLine($"        /// <summary>Sets a disabling condition on the preceding validation rule. When the condition returns <c>true</c>, the rule is skipped.{diNote}</summary>");
            sb.AppendLine($"        /// <param name=\"condition\">An expression that returns <c>true</c> to disable the rule.</param>");
            sb.AppendLine($"        /// <returns>This builder instance for further chaining.</returns>");
            sb.AppendLine($"        public DryvRuleBuilder<TModel> Disable{gen}(");
            sb.AppendLine($"            Expression<Func<TModel, {optPfx}bool>> condition)");
            sb.AppendLine("        {");
            sb.AppendLine("            this.SetDisableCondition(condition);");
            sb.AppendLine("            return this;");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        // ──────────────────
        //  Shared helpers
        // ──────────────────

        private static void AppendFileHeader(StringBuilder sb)
        {
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Linq.Expressions;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using Dryv.Validation;");
            sb.AppendLine();
            sb.AppendLine("namespace Dryv");
            sb.AppendLine("{");
        }

        /// <summary>Returns e.g. "&lt;TOptions1, TOptions2&gt;" or "" when count is 0.</summary>
        private static string GenericParams(int optionCount)
        {
            if (optionCount == 0) return "";
            var sb = new StringBuilder("<");
            for (int i = 1; i <= optionCount; i++)
            {
                if (i > 1) sb.Append(", ");
                sb.Append("TOptions").Append(i);
            }
            sb.Append(">");
            return sb.ToString();
        }

        /// <summary>Returns e.g. "TOptions1, TOptions2, " (trailing ", ") or "" when count is 0.</summary>
        private static string OptionsPrefix(int optionCount)
        {
            if (optionCount == 0) return "";
            var sb = new StringBuilder();
            for (int i = 1; i <= optionCount; i++)
            {
                sb.Append("TOptions").Append(i).Append(", ");
            }
            return sb.ToString();
        }

        /// <summary>Returns e.g. ", typeof(TOptions1), typeof(TOptions2)" or "" when count is 0.</summary>
        private static string TypeOfArgs(int optionCount)
        {
            if (optionCount == 0) return "";
            var sb = new StringBuilder();
            for (int i = 1; i <= optionCount; i++)
            {
                sb.Append(", typeof(TOptions").Append(i).Append(")");
            }
            return sb.ToString();
        }

        /// <summary>Returns the property parameter lines, each ending with a comma and newline.</summary>
        private static string PropertyParams(int propCount)
        {
            var sb = new StringBuilder();
            for (int j = 1; j <= propCount; j++)
            {
                sb.Append("            Expression<Func<TModel, object>> property").Append(j).AppendLine(",");
            }
            return sb.ToString();
        }

        /// <summary>Returns e.g. "property1, property2, " for the array initializer.</summary>
        private static string PropertyArray(int propCount)
        {
            var sb = new StringBuilder();
            for (int j = 1; j <= propCount; j++)
            {
                sb.Append("property").Append(j).Append(", ");
            }
            return sb.ToString();
        }
    }
}
