using Escape;
using Escape.Ast;
using Jurassic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;

namespace Dryv.Tests
{
    [TestClass]
    public class RegularExpressionTests : JavascriptTranslatorTestsBase
    {
        [TestMethod]
        public void TranslateNegadtedExpression()
        {
            var pattern = @"^\d+$";
            var expression = Expression(m =>
                !new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline).IsMatch(m.Text)
                    ? "fail"
                    : DryvValidationResult.Success);

            var jsProgram = GetTranslatedAst(expression);
            var conditional = GetBodyExpression<ConditionalExpression>(jsProgram);
            var unaryExpression = conditional.Test as UnaryExpression;
            
            Assert.IsNotNull(unaryExpression, "Unary expression not found.");
            Assert.AreEqual(UnaryOperator.LogicalNot, unaryExpression.Operator, "Logical Not not found.");
        }

        [TestMethod]
        public void TranslateIsMatch()
        {
            var pattern = @"^\d+$";
            var expression = Expression(m =>
                new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline).IsMatch(m.Text)
                    ? "fail"
                    : DryvValidationResult.Success);

            var jsProgram = GetTranslatedAst(expression);
            var conditional = GetBodyExpression<ConditionalExpression>(jsProgram);
            var method = GetMethod(conditional?.Test);
            var regexp = (method.Object as Literal)?.Value as RegExp;

            Assert.AreEqual(pattern, regexp?.Pattern);
            Assert.AreEqual(RegExpFlags.IgnoreCase, regexp?.Flags);
            Assert.AreEqual("test", method.Name);
        }

        [TestMethod]
        public void TranslateStaticIsMatch()
        {
            var pattern = @"^\d+$";
            var expression = Expression(m =>
                Regex.IsMatch(pattern, m.Text, RegexOptions.IgnoreCase | RegexOptions.Singleline)
                    ? "fail"
                    : DryvValidationResult.Success);

            var jsProgram = GetTranslatedAst(expression);
            var conditional = GetBodyExpression<ConditionalExpression>(jsProgram);
            var method = GetMethod(conditional?.Test);
            var regexp = (method.Object as Literal)?.Value as RegExp;

            Assert.AreEqual(pattern, regexp?.Pattern);
            Assert.AreEqual(RegExpFlags.IgnoreCase, regexp?.Flags);
            Assert.AreEqual("test", method.Name);
        }

        [TestMethod]
        public void TranslateMatch()
        {
            var pattern = @"^\d+$";
            var expression = Expression(m =>
                new Regex(pattern).Match(m.Text).Success
                    ? "fail"
                    : DryvValidationResult.Success);

            var jsProgram = GetTranslatedAst(expression);
            var conditional = GetBodyExpression<ConditionalExpression>(jsProgram);
            var method = GetMethod(conditional?.Test);
            var regexp = (method.Object as Literal)?.Value as RegExp;

            Assert.AreEqual(pattern, regexp?.Pattern);
            Assert.AreEqual(RegExpFlags.None, regexp?.Flags);
            Assert.AreEqual("test", method.Name);
        }

        [TestMethod]
        public void TranslateIsMatchWithLiteralPattern()
        {
            var expression = Expression(m => new Regex(@"^\d+$").IsMatch(m.Text) ? "fail" : DryvValidationResult.Success);
            var translation = Translate<TestModel>(expression);

            Assert.AreNotEqual(Null.Value, Evaluate(translation, "{text:'123'}"), translation);
            Assert.AreEqual(Null.Value, Evaluate(translation, "{text:'abc'}"), translation);
        }

        [TestMethod]
        public void TranslateNegatedIsMatchWithLiteralPatternAndOptions()
        {
            var expression = Expression(m => !new Regex("^abc$", RegexOptions.IgnoreCase).IsMatch(m.Text) ? "fail" : DryvValidationResult.Success);
            var translation = Translate<TestModel>(expression);

            Assert.AreEqual(Null.Value, Evaluate(translation, "{text:'ABC'}"), translation);
            Assert.AreNotEqual(Null.Value, Evaluate(translation, "{text:'xyz'}"), translation);
        }

        [TestMethod]
        public void TranslateMatchWithLiteralPattern()
        {
            var expression = Expression(m => new Regex(@"^\d+$").Match(m.Text).Success ? "fail" : DryvValidationResult.Success);
            var translation = Translate<TestModel>(expression);

            Assert.AreNotEqual(Null.Value, Evaluate(translation, "{text:'123'}"), translation);
            Assert.AreEqual(Null.Value, Evaluate(translation, "{text:'abc'}"), translation);
        }

        [TestMethod]
        public void TranslateStaticMatch()
        {
            var pattern = @"^\d+$";
            var expression = Expression(m =>
                Regex.Match(pattern, m.Text).Success
                    ? "fail"
                    : DryvValidationResult.Success);

            var jsProgram = GetTranslatedAst(expression);
            var conditional = GetBodyExpression<ConditionalExpression>(jsProgram);
            var method = GetMethod(conditional?.Test);
            var regexp = (method.Object as Literal)?.Value as RegExp;

            Assert.AreEqual(pattern, regexp?.Pattern);
            Assert.AreEqual(RegExpFlags.None, regexp?.Flags);
            Assert.AreEqual("test", method.Name);
        }

        private static object Evaluate(string translation, string model) =>
            new ScriptEngine().Evaluate($"({translation})({model})");
    }
}