using System.Linq.Expressions;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dryv.Configuration;
using Dryv.Rules;

namespace Dryv.JsonSchema.Tests;

[TestClass]
public class JsonSchemaTranslatorTests
{
    private readonly JsonSchemaTranslator translator = new(new DryvOptions());

    #region Null Checks → required

    [TestMethod]
    public void NullCheck_ProducesRequired()
    {
        var schema = Translate<TestModel>(m =>
            m.Text == null ? "Text is required" : DryvValidationResult.Success);

        Assert.IsTrue(schema.RootElement.TryGetProperty("required", out var req));
        Assert.AreEqual("text", req[0].GetString());
    }

    [TestMethod]
    public void ReversedNullCheck_ProducesRequired()
    {
        var schema = Translate<TestModel>(m =>
            null == m.Text ? "Text is required" : DryvValidationResult.Success);

        Assert.IsTrue(schema.RootElement.TryGetProperty("required", out var req));
        Assert.AreEqual("text", req[0].GetString());
    }

    #endregion

    #region String Emptiness → minLength

    [TestMethod]
    public void IsNullOrEmpty_ProducesMinLength1()
    {
        var schema = Translate<TestModel>(m =>
            string.IsNullOrEmpty(m.Text) ? "Text must not be empty" : DryvValidationResult.Success);

        var prop = GetPropertyConstraints(schema, "text");
        Assert.AreEqual(1, prop.GetProperty("minLength").GetInt32());
    }

    [TestMethod]
    public void IsNullOrWhiteSpace_ProducesMinLength1()
    {
        var schema = Translate<TestModel>(m =>
            string.IsNullOrWhiteSpace(m.Text) ? "Text must not be blank" : DryvValidationResult.Success);

        var prop = GetPropertyConstraints(schema, "text");
        Assert.AreEqual(1, prop.GetProperty("minLength").GetInt32());
    }

    #endregion

    #region String Length → minLength / maxLength

    [TestMethod]
    public void StringLengthLessThan_ProducesMinLength()
    {
        var schema = Translate<TestModel>(m =>
            m.Text.Length < 3 ? "Too short" : DryvValidationResult.Success);

        var prop = GetPropertyConstraints(schema, "text");
        Assert.AreEqual(3, prop.GetProperty("minLength").GetInt32());
    }

    [TestMethod]
    public void StringLengthGreaterThan_ProducesMaxLength()
    {
        var schema = Translate<TestModel>(m =>
            m.Text.Length > 50 ? "Too long" : DryvValidationResult.Success);

        var prop = GetPropertyConstraints(schema, "text");
        Assert.AreEqual(50, prop.GetProperty("maxLength").GetInt32());
    }

    [TestMethod]
    public void StringLengthLessThanOrEqual_ProducesMinLengthPlusOne()
    {
        var schema = Translate<TestModel>(m =>
            m.Text.Length <= 2 ? "Too short" : DryvValidationResult.Success);

        var prop = GetPropertyConstraints(schema, "text");
        Assert.AreEqual(3, prop.GetProperty("minLength").GetInt32());
    }

    [TestMethod]
    public void StringLengthGreaterThanOrEqual_ProducesMaxLengthMinusOne()
    {
        var schema = Translate<TestModel>(m =>
            m.Text.Length >= 100 ? "Too long" : DryvValidationResult.Success);

        var prop = GetPropertyConstraints(schema, "text");
        Assert.AreEqual(99, prop.GetProperty("maxLength").GetInt32());
    }

    #endregion

    #region Numeric Comparisons → minimum / maximum

    [TestMethod]
    public void NumericLessThan_ProducesMinimum()
    {
        var schema = Translate<TestModel>(m =>
            m.Age < 18 ? "Must be at least 18" : DryvValidationResult.Success);

        var prop = GetPropertyConstraints(schema, "age");
        Assert.AreEqual(18, prop.GetProperty("minimum").GetInt32());
    }

    [TestMethod]
    public void NumericGreaterThan_ProducesMaximum()
    {
        var schema = Translate<TestModel>(m =>
            m.Age > 120 ? "Too old" : DryvValidationResult.Success);

        var prop = GetPropertyConstraints(schema, "age");
        Assert.AreEqual(120, prop.GetProperty("maximum").GetInt32());
    }

    [TestMethod]
    public void NumericLessThanOrEqual_ProducesExclusiveMinimum()
    {
        var schema = Translate<TestModel>(m =>
            m.Age <= 0 ? "Must be positive" : DryvValidationResult.Success);

        var prop = GetPropertyConstraints(schema, "age");
        Assert.AreEqual(0, prop.GetProperty("exclusiveMinimum").GetInt32());
    }

    #endregion

    #region Regex → pattern

    [TestMethod]
    public void NegatedRegexIsMatch_ProducesPattern()
    {
        var schema = Translate<TestModel>(m =>
            !new Regex(@"^\d+$").IsMatch(m.Text) ? "Must be numeric" : DryvValidationResult.Success);

        var prop = GetPropertyConstraints(schema, "text");
        Assert.AreEqual(@"^\d+$", prop.GetProperty("pattern").GetString());
    }

    [TestMethod]
    public void NegatedRegexWithCapturedPattern_ProducesPattern()
    {
        var pattern = @"^[a-z]+$";
        var schema = Translate<TestModel>(m =>
            !new Regex(pattern).IsMatch(m.Text) ? "Must be lowercase" : DryvValidationResult.Success);

        var prop = GetPropertyConstraints(schema, "text");
        Assert.AreEqual(pattern, prop.GetProperty("pattern").GetString());
    }

    [TestMethod]
    public void NegatedCapturedRegexObject_ProducesPattern()
    {
        var regex = new Regex(@"^\w+$", RegexOptions.IgnoreCase);
        var schema = Translate<TestModel>(m =>
            !regex.IsMatch(m.Text) ? "Invalid" : DryvValidationResult.Success);

        var prop = GetPropertyConstraints(schema, "text");
        Assert.AreEqual(@"^\w+$", prop.GetProperty("pattern").GetString());
    }

    [TestMethod]
    public void RegexIsMatch_ProducesNotPattern()
    {
        var schema = Translate<TestModel>(m =>
            new Regex(@"^\d+$").IsMatch(m.Text) ? "Must not be all digits" : DryvValidationResult.Success);

        var prop = GetPropertyConstraints(schema, "text");
        Assert.IsTrue(prop.TryGetProperty("not", out var not));
        Assert.AreEqual(@"^\d+$", not.GetProperty("pattern").GetString());
    }

    #endregion

    #region Combined Constraints

    [TestMethod]
    public void CombinedFailureConditions_ProducesMultipleConstraints()
    {
        var schema = Translate<TestModel>(m =>
            m.Text == null || m.Text.Length < 2 ? "Invalid" : DryvValidationResult.Success);

        Assert.IsTrue(schema.RootElement.TryGetProperty("required", out _));
        var prop = GetPropertyConstraints(schema, "text");
        Assert.AreEqual(2, prop.GetProperty("minLength").GetInt32());
    }

    #endregion

    #region Output Format

    [TestMethod]
    public void Schema_HasTypeObject()
    {
        var schema = Translate<TestModel>(m =>
            m.Text == null ? "error" : DryvValidationResult.Success);

        Assert.AreEqual("object", schema.RootElement.GetProperty("type").GetString());
    }

    [TestMethod]
    public void PropertyName_IsCamelCase()
    {
        var schema = Translate<TestModel>(m =>
            string.IsNullOrEmpty(m.Text) ? "error" : DryvValidationResult.Success);

        Assert.IsTrue(schema.RootElement.GetProperty("properties").TryGetProperty("text", out _));
    }

    [TestMethod]
    public void Factory_ReturnsStaticJson()
    {
        Expression<Func<TestModel, DryvValidationResult>> expr = m =>
            m.Text == null ? "error" : DryvValidationResult.Success;

        var rule = CreateRule(typeof(TestModel));
        var result = translator.Translate(expr, null, rule);

        var json1 = result.Factory(null!, null!, new DryvOptions());
        var json2 = result.Factory(null!, null!, new DryvOptions());
        Assert.AreEqual(json1, json2);
    }

    #endregion

    #region Boolean Properties

    [TestMethod]
    public void NegatedBoolProp_ProducesConstTrue()
    {
        var schema = Translate<TestModel>(m =>
            !m.AcceptsTerms ? "Must accept terms" : DryvValidationResult.Success);

        var prop = GetPropertyConstraints(schema, "acceptsTerms");
        Assert.AreEqual(true, prop.GetProperty("const").GetBoolean());
    }

    #endregion

    #region String Methods

    [TestMethod]
    public void NegatedContains_ProducesPattern()
    {
        var schema = Translate<TestModel>(m =>
            !m.Email.Contains("@") ? "Must contain @" : DryvValidationResult.Success);

        var prop = GetPropertyConstraints(schema, "email");
        Assert.IsTrue(prop.TryGetProperty("pattern", out var pattern));
        Assert.IsTrue(pattern.GetString()!.Contains("@"));
    }

    [TestMethod]
    public void NegatedStartsWith_ProducesPattern()
    {
        var schema = Translate<TestModel>(m =>
            !m.Text.StartsWith("http") ? "Must start with http" : DryvValidationResult.Success);

        var prop = GetPropertyConstraints(schema, "text");
        Assert.IsTrue(prop.TryGetProperty("pattern", out var pattern));
        Assert.IsTrue(pattern.GetString()!.StartsWith("^"));
    }

    [TestMethod]
    public void NegatedEndsWith_ProducesPattern()
    {
        var schema = Translate<TestModel>(m =>
            !m.Text.EndsWith(".com") ? "Must end with .com" : DryvValidationResult.Success);

        var prop = GetPropertyConstraints(schema, "text");
        Assert.IsTrue(prop.TryGetProperty("pattern", out var pattern));
        Assert.IsTrue(pattern.GetString()!.EndsWith("$"));
    }

    #endregion

    #region Trim + Length

    [TestMethod]
    public void TrimLengthLessThan_ProducesMinLength()
    {
        var schema = Translate<TestModel>(m =>
            m.Text.Trim().Length < 5 ? "Too short" : DryvValidationResult.Success);

        var prop = GetPropertyConstraints(schema, "text");
        Assert.AreEqual(5, prop.GetProperty("minLength").GetInt32());
    }

    #endregion

    #region String Equality

    [TestMethod]
    public void StringNotEqual_ProducesConst()
    {
        var schema = Translate<TestModel>(m =>
            m.AccountType != "business" ? "Must be business" : DryvValidationResult.Success);

        var prop = GetPropertyConstraints(schema, "accountType");
        Assert.AreEqual("business", prop.GetProperty("const").GetString());
    }

    #endregion

    #region Collection Constraints

    [TestMethod]
    public void CollectionCountLessThan_ProducesMinItems()
    {
        var schema = Translate<TestModel>(m =>
            m.Tags.Count < 1 ? "At least one tag" : DryvValidationResult.Success);

        var prop = GetPropertyConstraints(schema, "tags");
        Assert.AreEqual(1, prop.GetProperty("minItems").GetInt32());
    }

    #endregion

    #region Exact String Length

    [TestMethod]
    public void SuccessExactLength_ProducesMinAndMaxLength()
    {
        var schema = Translate<TestModel>(m =>
            m.Text.Length == 5 ? DryvValidationResult.Success : "Must be exactly 5 chars");

        var prop = GetPropertyConstraints(schema, "text");
        Assert.AreEqual(5, prop.GetProperty("minLength").GetInt32());
        Assert.AreEqual(5, prop.GetProperty("maxLength").GetInt32());
    }

    #endregion

    #region Inverted Numeric (constant on left)

    [TestMethod]
    public void InvertedNumericGreaterThan_ProducesMinimum()
    {
        // 18 > m.Age is same as m.Age < 18 → minimum: 18
        var schema = Translate<TestModel>(m =>
            18 > m.Age ? "Must be at least 18" : DryvValidationResult.Success);

        var prop = GetPropertyConstraints(schema, "age");
        Assert.AreEqual(18, prop.GetProperty("minimum").GetInt32());
    }

    #endregion

    #region Helpers

    private JsonDocument Translate<TModel>(Expression<Func<TModel, DryvValidationResult>> expression)
    {
        var rule = CreateRule(typeof(TModel));
        var result = translator.Translate(expression, null, rule);
        var json = result.Factory(null!, null!, new DryvOptions());
        return JsonDocument.Parse(json);
    }

    private static DryvRuleDefinition CreateRule(Type modelType)
    {
        var rule = (DryvRuleDefinition)Activator.CreateInstance(typeof(DryvRuleDefinition), nonPublic: true)!;
        typeof(DryvRuleDefinition).GetProperty(nameof(DryvRuleDefinition.ModelType))!.SetValue(rule, modelType);
        return rule;
    }

    private static JsonElement GetPropertyConstraints(JsonDocument schema, string propertyName) =>
        schema.RootElement.GetProperty("properties").GetProperty(propertyName);

    private abstract class TestModel
    {
        public abstract string Text { get; set; }
        public int Age { get; set; }
        public decimal Price { get; set; }
        public bool AcceptsTerms { get; set; }
        public string Email { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
    }

    #endregion
}
