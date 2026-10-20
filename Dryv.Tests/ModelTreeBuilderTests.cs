using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Dryv.Configuration;
using Dryv.RuleDetection;
using Dryv.Translation;
using Dryv.Translation.Translators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dryv.Tests
{
    [TestClass]
    public class ModelTreeBuilderTests
    {
        [TestMethod]
        public void Build_ReturnsTreeWithCorrectRootType()
        {
            var finder = new ModelTreeBuilder();
            var tree = finder.Build(typeof(ParentModel));

            Assert.IsNotNull(tree, "Build should return a non-null tree.");
            Assert.AreEqual(typeof(ParentModel), tree.ModelType, "Root node should have the ParentModel type.");
        }

        [TestMethod]
        public void Build_TreeContainsExpectedChildProperties()
        {
            var finder = new ModelTreeBuilder();
            var tree = finder.Build(typeof(ParentModel));

            var childNames = tree.Children.Select(c => c.Name).ToList();

            Assert.IsTrue(childNames.Contains(nameof(ParentModel.Id)), "Tree should contain Id property.");
            Assert.IsTrue(childNames.Contains(nameof(ParentModel.Child1)), "Tree should contain Child1 property.");
            Assert.IsTrue(childNames.Contains(nameof(ParentModel.Child2)), "Tree should contain Child2 property.");
        }

        [TestMethod]
        public void Build_ChildNodesHaveCorrectModelType()
        {
            var finder = new ModelTreeBuilder();
            var tree = finder.Build(typeof(ParentModel));

            var child1Edge = tree.Children.First(c => c.Name == nameof(ParentModel.Child1));

            Assert.AreEqual(typeof(ChildModel), child1Edge.Child.ModelType, "Child1 node should have ChildModel type.");
        }
    }

    [TestClass]
    public class RuleMatchingTests
    {
        [TestMethod]
        public async Task Validate_ReturnsErrorsForInvalidModel()
        {
            var m = new ParentModel
            {
                Child1 = new ChildModel
                {
                    Name = "b"
                }
            };

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
            var ruleDiscovery = new RuleDiscovery();
            var javaScriptTranslator = new JavaScriptTranslator(customTranslators, methodCallTranslators, options);
            var ruleFinder = new DryvRuleFinder(treeBuilder, compiler, ruleDiscovery, null);
            var ruleTranslationPhase = new RuleTranslationPhase(ruleFinder, javaScriptTranslator, options);

            var validationCache = new DryvValidationCache(ruleFinder);
            var serviceResolver = DryvServiceResolverFactory.FromFunc(t => new Thingy());
            var pipeline = new RuleResolutionPipeline();
            var validator = new DryvValidator(validationCache, serviceResolver, options, pipeline);
            var errors = await validator.Validate(m);

            Assert.IsNotNull(errors, "Validation should return a non-null result.");
            Assert.IsTrue(errors.Count > 0, "Validation should return errors for a model with Name='b'.");
        }

        [TestMethod]
        public async Task TranslateValidationRules_ReturnsTranslatedCode()
        {
            var m = new ParentModel
            {
                Child1 = new ChildModel
                {
                    Name = "b"
                }
            };

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
            var ruleDiscovery = new RuleDiscovery();
            var javaScriptTranslator = new JavaScriptTranslator(customTranslators, methodCallTranslators, options);
            var ruleFinder = new DryvRuleFinder(treeBuilder, compiler, ruleDiscovery, null);
            var ruleTranslationPhase = new RuleTranslationPhase(ruleFinder, javaScriptTranslator, options);
            var pipeline = new RuleResolutionPipeline();
            var translator = new DryvTranslator(ruleTranslationPhase, pipeline);
            var result = await translator.TranslateValidationRules(m.GetType(), DryvServiceResolverFactory.FromFunc(t => null));

            Assert.IsNotNull(result, "TranslateValidationRules should return a non-null result.");
            Assert.IsNotNull(result.ValidationFunctions, "Translation result should contain validation functions.");
            Assert.IsTrue(result.ValidationFunctions.Count > 0, "Translation result should contain at least one validation function.");
        }
    }

    internal class ParentModel
    {
        //private static readonly DryvRules Rules = DryvRules.For<ParentModel>()
        //    .Rule(m => m.Child1.Name, m => m.Child1 != null && m.Child1.Name != "a" ? null : "error")
        //    .Rule(m => m.Child2.Id, m => m.Child2 != null && m.Child2.Id != "a" ? null : "error");
        public string Id { get; set; }

        public ChildModel Child1 { get; set; }
        public ChildModel Child2 { get; set; }
    }

    internal class Thingy
    {
        public Task<bool> IsValid(string text) => Task.FromResult(false);
    }

    internal class ChildModel
    {
        private static readonly DryvRules Rules = DryvRules.For<ChildModel>()
            .Rule(m => m.Name, m => m != null && m.Name != "b" ? null : "error")
            .Rule(m => m.Id, m => m != null && m.Id != "b" ? null : "error");

        public string Id { get; set; }
        public ChildModel Child3 { get; set; }

        public string Name { get; set; }
    }
}