using Escape.Ast;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dryv.Tests
{
    [TestClass]
    public class BooleanTranslationTests : JavascriptTranslatorTestsBase
    {
        [TestMethod]
        public void InlineOptions()
        {
            var exp = Expression(() => true);
            var jsProgram = GetTranslatedAst(exp);

            var literal = GetBodyExpression<Literal>(jsProgram);

            Assert.AreEqual(true, literal.Value);
        }
    }
}