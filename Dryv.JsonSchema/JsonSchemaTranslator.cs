using System.Linq.Expressions;
using System.Text.Json;
using Dryv.Configuration;
using Dryv.Rules;
using Dryv.Translation;

namespace Dryv.JsonSchema;

/// <summary>
/// Translates Dryv validation rules into JSON Schema fragments.
/// Delegates expression analysis to <see cref="ConstraintExtractor"/>,
/// schema assembly to <see cref="SchemaBuilder"/>, and expression
/// utilities to <see cref="ExpressionHelpers"/>.
/// </summary>
public class JsonSchemaTranslator(DryvOptions options) : Translator(options)
{
    public override Translation.TranslationResult Translate(
        Expression expression,
        MemberExpression propertyExpression,
        DryvRuleDefinition rule)
    {
        var lambda = (LambdaExpression)expression;
        var propertyName = ResolvePropertyName(propertyExpression, lambda);
        var constraints = new Dictionary<string, object?>();
        var required = false;

        ConstraintExtractor.AnalyzeBody(lambda.Body, constraints, ref required);

        var schema = SchemaBuilder.BuildPropertySchema(propertyName, constraints, required);
        var json = JsonSerializer.Serialize(schema);

        return new Translation.TranslationResult
        {
            Factory = (_, _, _) => json,
            InjectedServiceTypes = rule.ServiceTypes ?? Array.Empty<Type>(),
            CodeTemplate = json,
        };
    }

    public override bool TryWriteTerminal(Expression expression, TextWriter writer) => false;

    public override string FormatIdentifier(string name)
    {
        return string.IsNullOrEmpty(name)
            ? name
            : name.Length == 1
                ? name.ToLowerInvariant()
                : char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    public override string TranslateValue(object? value) =>
        JsonSerializer.Serialize(value);

    /// <summary>
    /// Merges the per-rule JSON Schema fragments produced by <see cref="Translate"/>
    /// into a single JSON Schema document.
    /// </summary>
    public static Dictionary<string, object> BuildSchema(
        TranslationResult translationResult,
        string? title = null)
    {
        var fragments = translationResult.ValidationFunctions
            .SelectMany(kvp => kvp.Value)
            .Select(r => r.ClientCode);

        return SchemaBuilder.MergeFragments(fragments, title);
    }

    private string ResolvePropertyName(MemberExpression? propertyExpression, LambdaExpression lambda)
    {
        if (propertyExpression?.Member != null)
        {
            return FormatIdentifier(propertyExpression.Member.Name);
        }

        var finder = new ExpressionVisitors.PropertyNameFinder(lambda.Parameters.FirstOrDefault());
        finder.Visit(lambda.Body);
        return finder.PropertyName != null
            ? FormatIdentifier(finder.PropertyName)
            : "unknown";
    }
}
