using Escape.Ast;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dryv.Tests
{
    [TestClass]
    public class EnumTranslationTests : JavascriptTranslatorTestsBase
    {
        [TestMethod]
        public void EnumsAreSerializedAsConfigured()
        {
            var expression = Expression<Model>(m => m.Prop1 != MyEnum.Two ? null : "fail");
            var translation = Translate<Model>(expression);
            var model = @"{prop1:'Two'}";
            var engine = new Jurassic.ScriptEngine();
            var script = $"({translation})({model})";
            var result = engine.Evaluate(script);

            Assert.IsNotNull(result);
        }

        private enum MyEnum
        {
            One,
            Two
        }

        private class Model
        {
            public MyEnum Prop1 { get; set; }
        }
    }
}