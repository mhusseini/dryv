using System;
using System.Linq.Expressions;
using Jurassic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dryv.Tests
{
    [TestClass]
    public class ArrayTests : JavascriptTranslatorTestsBase
    {
        [TestMethod]
        public void TranslateLength()
        {
            var expression = (Expression<Func<TestModel, object>>)(m => m.ItemArray.Length);
            var translation = Translate<TestModel>(expression);
            var model = @"{itemArray:['y', 'x', 'z']}";
            var engine = new ScriptEngine();
            var script = $"({translation})({model})";
            var result = engine.Evaluate(script);

            Assert.AreEqual(3, result);
        }
    }
}