using System.Linq;
using Dryv.Configuration;
using Dryv.Extensions;
using Dryv.Rules;
using Dryv.Translation.Translators;
using Escape.Ast;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dryv.Tests
{
    [TestClass]
    public class DryvParametersTranslatorTests : JavascriptTranslatorTestsBase
    {
        [TestMethod]
        public void Get_WithConstantArgument_TranslatesCorrectly()
        {
            // Arrange
            var parameterName = "testParameter";
            var expression = Expression(m =>
                m.Text == new DryvParameters(null).Get<string>(parameterName)
                    ? DryvValidationResult.Success
                    : DryvValidationResult.Error("Parameter value doesn't match".ToFormattedString()));

            // Act
            var jsProgram = GetTranslatedAst(expression, [new DryvParametersTranslator(new DryvOptions())]);
            var conditional = GetBodyExpression<ConditionalExpression>(jsProgram);
            var binaryExpression = conditional.Test as BinaryExpression;
            var rightSide = binaryExpression?.Right as CallExpression;

            // Assert
            Assert.IsNotNull(binaryExpression, "Binary expression not found");
            Assert.AreEqual(BinaryOperator.StrictlyEqual, binaryExpression.Operator, "Expected strict equality operator");

            // Check the right side of the equality (should be $ctx.parameter('testParameter'))
            Assert.IsNotNull(rightSide, "Call expression not found");
            var memberExpr = rightSide.Callee as MemberExpression;
            Assert.IsNotNull(memberExpr, "Member expression not found");

            var objectExpr = memberExpr.Object as Identifier;
            Assert.IsNotNull(objectExpr, "Object expression not found");
            Assert.AreEqual("$ctx", objectExpr.Name, "Expected $ctx object");

            var propertyExpr = memberExpr.Property as Identifier;
            Assert.IsNotNull(propertyExpr, "Property expression not found");
            Assert.AreEqual("parameter", propertyExpr.Name, "Expected parameter method");

            // Check the argument is our parameter name
            Assert.AreEqual(1, rightSide.Arguments.Count(), "Expected one argument");
            var argument = rightSide.Arguments.First() as Literal;
            Assert.IsNotNull(argument, "Argument should be a literal");
            Assert.AreEqual($"\"{parameterName}\"", argument.Raw, "Parameter name doesn't match");
        }
    }
}