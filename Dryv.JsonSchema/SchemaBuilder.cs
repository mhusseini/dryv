using System.Text.Json;

namespace Dryv.JsonSchema;

/// <summary>
/// Builds JSON Schema property fragments and merges per-rule fragments
/// into complete schema documents.
/// </summary>
internal static class SchemaBuilder
{
    internal static Dictionary<string, object> BuildPropertySchema(
        string propertyName,
        Dictionary<string, object?> constraints,
        bool required)
    {
        var schema = new Dictionary<string, object> { ["type"] = "object" };

        if (constraints.Count > 0)
        {
            schema["properties"] = new Dictionary<string, object>
            {
                [propertyName] = constraints
            };
        }

        if (required)
        {
            schema["required"] = new[] { propertyName };
        }

        return schema;
    }

    /// <summary>
    /// Merges the per-rule JSON Schema fragments produced by <see cref="JsonSchemaTranslator.Translate(System.Linq.Expressions.Expression, System.Linq.Expressions.MemberExpression, Dryv.Rules.DryvRuleDefinition)"/>
    /// into a single JSON Schema document.
    /// </summary>
    internal static Dictionary<string, object> MergeFragments(
        IEnumerable<string?> jsonFragments,
        string? title = null)
    {
        var mergedProperties = new Dictionary<string, Dictionary<string, JsonElement>>();
        var mergedRequired = new HashSet<string>();

        foreach (var fragment in jsonFragments)
        {
            if (string.IsNullOrEmpty(fragment))
            {
                continue;
            }

            using var doc = JsonDocument.Parse(fragment);
            var root = doc.RootElement;

            if (root.TryGetProperty("properties", out var props))
            {
                foreach (var prop in props.EnumerateObject())
                {
                    if (!mergedProperties.ContainsKey(prop.Name))
                        mergedProperties[prop.Name] = new Dictionary<string, JsonElement>();

                    foreach (var constraint in prop.Value.EnumerateObject())
                    {
                        mergedProperties[prop.Name][constraint.Name] = constraint.Value.Clone();
                    }
                }
            }

            if (!root.TryGetProperty("required", out var req))
            {
                continue;
            }
            
            foreach (var item in req.EnumerateArray())
            {
                mergedRequired.Add(item.GetString()!);
            }
        }

        var schema = new Dictionary<string, object>
        {
            ["$schema"] = "https://json-schema.org/draft/2020-12/schema",
            ["type"] = "object",
            ["properties"] = mergedProperties.ToDictionary(
                kvp => kvp.Key,
                kvp => (object)kvp.Value),
        };

        if (title != null)
        {
            schema["title"] = title;
        }

        if (mergedRequired.Count > 0)
        {
            schema["required"] = mergedRequired.OrderBy(r => r).ToArray();
        }

        return schema;
    }
}
