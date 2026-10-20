using System.Collections.ObjectModel;
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
        public void Test()
        {
            var finder = new ModelTreeBuilder();
            var tree = finder.Build(typeof(ParentModel));
        }
    }

    [TestClass]
    public class RuleMatchingTests
    {
        [TestMethod]
        public async Task Test()
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
            var modelType = typeof(ParentModel);
            
            var validator = new DryvValidator(ruleFinder, options);
            var errors = await validator.Validate(m, DryvServiceResolverFactory.FromFunc(t => new Thingy()));
            var translator = new DryvTranslator(ruleTranslationPhase);
            var code = translator.TranslateValidationRules(m.GetType(), DryvServiceResolverFactory.FromFunc(t => null));
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