using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Dryv.Extensions;
using Dryv.Reflection;
using Dryv.Rules;
using Dryv.Translation.Visitors;
using BindingFlags = System.Reflection.BindingFlags;

namespace Dryv.RuleDetection
{
    public class DryvRuleFinder(ModelTreeBuilder treeBuilder, DryvCompiler compiler, RuleDiscovery ruleDiscovery, IReadOnlyCollection<IDryvRuleAnnotator> annotators)
    {
        private const BindingFlags BindingFlagsForProperties = BindingFlags.FlattenHierarchy |
                                                               BindingFlags.Instance |
                                                               BindingFlags.Public |
                                                               BindingFlags.NonPublic;

        private readonly ConcurrentDictionary<string, IEnumerable<DryvCompiledRule>> compiledRuleTreeCache = new();
        private readonly IReadOnlyCollection<IDryvRuleAnnotator> annotators = annotators ?? Array.Empty<IDryvRuleAnnotator>();

        public void ClearCache()
        {
            ruleDiscovery.ClearCache();
            compiledRuleTreeCache.Clear();
        }

        public IEnumerable<DryvCompiledRule> FindValidationRulesInTree(Type rootType, RuleType ruleType)
        {
            return compiledRuleTreeCache.GetOrAdd($"{rootType.FullName}|{ruleType}", _ =>
            {
                var result = new List<DryvCompiledRule>();
                var tree = treeBuilder.Build(rootType);
                var flatTree = tree.Iterate(t => t.Children.Select(c => c.Child).Where(c => c != null)).ToList();
                var scopedRules = FindRulesInModelTree(rootType, ruleType, new HashSet<Type>(), null).ToList();

                foreach (var (rule, scopePath) in scopedRules)
                {
                    if (this.annotators != null)
                    {
                        foreach (var annotator in this.annotators)
                        {
                            annotator.Annotate(rule, rule.ValidationExpression);
                        }
                    }

                    var nodes = GetNodesForRule(flatTree, rule, scopePath);
                    result.AddRange(nodes.Select(node => this.ApplyRuleToNode(node, rule)));
                }

                return result;
            });
        }

        private IEnumerable<(DryvRuleDefinition Rule, string ScopePath)> FindRulesInModelTree(Type rootType, RuleType ruleType, ICollection<Type> processed, string currentPath)
        {
            if (processed.Contains(rootType))
            {
                yield break;
            }

            processed.Add(rootType);

            var baseTypes = rootType.Iterate(t => t?.GetBaseType())
                .Where(t => t != null && t.Namespace != typeof(object).Namespace)
                .ToList();

            foreach (var rule in from type in baseTypes
                                 from rule in ruleDiscovery.FindRulesOnType(type, ruleType)
                                 select rule)
            {
                yield return (rule, currentPath);
            }

            foreach (var scopedRule in from type in baseTypes
                                 from attribute in type.GetTypeInfo().GetCustomAttributes<DryvValidationAttribute>()
                                 where attribute.RuleContainerType != null
                                 from scopedRule in FindRulesInModelTree(attribute.RuleContainerType, ruleType, processed, currentPath)
                                 select scopedRule)
            {
                yield return scopedRule;
            }

            foreach (var scopedRule in from property in rootType.GetProperties(BindingFlagsForProperties)
                                 let propertyType = property.PropertyType.GetElementTypeOrSelf()
                                 where propertyType.Namespace != typeof(object).Namespace
                                 from scopedRule in FindRulesInModelTree(propertyType, ruleType, processed, currentPath)
                                 select scopedRule)
            {
                yield return scopedRule;
            }

            foreach (var scopedRule in from property in rootType.GetProperties(BindingFlagsForProperties)
                                 from attribute in property.GetCustomAttributes<DryvValidationAttribute>()
                                 where attribute.RuleContainerType != null
                                 let propertyPath = string.IsNullOrEmpty(currentPath) ? property.Name : currentPath + "." + property.Name
                                 from scopedRule in FindRulesInModelTree(attribute.RuleContainerType, ruleType, new HashSet<Type>(processed), propertyPath)
                                 select scopedRule)
            {
                yield return scopedRule;
            }

            foreach (var scopedRule in from field in rootType.GetTypeInfo().DeclaredFields
                                 from attribute in field.GetCustomAttributes<DryvValidationAttribute>()
                                 where attribute.RuleContainerType != null
                                 let fieldPath = string.IsNullOrEmpty(currentPath) ? field.Name : currentPath + "." + field.Name
                                 from scopedRule in FindRulesInModelTree(attribute.RuleContainerType, ruleType, new HashSet<Type>(processed), fieldPath)
                                 select scopedRule)
            {
                yield return scopedRule;
            }
        }

        private static string GetEffectiveModelPath(string originalPath, string transposedPath, PropertyInfo property)
        {
            var basePath = string.IsNullOrWhiteSpace(transposedPath) ? originalPath : transposedPath;
            var parts = basePath.Split('.');
            var path = string.Join(".", parts.Skip(1).Select(p => p.ToCamelCase()));
            var sep = string.IsNullOrWhiteSpace(path) ? string.Empty : ".";

            return path + sep + property.Name.ToCamelCase();
        }

        private static IEnumerable<ModelTreeNode> GetNodesForRule(List<ModelTreeNode> flatTree, DryvRuleDefinition rule, string scopePath)
        {
            var rulePropertyName = rule.Property.Name;
            var rulePropertyType = rule.Property.PropertyType;
            var ruleDeclaringType = rule.Property.DeclaringType;

            return flatTree.FindAll(n =>
            {
                if (n.Hierarchy == null || n.Hierarchy.Count == 0)
                    return false;

                var lastMember = n.Hierarchy.Last();
                if (lastMember.Name != rulePropertyName)
                    return false;

                var memberType = lastMember switch
                {
                    PropertyInfo p => p.PropertyType,
                    FieldInfo f => f.FieldType,
                    _ => null
                };

                if (memberType != rulePropertyType)
                    return false;

                var nodeDeclaringType = lastMember.DeclaringType;
                if (ruleDeclaringType == null || nodeDeclaringType == null || !ruleDeclaringType.IsAssignableFrom(nodeDeclaringType))
                    return false;

                // If scopePath is set, the node's path must start with it
                if (!string.IsNullOrEmpty(scopePath))
                {
                    var nodePath = string.Join(".", n.Hierarchy.Select(m => m.Name));
                    return nodePath.StartsWith(scopePath + ".");
                }

                return true;
            });
        }

        private static bool HasCollectionInPath(ModelTreeNode node)
        {
            if (node.Hierarchy == null || node.Hierarchy.Count <= 1)
                return false;

            foreach (var member in node.Hierarchy.Take(node.Hierarchy.Count - 1))
            {
                var type = member switch
                {
                    PropertyInfo p => p.PropertyType,
                    FieldInfo f => f.FieldType,
                    _ => null
                };
                if (type != null && type != type.GetElementTypeOrSelf())
                    return true;
            }

            return false;
        }

        private static Type TransposeExpressions(ModelTreeNode node, DryvRuleDefinition rule, out LambdaExpression newValidationExpression, out string transposedPath)
        {
            var firstMember = node.Hierarchy.First();
            var modelType = firstMember.DeclaringType ?? throw new InvalidOperationException($"DeclaringType of member '{firstMember.Name}' is null.");
            var modelParameter = Expression.Parameter(modelType, "$m");
            var modelReplacement = TransposePath(modelParameter, node, out transposedPath);

            var parameters = new List<ParameterExpression> { modelParameter };
            parameters.AddRange(rule.ValidationExpression.Parameters.Skip(1));

            var replacer = new NodeReplacer();
            var validationBody = replacer.Replace(
                rule.ValidationExpression.Body,
                rule.ValidationExpression.Parameters.First(),
                modelReplacement);
            newValidationExpression = Expression.Lambda(validationBody, parameters);

            return modelType;
        }

        private static Expression TransposePath(ParameterExpression modelParameter, ModelTreeNode node, out string transposedPath)
        {
            Expression modelReplacement = modelParameter;
            var sb = new StringBuilder(modelParameter.Name);

            foreach (var memberInfo in node.Hierarchy.Take(node.Hierarchy.Count - 1))
            {
                sb.Append(".");
                sb.Append(memberInfo.Name.ToCamelCase());
                modelReplacement = Expression.MakeMemberAccess(modelReplacement, memberInfo);
            }

            transposedPath = sb.ToString();
            return modelReplacement;
        }

        private DryvCompiledRule ApplyRuleToNode(ModelTreeNode node, DryvRuleDefinition rule)
        {
            LambdaExpression newValidationExpression;
            Type modelType;
            string transposedPath = null;

            var hasCollectionInPath = HasCollectionInPath(node);
            if (node.UniquePath != rule.UniquePath && !hasCollectionInPath)
            {
                modelType = TransposeExpressions(node, rule, out newValidationExpression, out transposedPath);
            }
            else
            {
                newValidationExpression = rule.ValidationExpression;
                modelType = rule.ModelType;
            }

            var concreteProperty = node.Hierarchy?.LastOrDefault() as PropertyInfo ?? rule.Property;
            var serviceTypes = rule.ServiceTypes ?? newValidationExpression.Parameters.Skip(1).Select(p => p.Type).ToArray();

            var compiledRule = new DryvCompiledRule
            {
                ModelType = modelType,
                CompiledEnablingExpression = compiler.CompileEnablingExpression(rule, rule.EnablingExpression),
                CompiledValidationExpression = compiler.CompileValidationExpression(rule, newValidationExpression),
                EnablingExpression = rule.EnablingExpression,
                ValidationExpression = newValidationExpression,
                EvaluationLocation = rule.EvaluationLocation,
                PropertyExpression = rule.PropertyExpression,
                ServiceTypes = serviceTypes,
                Group = rule.Group,
                Name = rule.Name,
                RuleType = rule.RuleType,
                ModelPath = GetEffectiveModelPath(rule.ModelPath, transposedPath, concreteProperty),
                Property = concreteProperty,
                UniquePath = rule.UniquePath,
                Parameters = rule.Parameters,
                Annotations = rule.Annotations,
                RelatedProperties = rule.RelatedProperties?.ToDictionary(
                    p => p.Key,
                    p => GetEffectiveModelPath(p.Value, transposedPath, p.Key)),
            };

            return compiledRule;
        }
    }
}