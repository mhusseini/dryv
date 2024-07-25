using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Dryv;
using Dryv.AspNetCore.Json;
using Dryv.Configuration;
using Dryv.RuleDetection;
using Dryv.Rules;
using Dryv.SampleConsole.Models;
using Dryv.Translation;
using Dryv.Translation.Translators;

internal class Program
{
    private static async Task Main()
    {
        var model = new HomeModel
        {
            Enum = MyEnum.AnotherValue,
            // Person = new Person(),
            // ShippingAddress = new Address(),
            // BillingAddress = new Address { Deactivated = true },
        };
        
        var jsonOptions = new JsonSerializerOptions
        {
            Converters =
            {
                new JsonStringEnumConverter(),
                new ValueTupleFactory()
            },
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        var options = new DryvOptions();
        options.JsonConversion = v => JsonSerializer.Serialize(v, jsonOptions);
        options.Translators.Add<DryvValidationResultTranslator>();
        options.Translators.Add<DateTimeTranslator>();
        options.Translators.Add<StringTranslator>();
        options.Translators.Add<ToStringTranslator>();
        options.Translators.Add<EnumerableTranslator>();
        options.Translators.Add<RegexTranslator>();
        options.Translators.Add<CustomCodeTranslator>();

        //var validator = new DryvValidator(new DryvRuleFinder(new ModelTreeBuilder(), new DryvCompiler(), null, null, options), options);
        var translator = new DryvTranslator(new DryvRuleFinder(
            new ModelTreeBuilder(),
            new DryvCompiler(),
            new JavaScriptTranslator(
                options.Translators.Where(t => typeof(IDryvCustomTranslator).IsAssignableFrom(t)).Select(Activator.CreateInstance).Cast<IDryvCustomTranslator>().ToList(),
                options.Translators.Where(t => typeof(IDryvMethodCallTranslator).IsAssignableFrom(t)).Select(Activator.CreateInstance).Cast<IDryvMethodCallTranslator>().ToList(),
                options
            ),
            options.Annotators.Select(Activator.CreateInstance).Cast<IDryvRuleAnnotator>().ToList(),
            options
        ));

        var translated = await translator.TranslateValidationRules(typeof(HomeModel), Activator.CreateInstance);
        // var errors = await validator.Validate(model, Activator.CreateInstance);
        //
        // foreach (var error in from e in errors
        //                       select e.Value.Type + " " + e.Key + ": " + e.Value.Text)
        // {
        //     Console.WriteLine(error);
        // }
    }
}