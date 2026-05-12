using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Dryv.Configuration;
using Dryv.RuleDetection;
using Dryv.Rules;
using Dryv.Translation;
using Dryv.Translation.Translators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dryv.Tests
{
    [TestClass]
    public class MultiPropertyRuleTests
    {
        private DryvRuleFinder sut;

        [TestInitialize]
        public void Initialize()
        {
            DryvRuleFinder.ClearCache();

            var methodCallTranslators = new Collection<IDryvMethodCallTranslator>
            {
                new RegexTranslator(),
                new DryvValidationResultTranslator(),
                new StringTranslator(),
                new EnumerableTranslator()
            };

            var customTranslators = new Collection<IDryvCustomTranslator>
            {
                new RegexTranslator(),
                new DryvValidationResultTranslator(),
                new ObjectTranslator()
            };

            var options = new DryvOptions();
            var treeBuilder = new ModelTreeBuilder();
            var compiler = new DryvCompiler();
            var translator = new JavaScriptTranslator(customTranslators, methodCallTranslators, options);

            sut = new DryvRuleFinder(treeBuilder, compiler, translator, null, options);
        }

        [TestMethod]
        public void MultiPropertyRule_AllProperties_HaveClientRulesWithFullValidation()
        {
            var allRules = sut.FindValidationRulesInTree(typeof(AddressModel), RuleType.Validation);
            var options = new DryvOptions();

            foreach (var propName in new[] { "Street", "City", "Zip" })
            {
                var prop = typeof(AddressModel).GetProperty(propName);
                var clientRules = allRules.Where(r => r.Property == prop && r.EvaluationLocation.HasFlag(DryvEvaluationLocation.Client)).ToList();

                Assert.IsTrue(clientRules.Any(), $"{propName} should have client-side rules");

                var code = clientRules.First().TranslatedValidationExpression(_ => null, new object[] { "" }, options);
                Assert.IsTrue(code.Contains("Address is incomplete"), $"{propName} should have the full validation logic in client code");
            }
        }

        [TestMethod]
        public void MultiPropertyRule_AllProperties_HaveRelatedProperties()
        {
            var allRules = sut.FindValidationRulesInTree(typeof(AddressModel), RuleType.Validation);

            var streetProp = typeof(AddressModel).GetProperty(nameof(AddressModel.Street));
            var cityProp = typeof(AddressModel).GetProperty(nameof(AddressModel.City));
            var zipProp = typeof(AddressModel).GetProperty(nameof(AddressModel.Zip));

            var streetRule = allRules.First(r => r.Property == streetProp && r.EvaluationLocation.HasFlag(DryvEvaluationLocation.Client));
            var cityRule = allRules.First(r => r.Property == cityProp && r.EvaluationLocation.HasFlag(DryvEvaluationLocation.Client));
            var zipRule = allRules.First(r => r.Property == zipProp && r.EvaluationLocation.HasFlag(DryvEvaluationLocation.Client));

            Assert.IsTrue(streetRule.RelatedProperties.Values.Any(v => v.Contains("city")), "Street should have city as related");
            Assert.IsTrue(streetRule.RelatedProperties.Values.Any(v => v.Contains("zip")), "Street should have zip as related");
            Assert.IsTrue(cityRule.RelatedProperties.Values.Any(v => v.Contains("street")), "City should have street as related");
            Assert.IsTrue(zipRule.RelatedProperties.Values.Any(v => v.Contains("street")), "Zip should have street as related");
        }

        [TestMethod]
        public void MultiPropertyRule_AllProperties_HaveGroupSetToFirstPropertyPath()
        {
            var allRules = sut.FindValidationRulesInTree(typeof(AddressModel), RuleType.Validation);

            foreach (var propName in new[] { "Street", "City", "Zip" })
            {
                var prop = typeof(AddressModel).GetProperty(propName);
                var clientRules = allRules.Where(r => r.Property == prop && r.EvaluationLocation.HasFlag(DryvEvaluationLocation.Client)).ToList();

                Assert.IsTrue(clientRules.Any(), $"{propName} should have client-side rules");
                Assert.AreEqual("street", clientRules.First().Group,
                    $"{propName}'s rule group should be set to the first property's path ('street')");
            }
        }

        [TestMethod]
        public void MultiPropertyRule_ServerSide_AllPropertiesHaveFullRule()
        {
            var streetProperty = typeof(AddressModel).GetProperty(nameof(AddressModel.Street));
            var cityProperty = typeof(AddressModel).GetProperty(nameof(AddressModel.City));
            var zipProperty = typeof(AddressModel).GetProperty(nameof(AddressModel.Zip));

            var allRules = sut.FindValidationRulesInTree(typeof(AddressModel), RuleType.Validation);

            foreach (var prop in new[] { streetProperty, cityProperty, zipProperty })
            {
                var serverRules = allRules.Where(r => r.Property == prop && r.EvaluationLocation.HasFlag(DryvEvaluationLocation.Server)).ToList();
                Assert.IsTrue(serverRules.Any(), $"{prop.Name} should have server rules");

                var result = serverRules.First().CompiledValidationExpression(new AddressModel { Street = null, City = null, Zip = null }, new object[0]);
                Assert.IsNotNull(result, $"{prop.Name} server rule should produce an error when fields are empty");
            }
        }

        [TestMethod]
        public void MultiPropertyRule_SinglePropertyRule_HasNoGroup()
        {
            var emailProperty = typeof(AddressModel).GetProperty(nameof(AddressModel.Email));
            var allRules = sut.FindValidationRulesInTree(typeof(AddressModel), RuleType.Validation);
            var clientRules = allRules.Where(r => r.Property == emailProperty && r.EvaluationLocation.HasFlag(DryvEvaluationLocation.Client)).ToList();

            Assert.IsTrue(clientRules.Any(), "Single-property rule should have client-side rule");
            Assert.IsTrue(string.IsNullOrWhiteSpace(clientRules.First().Group),
                "Single-property rule should not have a group set");

            var code = clientRules.First().TranslatedValidationExpression(_ => null, new object[] { "" }, new DryvOptions());
            Assert.IsTrue(code.Contains("Email is required"), "Single-property rule should retain full validation logic");
        }

        [TestMethod]
        public void MultiPropertyRule_ExplicitGroupIsNotOverridden()
        {
            var allRules = sut.FindValidationRulesInTree(typeof(ModelWithExplicitGroup), RuleType.Validation);
            var prop = typeof(ModelWithExplicitGroup).GetProperty(nameof(ModelWithExplicitGroup.Field1));
            var clientRules = allRules.Where(r => r.Property == prop && r.EvaluationLocation.HasFlag(DryvEvaluationLocation.Client)).ToList();

            Assert.IsTrue(clientRules.Any(), "Field1 should have client-side rules");
            Assert.AreEqual("my-custom-group", clientRules.First().Group,
                "Explicit group from settings should not be overridden");
        }

        [TestMethod]
        public void MultiPropertyRule_DumpGeneratedOutput()
        {
            var allRules = sut.FindValidationRulesInTree(typeof(AddressModel), RuleType.Validation);
            var options = new DryvOptions();
            var properties = new[] { "Street", "City", "Zip", "Email" };

            foreach (var propName in properties)
            {
                var prop = typeof(AddressModel).GetProperty(propName);
                var clientRules = allRules
                    .Where(r => r.Property == prop && r.EvaluationLocation.HasFlag(DryvEvaluationLocation.Client))
                    .ToList();

                System.Console.WriteLine($"\n=== {propName} (client rules: {clientRules.Count}) ===");
                foreach (var rule in clientRules)
                {
                    var code = rule.TranslatedValidationExpression(_ => null, new object[] { "" }, options);
                    var related = rule.RelatedProperties?.Any() == true
                        ? string.Join(", ", rule.RelatedProperties.Values)
                        : "(none)";
                    System.Console.WriteLine($"  group: {rule.Group ?? "(none)"}");
                    System.Console.WriteLine($"  related: [{related}]");
                    System.Console.WriteLine($"  validate: {code}");
                }
            }
        }

        private class AddressModel
        {
            public static DryvRules Rules = DryvRules
                .For<AddressModel>()
                .Rule(m => m.Street, m => m.City, m => m.Zip,
                    m => string.IsNullOrWhiteSpace(m.Street) || string.IsNullOrWhiteSpace(m.City) || string.IsNullOrWhiteSpace(m.Zip)
                        ? DryvValidationResult.Error("Address is incomplete")
                        : DryvValidationResult.Success)
                .Rule(m => m.Email,
                    m => string.IsNullOrWhiteSpace(m.Email)
                        ? DryvValidationResult.Error("Email is required")
                        : DryvValidationResult.Success);

            public string Street { get; set; }
            public string City { get; set; }
            public string Zip { get; set; }
            public string Email { get; set; }
        }

        private class ModelWithExplicitGroup
        {
            public static DryvRules Rules = DryvRules
                .For<ModelWithExplicitGroup>()
                .Rule(m => m.Field1, m => m.Field2,
                    m => string.IsNullOrWhiteSpace(m.Field1)
                        ? DryvValidationResult.Error("error")
                        : DryvValidationResult.Success,
                    new DryvRuleSettings("my-custom-group"));

            public string Field1 { get; set; }
            public string Field2 { get; set; }
        }
    }
}
