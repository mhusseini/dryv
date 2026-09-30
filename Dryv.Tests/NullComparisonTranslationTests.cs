using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jurassic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dryv.Tests
{
    [TestClass]
    [SuppressMessage("ReSharper", "NegativeEqualityExpression")]
    [SuppressMessage("ReSharper", "LocalizableElement")]
    public class NullComparisonTranslationTests : JavascriptTranslatorTestsBase
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        private const string NullCheck = "(m.value == null || m.value === \"\")";
        private const string NotNullCheck = "(m.value != null && m.value !== \"\")";
        private const string EmptyStringModel = @"{""value"":""""}";

        private static readonly DateTime SomeDate = new(2020, 2, 29, 13, 37, 0);
        private static readonly Guid SomeGuid = Guid.Parse("6f9619ff-8b86-d011-b42d-00c04fc964ff");

        [TestMethod]
        public void NullableValueTypeComparedToNullIsTranslatedToNullCheck()
        {
            AssertTranslation<bool?>(m => m.Value == null, NullCheck);
            AssertTranslation<bool?>(m => null == m.Value, NullCheck);
            AssertTranslation<bool?>(m => m.Value != null, NotNullCheck);
            AssertTranslation<bool?>(m => null != m.Value, NotNullCheck);
            AssertTranslation<bool?>(m => !(m.Value == null), NotNullCheck);
            AssertTranslation<bool?>(m => !(m.Value != null), NullCheck);
            AssertTranslation<int?>(m => m.Value == null, NullCheck);
            AssertTranslation<TestEnum?>(m => m.Value == null, NullCheck);
            AssertTranslation<DateTime?>(m => m.Value == null, NullCheck);
        }

        [TestMethod]
        public void NullableValueTypeNullCheckTreatsEmptyStringAsNull()
        {
            Assert.IsTrue(EvaluateJavaScript(Translate<Model<int?>>(Condition<int?>(m => m.Value == null)), EmptyStringModel));
            Assert.IsFalse(EvaluateJavaScript(Translate<Model<int?>>(Condition<int?>(m => m.Value != null)), EmptyStringModel));
            Assert.IsTrue(EvaluateJavaScript(Translate<Model<bool?>>(Condition<bool?>(m => m.Value == null)), EmptyStringModel));
            Assert.IsFalse(EvaluateJavaScript(Translate<Model<bool?>>(Condition<bool?>(m => !(m.Value == null))), EmptyStringModel));
            Assert.IsTrue(EvaluateJavaScript(Translate<Model<DateTime?>>(Condition<DateTime?>(m => m.Value == null)), EmptyStringModel));
            Assert.IsTrue(EvaluateJavaScript(Translate<Model<TestEnum?>>(Condition<TestEnum?>(m => m.Value == null)), EmptyStringModel));
        }

        [TestMethod]
        public void NullableValueTypeNullCheckTreatsMissingPropertyAsNull()
        {
            Assert.IsTrue(EvaluateJavaScript(Translate<Model<bool?>>(Condition<bool?>(m => m.Value == null)), "{}"));
            Assert.IsFalse(EvaluateJavaScript(Translate<Model<bool?>>(Condition<bool?>(m => m.Value != null)), "{}"));
            Assert.IsTrue(EvaluateJavaScript(Translate<Model<int?>>(Condition<int?>(m => m.Value == null)), "{}"));
            Assert.IsFalse(EvaluateJavaScript(Translate<Model<int?>>(Condition<int?>(m => m.Value != null)), "{}"));
        }

        [TestMethod]
        public void NullableBooleanComparisons()
        {
            var values = new bool?[] { null, false, true };

            AssertSameResults(m => m.Value == null, values);
            AssertSameResults(m => m.Value != null, values);
            AssertSameResults(m => null == m.Value, values);
            AssertSameResults(m => null != m.Value, values);
            AssertSameResults(m => !(m.Value == null), values);
            AssertSameResults(m => !(m.Value != null), values);
            AssertSameResults(m => m.Value == false, values);
            AssertSameResults(m => m.Value != false, values);
            AssertSameResults(m => m.Value == true, values);
            AssertSameResults(m => m.Value != true, values);
            AssertSameResults(m => m.Value == null || m.Value == false, values);
            AssertSameResults(m => m.Value != null && m.Value == false, values);
        }

        [TestMethod]
        public void NullableIntegerComparisons()
        {
            var values = new int?[] { null, 0, 1, -1 };

            AssertSameResults(m => m.Value == null, values);
            AssertSameResults(m => m.Value != null, values);
            AssertSameResults(m => null == m.Value, values);
            AssertSameResults(m => !(m.Value == null), values);
            AssertSameResults(m => m.Value == 0, values);
            AssertSameResults(m => m.Value != 0, values);
            AssertSameResults(m => m.Value == null || m.Value == 0, values);
            AssertSameResults(m => m.Value != null && m.Value != 0, values);
        }

        [TestMethod]
        public void NullableLongComparisons()
        {
            var values = new long?[] { null, 0L, 1L };

            AssertSameResults(m => m.Value == null, values);
            AssertSameResults(m => m.Value != null, values);
            AssertSameResults(m => m.Value == 0L, values);
            AssertSameResults(m => m.Value != 0L, values);
        }

        [TestMethod]
        public void NullableDecimalComparisons()
        {
            var values = new decimal?[] { null, 0m, 1m };

            AssertSameResults(m => m.Value == null, values);
            AssertSameResults(m => m.Value != null, values);
            AssertSameResults(m => m.Value == 0m, values);
            AssertSameResults(m => m.Value != 0m, values);
        }

        [TestMethod]
        public void NullableDoubleComparisons()
        {
            var values = new double?[] { null, 0d, 1.5d };

            AssertSameResults(m => m.Value == null, values);
            AssertSameResults(m => m.Value != null, values);
            AssertSameResults(m => m.Value == 0d, values);
            AssertSameResults(m => m.Value != 0d, values);
        }

        [TestMethod]
        public void NullableEnumComparisons()
        {
            var values = new TestEnum?[] { null, TestEnum.First, TestEnum.Second };

            AssertSameResults(m => m.Value == null, values);
            AssertSameResults(m => m.Value != null, values);
            AssertSameResults(m => null == m.Value, values);
            AssertSameResults(m => null != m.Value, values);
            AssertSameResults(m => m.Value == TestEnum.First, values);
            AssertSameResults(m => m.Value != TestEnum.First, values);
        }

        [TestMethod]
        public void NullableEnumSerializedAsNumberComparedToNull()
        {
            var isNull = Translate<Model<TestEnum?>>(Condition<TestEnum?>(m => m.Value == null));
            var isNotNull = Translate<Model<TestEnum?>>(Condition<TestEnum?>(m => m.Value != null));

            Assert.IsTrue(EvaluateJavaScript(isNull, @"{""value"":null}"), isNull);
            Assert.IsFalse(EvaluateJavaScript(isNull, @"{""value"":0}"), isNull);
            Assert.IsFalse(EvaluateJavaScript(isNotNull, @"{""value"":null}"), isNotNull);
            Assert.IsTrue(EvaluateJavaScript(isNotNull, @"{""value"":0}"), isNotNull);
        }

        [TestMethod]
        public void NullableDateTimeComparedToNull()
        {
            var values = new DateTime?[] { null, default(DateTime), SomeDate };

            AssertSameResults(m => m.Value == null, values);
            AssertSameResults(m => m.Value != null, values);
            AssertSameResults(m => null == m.Value, values);
        }

        [TestMethod]
        public void NullableGuidComparisons()
        {
            var values = new Guid?[] { null, Guid.Empty, SomeGuid };

            AssertSameResults(m => m.Value == null, values);
            AssertSameResults(m => m.Value != null, values);
            AssertSameResults(m => m.Value == Guid.Empty, values);
            AssertSameResults(m => m.Value != Guid.Empty, values);
            AssertSameResults(m => m.Value == SomeGuid, values);
        }

#pragma warning disable CS0472, CS8073
        [TestMethod]
        public void BooleanComparisons()
        {
            var values = new[] { false, true };

            AssertSameResults(m => m.Value == null, values);
            AssertSameResults(m => m.Value != null, values);
            AssertSameResults(m => null == m.Value, values);
            AssertSameResults(m => m.Value == false, values);
            AssertSameResults(m => m.Value != false, values);
            AssertSameResults(m => m.Value == true, values);
        }

        [TestMethod]
        public void IntegerComparisons()
        {
            var values = new[] { 0, 1, -1 };

            AssertSameResults(m => m.Value == null, values);
            AssertSameResults(m => m.Value != null, values);
            AssertSameResults(m => null != m.Value, values);
            AssertSameResults(m => m.Value == 0, values);
            AssertSameResults(m => m.Value != 0, values);
        }

        [TestMethod]
        public void DecimalComparisons()
        {
            var values = new[] { 0m, 1m };

            AssertSameResults(m => m.Value == null, values);
            AssertSameResults(m => m.Value != null, values);
            AssertSameResults(m => m.Value == 0m, values);
            AssertSameResults(m => m.Value != 0m, values);
        }

        [TestMethod]
        public void DoubleComparisons()
        {
            var values = new[] { 0d, 1.5d };

            AssertSameResults(m => m.Value == null, values);
            AssertSameResults(m => m.Value != null, values);
            AssertSameResults(m => m.Value == 0d, values);
            AssertSameResults(m => m.Value != 0d, values);
        }

        [TestMethod]
        public void EnumComparisons()
        {
            var values = new[] { TestEnum.First, TestEnum.Second };

            AssertSameResults(m => m.Value == null, values);
            AssertSameResults(m => m.Value != null, values);
            AssertSameResults(m => m.Value == TestEnum.First, values);
            AssertSameResults(m => m.Value != TestEnum.First, values);
        }

        [TestMethod]
        public void DateTimeComparedToNull()
        {
            var values = new[] { default(DateTime), SomeDate };

            AssertSameResults(m => m.Value == null, values);
            AssertSameResults(m => m.Value != null, values);
        }

        [TestMethod]
        public void GuidComparisons()
        {
            var values = new[] { Guid.Empty, SomeGuid };

            AssertSameResults(m => m.Value == null, values);
            AssertSameResults(m => m.Value != null, values);
            AssertSameResults(m => m.Value == Guid.Empty, values);
            AssertSameResults(m => m.Value != Guid.Empty, values);
        }
#pragma warning restore CS0472, CS8073

        [TestMethod]
        public void ReferenceTypeComparedToNull()
        {
            var values = new[] { null, "text" };

            AssertSameResults(m => m.Value == null, values);
            AssertSameResults(m => m.Value != null, values);
            AssertSameResults(m => null == m.Value, values);
            AssertSameResults(m => null != m.Value, values);
            AssertSameResults(m => !(m.Value == null), values);
        }

        [TestMethod]
        public void NullableValueTypeCoalesce()
        {
            AssertSameResults<int?>(m => (m.Value ?? 5) == 0, null, 0, 1, 5);
            AssertSameResults<int?>(m => (m.Value ?? 5) == 5, null, 0, 1, 5);
            AssertSameResults<decimal?>(m => (m.Value ?? 1m) == 0m, null, 0m, 1m);
            AssertSameResults<double?>(m => (m.Value ?? 1d) == 0d, null, 0d, 1d);
            AssertSameResults<bool?>(m => m.Value ?? true, null, false, true);
            AssertSameResults<bool?>(m => (m.Value ?? true) == false, null, false, true);
            AssertSameResults<bool?>(m => !(m.Value ?? true), null, false, true);
            AssertSameResults<TestEnum?>(m => (m.Value ?? TestEnum.Second) == TestEnum.Second, null, TestEnum.First, TestEnum.Second);
        }

        [TestMethod]
        public void NullableValueTypeCoalesceTreatsEmptyStringAsNull()
        {
            Assert.IsTrue(EvaluateJavaScript(Translate<Model<int?>>(Condition<int?>(m => (m.Value ?? 5) == 5)), EmptyStringModel));
            Assert.IsTrue(EvaluateJavaScript(Translate<Model<bool?>>(Condition<bool?>(m => m.Value ?? true)), EmptyStringModel));
        }

        [TestMethod]
        public void ReferenceTypeCoalesce()
        {
            AssertSameResults<string>(m => (m.Value ?? "fallback") == "fallback", null, "text");
        }

        [TestMethod]
        public void HasValue()
        {
            AssertSameResults<bool?>(m => m.Value.HasValue, null, false, true);
            AssertSameResults<bool?>(m => !m.Value.HasValue, null, false, true);
            AssertSameResults<int?>(m => m.Value.HasValue, null, 0, 1);
            AssertSameResults<int?>(m => !m.Value.HasValue, null, 0, 1);
            AssertSameResults<decimal?>(m => m.Value.HasValue, null, 0m, 1m);
            AssertSameResults<DateTime?>(m => m.Value.HasValue, null, default(DateTime), SomeDate);
            AssertSameResults<TestEnum?>(m => m.Value.HasValue, null, TestEnum.First, TestEnum.Second);
            AssertSameResults<Guid?>(m => m.Value.HasValue, null, Guid.Empty, SomeGuid);
        }

        [TestMethod]
        public void HasValueTreatsEmptyStringAndMissingPropertyAsNull()
        {
            var translation = Translate<Model<int?>>(Condition<int?>(m => m.Value.HasValue));

            Assert.IsFalse(EvaluateJavaScript(translation, EmptyStringModel), translation);
            Assert.IsFalse(EvaluateJavaScript(translation, "{}"), translation);
        }

        [TestMethod]
        public void Value()
        {
            AssertSameResults<bool?>(m => m.Value.Value, false, true);
            AssertSameResults<bool?>(m => !m.Value.Value, false, true);
            AssertSameResults<bool?>(m => m.Value.HasValue && m.Value.Value, null, false, true);
            AssertSameResults<int?>(m => m.Value.Value == 0, 0, 1);
            AssertSameResults<int?>(m => m.Value.HasValue && m.Value.Value > 0, null, 0, 1);
            AssertSameResults<TestEnum?>(m => m.Value.Value == TestEnum.First, TestEnum.First, TestEnum.Second);
            AssertSameResults<Guid?>(m => m.Value.Value == Guid.Empty, Guid.Empty, SomeGuid);
        }

        [TestMethod]
        public void GetValueOrDefault()
        {
            AssertSameResults<int?>(m => m.Value.GetValueOrDefault() == 0, null, 0, 1);
            AssertSameResults<int?>(m => m.Value.GetValueOrDefault(5) == 5, null, 0, 5);
            AssertSameResults<bool?>(m => m.Value.GetValueOrDefault(), null, false, true);
            AssertSameResults<bool?>(m => !m.Value.GetValueOrDefault(true), null, false, true);
            AssertSameResults<decimal?>(m => m.Value.GetValueOrDefault() == 0m, null, 0m, 1m);
            AssertSameResults<TestEnum?>(m => m.Value.GetValueOrDefault() == TestEnum.First, null, TestEnum.First, TestEnum.Second);
            AssertSameResults<TestEnum?>(m => m.Value.GetValueOrDefault(TestEnum.Second) == TestEnum.Second, null, TestEnum.First, TestEnum.Second);
            AssertSameResults<Guid?>(m => m.Value.GetValueOrDefault() == Guid.Empty, null, Guid.Empty, SomeGuid);
        }

        [TestMethod]
        public void GetValueOrDefaultTreatsEmptyStringAsNull()
        {
            var translation = Translate<Model<int?>>(Condition<int?>(m => m.Value.GetValueOrDefault(5) == 5));

            Assert.IsTrue(EvaluateJavaScript(translation, EmptyStringModel), translation);
        }

        [TestMethod]
        public void ValidationRuleWithNullableBooleanNullCheck()
        {
            var translation = Translate<Model<bool?>>(Expression<Model<bool?>>(m => m.Value == null ? "fail" : null));

            Assert.AreNotEqual(Null.Value, Evaluate(translation, @"{""value"":null}"), translation);
            Assert.AreEqual(Null.Value, Evaluate(translation, @"{""value"":false}"), translation);
            Assert.AreEqual(Null.Value, Evaluate(translation, @"{""value"":true}"), translation);
        }

        private static System.Linq.Expressions.Expression<Func<Model<T>, bool>> Condition<T>(System.Linq.Expressions.Expression<Func<Model<T>, bool>> condition) =>
            condition;

        private static void AssertTranslation<T>(System.Linq.Expressions.Expression<Func<Model<T>, bool>> condition, string expected)
        {
            var translation = Translate<Model<T>>(condition);

            StringAssert.Contains(translation, expected, $"Unexpected translation of '{condition.Body}'.");
        }

        private static void AssertSameResults<T>(System.Linq.Expressions.Expression<Func<Model<T>, bool>> condition, params T[] values)
        {
            var translation = Translate<Model<T>>(condition);
            var evaluate = condition.Compile();

            foreach (var value in values)
            {
                var model = new Model<T> { Value = value };
                var json = JsonSerializer.Serialize(model, SerializerOptions);

                Assert.AreEqual(evaluate(model), EvaluateJavaScript(translation, json),
                    $"'{condition.Body}' evaluates differently in JavaScript for model {json}. Translation: {translation}");
            }
        }

        private static bool EvaluateJavaScript(string translation, string model) =>
            (bool)new ScriptEngine().Evaluate($"!!(({translation})({model}))");

        private static object Evaluate(string translation, string model) =>
            new ScriptEngine().Evaluate($"({translation})({model})");

        private enum TestEnum
        {
            First,
            Second
        }

        private class Model<T>
        {
            public T Value { get; set; }
        }
    }
}
