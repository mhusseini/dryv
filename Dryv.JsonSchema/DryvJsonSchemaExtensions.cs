using Dryv.Configuration;

namespace Dryv.JsonSchema;

public static class DryvJsonSchemaExtensions
{
    public static void UseJsonSchemaTranslator(this DryvOptions options)
    {
        options.UseTranslator<JsonSchemaTranslator>();
        options.UseClientValidationFunctionWriter<JsonSchemaFunctionWriter>();
        options.UseClientValidationSetWriter<JsonSchemaSetWriter>();
    }
}
