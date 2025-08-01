using System;
using System.Text.RegularExpressions;
using Dryv.Translation;
using Escape;
using Escape.Ast;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dryv.Tests
{
    [TestClass]
    public class JavascriptTranslatorTests : JavascriptTranslatorTestsBase
    {
        private readonly string patternField = @"^\d+$";
        private readonly Regex regexField = new Regex(@"^\d+$");
        private readonly string var2Field = "fail";

        private string PatternProperty => patternField;
        private Regex RegexProperty => regexField;
        private string Var2Property => var2Field;


        [TestMethod]
        public void InlineLambdaExpreession()
        {
            var expression = Expression(m => ((Func<TestModel, string>)(m2 => m2.Text))(m));
            var translation = Translate<TestModel>(expression);

            Assert.IsNotNull(translation);
        }


        [TestMethod]
        public void NonInlineLambdaExpreession()
        {
            var x = (Func<TestModel, string>)(m2 => m2.Text);
            var expression = Expression(m => x(m));
            try
            {
                var translation = Translate<TestModel>(expression);
                Assert.Fail();
            }
            catch (DryvExpressionNotSupportedException)
            {
            }
        }

        [TestMethod]
        public void GetArgumentFromField()
        {
            var expression = Expression(m =>
                new Regex(patternField, RegexOptions.IgnoreCase | RegexOptions.Singleline).IsMatch(m.Text)
                    ? "fail"
                    : DryvValidationResult.Success);

            var jsProgram = GetTranslatedAst(expression);
            var conditional = GetBodyExpression<ConditionalExpression>(jsProgram);
            var method = GetMethod(conditional?.Test);
            var regexp = (method.Object as Literal)?.Value as RegExp;

            Assert.AreEqual(patternField, regexp?.Pattern);
        }

        [TestMethod]
        public void GetArgumentFromFieldWhereOtherFieldExists()
        {
            var expression = Expression(m =>
                new Regex(patternField, RegexOptions.IgnoreCase | RegexOptions.Singleline).IsMatch(m.Text)
                    ? var2Field
                    : DryvValidationResult.Success);

            var jsProgram = GetTranslatedAst(expression);
            var conditional = GetBodyExpression<ConditionalExpression>(jsProgram);
            var method = GetMethod(conditional?.Test);
            var regexp = (method.Object as Literal)?.Value as RegExp;

            Assert.AreEqual(patternField, regexp?.Pattern);
        }

        [TestMethod]
        public void GetArgumentFromProperty()
        {
            var expression = Expression(m =>
                new Regex(PatternProperty, RegexOptions.IgnoreCase | RegexOptions.Singleline).IsMatch(m.Text)
                    ? "fail"
                    : DryvValidationResult.Success);

            var jsProgram = GetTranslatedAst(expression);
            var conditional = GetBodyExpression<ConditionalExpression>(jsProgram);
            var method = GetMethod(conditional?.Test);
            var regexp = (method.Object as Literal)?.Value as RegExp;

            Assert.AreEqual(PatternProperty, regexp?.Pattern);
        }

        [TestMethod]
        public void GetArgumentFromPropertyWhereOtherPropertyExists()
        {
            var expression = Expression(m =>
                new Regex(PatternProperty, RegexOptions.IgnoreCase | RegexOptions.Singleline).IsMatch(m.Text)
                    ? Var2Property
                    : DryvValidationResult.Success);

            var jsProgram = GetTranslatedAst(expression);
            var conditional = GetBodyExpression<ConditionalExpression>(jsProgram);
            var method = GetMethod(conditional?.Test);
            var regexp = (method.Object as Literal)?.Value as RegExp;

            Assert.AreEqual(PatternProperty, regexp?.Pattern);
        }

        [TestMethod]
        public void GetArgumentFromVariable()
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
        }

        [TestMethod]
        public void GetArgumentFromVariableWhereOtherVariableExists()
        {
            var pattern = @"^\d+$";
            var var2 = "fail";
            var expression = Expression(m =>
                new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline).IsMatch(m.Text)
                    ? var2
                    : DryvValidationResult.Success);

            var jsProgram = GetTranslatedAst(expression);
            var conditional = GetBodyExpression<ConditionalExpression>(jsProgram);
            var method = GetMethod(conditional?.Test);
            var regexp = (method.Object as Literal)?.Value as RegExp;

            Assert.AreEqual(pattern, regexp?.Pattern);
        }

        [TestMethod]
        public void GetObjectFromField()
        {
            var expression = Expression(m =>
                regexField.IsMatch(m.Text)
                    ? "fail"
                    : DryvValidationResult.Success);

            var jsProgram = GetTranslatedAst(expression);
            var conditional = GetBodyExpression<ConditionalExpression>(jsProgram);
            var method = GetMethod(conditional?.Test);
            var regexp = (method.Object as Literal)?.Value as RegExp;

            Assert.AreEqual(regexField.ToString(), regexp?.Pattern);
        }

        [TestMethod]
        public void GetObjectFromFieldWhereOtherFieldExists()
        {
            var expression = Expression(m =>
                regexField.IsMatch(m.Text)
                    ? var2Field
                    : DryvValidationResult.Success);

            var jsProgram = GetTranslatedAst(expression);
            var conditional = GetBodyExpression<ConditionalExpression>(jsProgram);
            var method = GetMethod(conditional?.Test);
            var regexp = (method.Object as Literal)?.Value as RegExp;

            Assert.AreEqual(regexField.ToString(), regexp?.Pattern);
        }

        [TestMethod]
        public void GetObjectFromProperty()
        {
            var expression = Expression(m =>
                RegexProperty.IsMatch(m.Text)
                    ? "fail"
                    : DryvValidationResult.Success);

            var jsProgram = GetTranslatedAst(expression);
            var conditional = GetBodyExpression<ConditionalExpression>(jsProgram);
            var method = GetMethod(conditional?.Test);
            var regexp = (method.Object as Literal)?.Value as RegExp;

            Assert.AreEqual(RegexProperty.ToString(), regexp?.Pattern);
        }

        [TestMethod]
        public void GetObjectFromPropertyWhereOtherPropertyExists()
        {
            var expression = Expression(m =>
                RegexProperty.IsMatch(m.Text)
                    ? Var2Property
                    : DryvValidationResult.Success);

            var jsProgram = GetTranslatedAst(expression);
            var conditional = GetBodyExpression<ConditionalExpression>(jsProgram);
            var method = GetMethod(conditional?.Test);
            var regexp = (method.Object as Literal)?.Value as RegExp;

            Assert.AreEqual(RegexProperty.ToString(), regexp?.Pattern);
        }

        [TestMethod]
        public void GetObjectFromVariable()
        {
            var pattern = @"^\d+$";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var expression = Expression(m =>
                 regex.IsMatch(m.Text)
                    ? "fail"
                    : DryvValidationResult.Success);

            var jsProgram = GetTranslatedAst(expression);
            var conditional = GetBodyExpression<ConditionalExpression>(jsProgram);
            var method = GetMethod(conditional?.Test);
            var regexp = (method.Object as Literal)?.Value as RegExp;

            Assert.AreEqual(pattern, regexp?.Pattern);
        }

        [TestMethod]
        public void GetObjectFromVariableWhereOtherVariableExists()
        {
            var pattern = @"^\d+$";
            var var2 = "fail";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var expression = Expression(m =>
                regex.IsMatch(m.Text)
                    ? var2
                    : DryvValidationResult.Success);

            var jsProgram = GetTranslatedAst(expression);
            var conditional = GetBodyExpression<ConditionalExpression>(jsProgram);
            var method = GetMethod(conditional?.Test);
            var regexp = (method.Object as Literal)?.Value as RegExp;

            Assert.AreEqual(pattern, regexp?.Pattern);
        }

        [TestMethod]
        public void PreevaluateStaticMethod()
        {
            var expression = Expression(model => GetText());
            var script = Translate<TestModel>(expression);

            var engine = new Jurassic.ScriptEngine();
            var result = engine.Evaluate($"({script})()");

            Assert.AreEqual("Text", result);
        }

        private static string GetText() => "Text";
    }
}