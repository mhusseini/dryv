using System.Text.Json;
using Dryv.Validation;

namespace Dryv.JsonSchema;

/// <summary>
/// Merges per-rule JSON Schema fragments for a single property path
/// into a unified constraints object.
/// </summary>
public class JsonSchemaFunctionWriter : IDryvClientValidationFunctionWriter
{
    public Action<TextWriter> GetValidationFunction(IEnumerable<TranslatedRule> translatedRules) => writer =>
    {
        var constraints = new Dictionary<string, JsonElement>();
        var required = false;

        foreach (var rule in translatedRules)
        {
            if (string.IsNullOrEmpty(rule.ClientCode))
            {
                continue;
            }

            using var doc = JsonDocument.Parse(rule.ClientCode);
            var root = doc.RootElement;

            if (root.TryGetProperty("properties", out var props))
            {
                foreach (var prop in props.EnumerateObject())
                {
                    foreach (var constraint in prop.Value.EnumerateObject())
                    {
                        constraints[constraint.Name] = constraint.Value.Clone();
                    }
                }
            }

            if (root.TryGetProperty("required", out _))
            {
                required = true;
            }
        }

        if (required)
        {
            constraints["$required"] = JsonDocument.Parse("true").RootElement;
        }

        writer.Write(JsonSerializer.Serialize(constraints));
    };
}
