using System.Text.Json;
using Dryv.Validation;

namespace Dryv.JsonSchema;

/// <summary>
/// Composes per-property constraint objects (produced by <see cref="JsonSchemaFunctionWriter"/>)
/// into a complete JSON Schema document.
/// </summary>
public class JsonSchemaSetWriter : IDryvClientValidationSetWriter
{
    public void WriteBegin(TextWriter writer)
    {
        writer.Write("{\"$schema\":\"https://json-schema.org/draft/2020-12/schema\",\"type\":\"object\"");
    }

    public void WriteEnd(TextWriter writer)
    {
        writer.Write("}");
    }

    public void WriteValidationSet(
        TextWriter writer,
        string validationSetName,
        IDictionary<string, Action<TextWriter>> validators,
        IDictionary<string, Action<TextWriter>> disablers,
        IDictionary<string, object> parameters)
    {
        if (!string.IsNullOrEmpty(validationSetName))
        {
            writer.Write(",\"title\":");
            writer.Write(JsonSerializer.Serialize(validationSetName));
        }

        var required = new List<string>();
        var propertySep = "";

        writer.Write(",\"properties\":{");

        foreach (var (path, writeAction) in validators)
        {
            // Capture the function writer's output for this property.
            using var sw = new StringWriter();
            writeAction(sw);
            var constraintsJson = sw.ToString();

            // Separate actual constraints from the $required marker.
            using var doc = JsonDocument.Parse(constraintsJson);
            var root = doc.RootElement;

            var cleanConstraints = new Dictionary<string, JsonElement>();

            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Name == "$required")
                {
                    if (prop.Value.ValueKind == JsonValueKind.True)
                    {
                        required.Add(path);
                    }
                }
                else
                {
                    cleanConstraints[prop.Name] = prop.Value.Clone();
                }
            }

            writer.Write(propertySep);
            writer.Write(JsonSerializer.Serialize(path));
            writer.Write(":");
            writer.Write(JsonSerializer.Serialize(cleanConstraints));
            propertySep = ",";
        }

        writer.Write("}");

        if (required.Count > 0)
        {
            required.Sort(StringComparer.Ordinal);
            writer.Write(",\"required\":");
            writer.Write(JsonSerializer.Serialize(required));
        }
    }
}
