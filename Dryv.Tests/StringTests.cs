using System;
using System.Linq;
using Dryv.Extensions;
using Escape.Ast;
using Jurassic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dryv.Tests
{
    [TestClass]
    public class StringTests : JavascriptTranslatorTestsBase
    {
        [TestMethod]
        public void InterpolationStrings()
        {
            var expression = Expression(m => $"123{m.Text}abc");

            var jsProgram = GetTranslatedAst(expression);
            var binaryExpression = (dynamic)GetBodyExpression<BinaryExpression>(jsProgram);
            Assert.AreEqual(BinaryOperator.Plus, binaryExpression.Operator);
            Assert.AreEqual(nameof(TestModel.Text).ToCamelCase(), binaryExpression.Left.Right.Property.Name);
        }
        
        [TestMethod]
        public void InterpolationStringsWithFormat()
        {
            var expression = Expression(m => $"{m.IntItem:D}");

            var jsProgram = GetTranslatedAst(expression);
            var callExpression = GetBodyExpression<CallExpression>(jsProgram);
            
            Assert.AreEqual("format", ((callExpression.Callee as MemberExpression)?.Property as Identifier)?.Name);
            Assert.AreEqual("intItem", ((callExpression.Arguments.First() as MemberExpression)?.Property as Identifier)?.Name);
            Assert.AreEqual("int32", (callExpression.Arguments.Skip(1).First() as Literal)?.Value);
            Assert.AreEqual("D", (callExpression.Arguments.Last() as Literal)?.Value);
        }
        
        [TestMethod]
        public void TranslateToString()
        {
            var expression = Expression(m => m.Text.ToString());

            var jsProgram = GetTranslatedAst(expression);
            var callExpression = GetBodyExpression<CallExpression>(jsProgram);
            
            Assert.AreEqual("format", ((callExpression.Callee as MemberExpression)?.Property as Identifier)?.Name);
            Assert.AreEqual("text", ((callExpression.Arguments.First() as MemberExpression)?.Property as Identifier)?.Name);
            Assert.AreEqual("string", (callExpression.Arguments.Skip(1).First() as Literal)?.Value);
        }
        
        [TestMethod]
        public void TranslateToStringWithFormat()
        {
            var expression = Expression(m => m.IntItem.ToString("D"));

            var jsProgram = GetTranslatedAst(expression);
            var callExpression = GetBodyExpression<CallExpression>(jsProgram);
            
            Assert.AreEqual("format", ((callExpression.Callee as MemberExpression)?.Property as Identifier)?.Name);
            Assert.AreEqual("intItem", ((callExpression.Arguments.First() as MemberExpression)?.Property as Identifier)?.Name);
            Assert.AreEqual("int32", (callExpression.Arguments.Skip(1).First() as Literal)?.Value);
            Assert.AreEqual("D", (callExpression.Arguments.Last() as Literal)?.Value);
        }

        [TestMethod]
        public void TranslateCompareTo()
        {
            var expression = Expression(m =>
                m.Text.CompareTo("Oscorp") == 0
                    ? "fail"
                    : DryvValidationResult.Success);

            var jsProgram = GetTranslatedAst(expression);
            var conditional = GetBodyExpression<ConditionalExpression>(jsProgram);
            var binaryExpression = conditional.Test as BinaryExpression;

            var leftMethod = GetMethod(binaryExpression?.Left);
            Assert.AreEqual("localeCompare", leftMethod.Name);
        }

        [TestMethod]
        public void TranslateEndsWithWithIgnoreCase()
        {
            var expression = Expression(m =>
                m.Text.EndsWith("xy", StringComparison.OrdinalIgnoreCase)
                    ? "fail"
                    : DryvValidationResult.Success);

            var translation = Translate<TestModel>(expression);
            var model = @"{text:'zzzzzzzXY'}";
            var engine = new ScriptEngine();
            var script = $"({translation})({model})";
            var result = engine.Evaluate(script) as string;

            Assert.IsNull(result);
        }

        [TestMethod]
        public void TranslateNotEndsWith()
        {
            var expression = Expression(m =>
                m.Text.EndsWith("xy")
                    ? "fail"
                    : DryvValidationResult.Success);

            var translation = Translate<TestModel>(expression);
            var model = @"{text:'ab'}";
            var engine = new ScriptEngine();
            var script = $"({translation})({model})";
            var result = engine.Evaluate(script);

            Assert.AreEqual(Null.Value, result);
        }

        [TestMethod]
        public void TranslateEqualsWithIgnoreCase()
        {
            var expression = Expression(m =>
                m.Text.Equals("Oscorp", StringComparison.OrdinalIgnoreCase)
                    ? "fail"
                    : DryvValidationResult.Success);

            var jsProgram = GetTranslatedAst(expression);
            var conditional = GetBodyExpression<ConditionalExpression>(jsProgram);
            var binaryExpression = conditional.Test as BinaryExpression;

            var leftMethod = GetMethod(binaryExpression?.Left);
            Assert.AreEqual("toLowerCase", leftMethod.Name);

            var rightMethod = GetMethod(binaryExpression?.Right);
            Assert.AreEqual("toLowerCase", rightMethod.Name);
        }

        [TestMethod]
        public void TranslateIndexOfWithIgnoreCase()
        {
            var expression = Expression(m =>
                m.Text.IndexOf("Oscorp", StringComparison.OrdinalIgnoreCase) == 0
                    ? "fail"
                    : DryvValidationResult.Success);

            var jsProgram = GetTranslatedAst(expression);
            var conditional = GetBodyExpression<ConditionalExpression>(jsProgram);
            var binaryExpression = conditional.Test as BinaryExpression;

            var leftMethod = GetMethod(binaryExpression?.Left);
            var leftMethod2 = GetMethod(leftMethod.Object);

            Assert.AreEqual("indexOf", leftMethod.Name);
            Assert.AreEqual("toLowerCase", leftMethod2.Name);
        }

        [TestMethod]
        public void TranslateIsNullOrWhiteSpace()
        {
            var expression = Expression(m =>
                string.IsNullOrWhiteSpace(m.Text)
                    ? "fail"
                    : DryvValidationResult.Success);

            var jsProgram = GetTranslatedAst(expression);
            var conditional = (dynamic)GetBodyExpression<ConditionalExpression>(jsProgram);
            var unaryExpression = conditional.Test;
            var callExpression = unaryExpression.Argument;

            Assert.AreEqual(UnaryOperator.LogicalNot, unaryExpression.Operator);
            Assert.AreEqual("test", callExpression.Callee.Property.Name);
            Assert.AreEqual(@"/\S/", callExpression.Callee.Object.Raw);

            var logicalExpression = callExpression.Arguments[0];

            Assert.AreEqual(LogicalOperator.LogicalOr, logicalExpression.Operator);
            Assert.AreEqual(nameof(TestModel.Text).ToCamelCase(), logicalExpression.Left.Property.Name);
            Assert.AreEqual(string.Empty, logicalExpression.Right.Value);
        }

        [TestMethod]
        public void TranslateStartsWithWithIgnoreCase()
        {
            var expression = Expression(m =>
                m.Text.StartsWith("Oscorp", StringComparison.OrdinalIgnoreCase)
                    ? "fail"
                    : DryvValidationResult.Success);

            var jsProgram = GetTranslatedAst(expression);
            var conditional = GetBodyExpression<ConditionalExpression>(jsProgram);
            var binaryExpression = conditional.Test as BinaryExpression;

            var leftMethod = GetMethod(binaryExpression?.Left);
            var leftMethod2 = GetMethod(leftMethod.Object);

            Assert.AreEqual("indexOf", leftMethod.Name);
            Assert.AreEqual("toLowerCase", leftMethod2.Name);
        }

        [TestMethod]
        public void TranslateStaticCompareTo()
        {
            var expression = Expression(m =>
                string.Compare(m.Text, "Oscorp", StringComparison.CurrentCulture) == 0
                    ? "fail"
                    : DryvValidationResult.Success);

            var jsProgram = GetTranslatedAst(expression);
            var conditional = GetBodyExpression<ConditionalExpression>(jsProgram);
            var binaryExpression = conditional.Test as BinaryExpression;

            var leftMethod = GetMethod(binaryExpression?.Left);
            Assert.AreEqual("localeCompare", leftMethod.Name);
        }

        [TestMethod]
        public void TranslateStaticCompareToCaseInsensitive()
        {
            var expression = Expression(m =>
                string.Compare(m.Text, "Oscorp", StringComparison.OrdinalIgnoreCase) == 0
                    ? "fail"
                    : DryvValidationResult.Success);

            var jsProgram = GetTranslatedAst(expression);
            var conditional = GetBodyExpression<ConditionalExpression>(jsProgram);
            var binaryExpression = conditional.Test as BinaryExpression;

            var leftMethod = GetMethod(binaryExpression?.Left);
            var leftMethod2 = GetMethod(leftMethod.Object);

            Assert.AreEqual("toLowerCase", leftMethod2.Name);
            Assert.AreEqual("localeCompare", leftMethod.Name);
        }
    }
}