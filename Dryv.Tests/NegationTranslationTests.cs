using System;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dryv.Rules;
using Dryv.Translation.Translators;
using Jurassic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dryv.Tests
{
    [TestClass]
    public class NegationTranslationTests : JavascriptTranslatorTestsBase
    {
        private const string Context = "{dryv:{parseDate:function(value){return new Date(value);}}}";

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static readonly Model[] Models =
        [
            new() { Number = 0, Text = "", Start = new DateTime(2020, 1, 1), End = new DateTime(2020, 1, 1), Items = [], Flags = [false, true] },
            new() { Flag = true, Number = 5, NullableFlag = false, Text = "abc", Start = new DateTime(2021, 6, 15), End = new DateTime(2020, 1, 1), Items = ["a"], Flags = [true, false] },
            new() { Other = true, Number = -1, NullableFlag = true, Text = "xyz", Start = new DateTime(2019, 12, 31), End = new DateTime(2020, 1, 1), Items = ["b", "c"], Flags = [true, true] },
            new() { Flag = true, Other = true, Number = 10, NullableFlag = true, Text = "a", Start = new DateTime(2020, 1, 2), End = new DateTime(2020, 1, 1), Items = ["a", "b"], Flags = [false, false] }
        ];

        [TestMethod]
        public void NegatedComparisons()
        {
            AssertSameResults(m => !(m.Number == 0));
            AssertSameResults(m => !(m.Number != 0));
            AssertSameResults(m => !(m.Number > 0));
            AssertSameResults(m => !(m.Number >= 5));
            AssertSameResults(m => !(m.Number < 0));
            AssertSameResults(m => !(m.Number <= 5));
            AssertSameResults(m => !(m.Text == "abc"));
            AssertSameResults(m => !(m.NullableFlag == true));
            AssertSameResults(m => !(m.Start > m.End));
        }

        [TestMethod]
        public void NegatedBinaryExpressionIsTranslatedToNegation()
        {
            StringAssert.Contains(Translate<Model>(Condition(m => !(m.Number == 0))), "!(m.number===0)");
        }

        [TestMethod]
        public void NegatedLogicalExpressions()
        {
            AssertSameResults(m => !(m.Flag && m.Other));
            AssertSameResults(m => !(m.Flag || m.Other));
            AssertSameResults(m => !(m.Flag && m.Number > 0));
            AssertSameResults(m => !m.Flag || !m.Other);
            AssertSameResults(m => !(m.Flag ? m.Other : m.Number > 0));
        }

        [TestMethod]
        public void DoubleNegations()
        {
            AssertSameResults(m => !!m.Flag);
            AssertSameResults(m => !!(m.Number > 0));
            AssertSameResults(m => !(!m.Flag && m.Other));
        }

        [TestMethod]
        public void NegatedNullComparisons()
        {
            AssertSameResults(m => !(m.NullableFlag == null));
            AssertSameResults(m => !(m.NullableFlag != null));
        }

        [TestMethod]
        public void NegatedEnumerableMethodCalls()
        {
            AssertSameResults(m => !m.Items.Any(x => x == "a"));
            AssertSameResults(m => !m.Items.All(x => x == "a"));
            AssertSameResults(m => !m.Items.Contains("a"));
            AssertSameResults(m => m.Items.Any(x => !(x == "a")));
        }

        [TestMethod]
        public void NegatedEnumerableElements()
        {
            AssertSameResults(m => !m.Flags.First());
            AssertSameResults(m => !m.Flags.FirstOrDefault(x => x));
            AssertSameResults(m => !m.Flags.Last());
            AssertSameResults(m => !m.Flags.LastOrDefault());
            AssertSameResults(m => !m.Flags.ElementAt(1));
            AssertSameResults(m => !m.Flags.ElementAtOrDefault(1));
            AssertSameResults(m => !m.Flags.Max(x => x));
            AssertSameResults(m => !m.Flags.Min(x => x));
        }

        [TestMethod]
        public void NegatedStringMethodCalls()
        {
            AssertSameResults(m => !m.Text.Contains("a"));
            AssertSameResults(m => !m.Text.StartsWith("a"));
            AssertSameResults(m => !string.IsNullOrEmpty(m.Text));
        }

        [TestMethod]
        public void NegatedRegularExpressions()
        {
            var pattern = "^a";

            AssertSameResults(m => !new Regex(pattern, RegexOptions.None).IsMatch(m.Text));
            AssertSameResults(m => !new Regex(pattern, RegexOptions.None).Match(m.Text).Success);
        }

        [TestMethod]
        public void NegatedInjectedStaticMethodCall()
        {
            AssertSameResults(m => !IsFeatureEnabled());
            AssertSameResults(m => m.Flag || !IsFeatureEnabled());
        }

        [TestMethod]
        public void NegatedParameterIsTranslatedToNegation()
        {
            var translation = Translate<Model>(Condition(m => !new DryvParameters(null).Get<bool>("flag")));

            StringAssert.Contains(translation, "!$ctx.parameter(\"flag\")");
        }

        [TestMethod]
        public void NegatedArbitraryMethodCallIsTranslatedToNegation()
        {
            var translation = Translate<Model>(Condition(m => !new Helper().IsValid(m.Text)), [new AllMethodCallTranslator()]);

            StringAssert.Contains(translation, "!{}.isValid(m.text)");
        }

        private static System.Linq.Expressions.Expression<Func<Model, bool>> Condition(System.Linq.Expressions.Expression<Func<Model, bool>> condition) =>
            condition;

        private static void AssertSameResults(System.Linq.Expressions.Expression<Func<Model, bool>> condition)
        {
            var translation = Translate<Model>(condition);
            var evaluate = condition.Compile();

            foreach (var model in Models)
            {
                var json = JsonSerializer.Serialize(model, SerializerOptions);

                Assert.AreEqual(evaluate(model), EvaluateJavaScript(translation, json),
                    $"'{condition.Body}' evaluates differently in JavaScript for model {json}. Translation: {translation}");
            }
        }

        private static bool EvaluateJavaScript(string translation, string model) =>
            (bool)new ScriptEngine().Evaluate($"!!(({translation})({model}, {Context}))");

        private static bool IsFeatureEnabled() => true;

        private class Helper
        {
            public bool IsValid(string text) => text != null;
        }

        private class Model
        {
            public bool Flag { get; set; }
            public bool Other { get; set; }
            public int Number { get; set; }
            public bool? NullableFlag { get; set; }
            public string Text { get; set; }
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public string[] Items { get; set; }
            public bool[] Flags { get; set; }
        }
    }
}
