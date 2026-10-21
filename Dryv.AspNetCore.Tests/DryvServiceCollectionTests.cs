using System;
using System.Linq.Expressions;
using Dryv.Configuration;
using Dryv.Rules;
using Dryv.Translation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dryv.AspNetCore.Tests
{
    [TestClass]
    public class DryvServiceCollectionTests
    {
        [TestMethod]
        public void Translators_AcceptsIDryvMethodCallTranslator()
        {
            var options = new DryvOptions();
            options.Translators.Add<TestMethodCallTranslator>();

            Assert.AreEqual(1, options.Translators.Count());
        }

        [TestMethod]
        public void Translators_AcceptsIDryvCustomTranslator()
        {
            var options = new DryvOptions();
            options.Translators.Add<TestCustomTranslator>();

            Assert.AreEqual(1, options.Translators.Count());
        }

        [TestMethod]
        public void Translators_RejectsInvalidType()
        {
            var options = new DryvOptions();
            var thrown = false;
            try { options.Translators.Add(typeof(string)); }
            catch (ArgumentException) { thrown = true; }
            Assert.IsTrue(thrown, "Expected ArgumentException for invalid translator type.");
        }

        [TestMethod]
        public void Annotators_AcceptsIDryvRuleAnnotator()
        {
            var options = new DryvOptions();
            options.Annotators.Add<TestAnnotator>();

            Assert.AreEqual(1, options.Annotators.Count());
        }

        [TestMethod]
        public void Annotators_RejectsInvalidType()
        {
            var options = new DryvOptions();
            var thrown = false;
            try { options.Annotators.Add(typeof(string)); }
            catch (ArgumentException) { thrown = true; }
            Assert.IsTrue(thrown, "Expected ArgumentException for invalid annotator type.");
        }

        [TestMethod]
        public void NoConstraints_AcceptsAnyType()
        {
            var collection = new DryvServiceCollection();
            collection.Add(typeof(string));

            Assert.AreEqual(1, collection.Count());
        }

        private class TestMethodCallTranslator : IDryvMethodCallTranslator
        {
            public bool SupportsType(Type type) => false;
            public bool Translate(MethodTranslationContext context) => false;
        }

        private class TestCustomTranslator : IDryvCustomTranslator
        {
            public bool? AllowSurroundingBrackets(Expression expression) => null;
            public bool TryTranslate(CustomTranslationContext context) => false;
        }

        private class TestAnnotator : IDryvRuleAnnotator
        {
            public void Annotate(DryvRuleDefinition rule, LambdaExpression expression) { }
        }
    }

    internal static class EnumerableExtensions
    {
        public static int Count(this DryvServiceCollection collection)
        {
            var count = 0;
            foreach (var _ in collection) count++;
            return count;
        }
    }
}
