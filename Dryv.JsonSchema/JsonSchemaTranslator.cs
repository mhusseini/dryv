using System.Linq.Expressions;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dryv.Configuration;
using Dryv.Rules;
using Dryv.Translation;

namespace Dryv.JsonSchema;

/// <summary>
/// Translates Dryv validation rules into JSON Schema fragments.
/// Proof-of-concept demonstrating the language-agnostic translator architecture.
/// </summary>
public class JsonSchemaTranslator(DryvOptions options) : Translator(options)
{
    public override Translation.TranslationResult Translate(
        Expression expression,
        MemberExpression propertyExpression,
        DryvRuleDefinition rule)
    {
        var lambda = (LambdaExpression)expression;
        var propertyName = ResolvePropertyName(propertyExpression, lambda);
        var constraints = new Dictionary<string, object>();
        var required = false;

        AnalyzeBody(lambda.Body, constraints, ref required);

        var schema = BuildPropertySchema(propertyName, constraints, required);
        var json = JsonSerializer.Serialize(schema);

        return new Translation.TranslationResult
        {
            Factory = (_, _, _) => json,
            InjectedServiceTypes = rule.ServiceTypes ?? Array.Empty<Type>(),
            CodeTemplate = json,
        };
    }

    public override bool TryWriteTerminal(Expression expression, TextWriter writer) => false;

    public override string FormatIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return name.Length == 1
            ? name.ToLowerInvariant()
            : char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    public override string TranslateValue(object? value) =>
        JsonSerializer.Serialize(value);

    /// <summary>
    /// Merges the per-rule JSON Schema fragments produced by <see cref="Translate"/>
    /// into a single JSON Schema document.
    /// </summary>
    public static Dictionary<string, object> BuildSchema(
        TranslationResult translationResult,
        string? title = null)
    {
        var mergedProperties = new Dictionary<string, Dictionary<string, JsonElement>>();
        var mergedRequired = new HashSet<string>();

        foreach (var (_, rules) in translationResult.ValidationFunctions)
        {
            foreach (var rule in rules)
            {
                if (string.IsNullOrEmpty(rule.ClientCode))
                {
                    continue;
                }

                using var doc = JsonDocument.Parse(rule.ClientCode);
                var root = doc.RootElement;

                if (root.TryGetProperty("properties", out var props))
                {
                    foreach (var prop in props.EnumerateObject())
                    {
                        if (!mergedProperties.ContainsKey(prop.Name))
                        {
                            mergedProperties[prop.Name] = new Dictionary<string, JsonElement>();
                        }

                        foreach (var constraint in prop.Value.EnumerateObject())
                        {
                            mergedProperties[prop.Name][constraint.Name] = constraint.Value.Clone();
                        }
                    }
                }

                if (root.TryGetProperty("required", out var req))
                {
                    foreach (var item in req.EnumerateArray())
                    {
                        mergedRequired.Add(item.GetString()!);
                    }
                }
            }
        }

        var schema = new Dictionary<string, object>
        {
            ["$schema"] = "https://json-schema.org/draft/2020-12/schema",
            ["type"] = "object",
            ["properties"] = mergedProperties.ToDictionary(
                kvp => kvp.Key,
                kvp => (object)kvp.Value),
        };

        if (title != null)
            schema["title"] = title;

        if (mergedRequired.Count > 0)
            schema["required"] = mergedRequired.OrderBy(r => r).ToArray();

        return schema;
    }

    #region Schema Building

    private static Dictionary<string, object> BuildPropertySchema(
        string propertyName,
        Dictionary<string, object> constraints,
        bool required)
    {
        var schema = new Dictionary<string, object> { ["type"] = "object" };

        if (constraints.Count > 0)
        {
            schema["properties"] = new Dictionary<string, object>
            {
                [propertyName] = constraints
            };
        }

        if (required)
        {
            schema["required"] = new[] { propertyName };
        }

        return schema;
    }

    #endregion

    #region Expression Analysis

    private static void AnalyzeBody(
        Expression body,
        Dictionary<string, object> constraints,
        ref bool required)
    {
        body = UnwrapConvert(body);

        if (body is ConditionalExpression conditional)
            AnalyzeConditional(conditional, constraints, ref required);
    }

    private static void AnalyzeConditional(
        ConditionalExpression conditional,
        Dictionary<string, object> constraints,
        ref bool required)
    {
        // Standard Dryv pattern: failureCondition ? "error" : DryvValidationResult.Success
        if (IsSuccessResult(conditional.IfFalse))
            ExtractFromFailureCondition(conditional.Test, constraints, ref required);
        // Inverted: successCondition ? DryvValidationResult.Success : "error"
        else if (IsSuccessResult(conditional.IfTrue))
            ExtractFromSuccessCondition(conditional.Test, constraints, ref required);
    }

    /// <summary>
    /// Extracts JSON Schema constraints from an expression that is true when validation FAILS.
    /// The extracted constraints represent the negated (valid) case.
    /// </summary>
    private static void ExtractFromFailureCondition(
        Expression condition,
        Dictionary<string, object> constraints,
        ref bool required)
    {
        condition = UnwrapConvert(condition);

        switch (condition)
        {
            // string.IsNullOrEmpty(m.Prop) → valid: not empty → minLength: 1
            case MethodCallExpression { Method: { Name: "IsNullOrEmpty" or "IsNullOrWhiteSpace", IsStatic: true } }:
                constraints["minLength"] = 1;
                break;

            // m.Prop == null → valid: required
            case BinaryExpression { NodeType: ExpressionType.Equal } eq
                when IsNullConstant(eq.Left) || IsNullConstant(eq.Right):
                required = true;
                break;

            // !(successCondition) → delegate to success extractor
            case UnaryExpression { NodeType: ExpressionType.Not } not:
                ExtractFromSuccessCondition(not.Operand, constraints, ref required);
                break;

            // m.Prop.Length < N → valid: length >= N → minLength: N
            case BinaryExpression { NodeType: ExpressionType.LessThan } lt
                when IsStringLength(lt.Left) && TryEvaluateAs<int>(lt.Right, out var min):
                constraints["minLength"] = min;
                break;

            // m.Prop.Length <= N → valid: length > N → minLength: N + 1
            case BinaryExpression { NodeType: ExpressionType.LessThanOrEqual } lte
                when IsStringLength(lte.Left) && TryEvaluateAs<int>(lte.Right, out var minPlus):
                constraints["minLength"] = minPlus + 1;
                break;

            // m.Prop.Length > N → valid: length <= N → maxLength: N
            case BinaryExpression { NodeType: ExpressionType.GreaterThan } gt
                when IsStringLength(gt.Left) && TryEvaluateAs<int>(gt.Right, out var max):
                constraints["maxLength"] = max;
                break;

            // m.Prop.Length >= N → valid: length < N → maxLength: N - 1
            case BinaryExpression { NodeType: ExpressionType.GreaterThanOrEqual } gte
                when IsStringLength(gte.Left) && TryEvaluateAs<int>(gte.Right, out var maxMinus):
                constraints["maxLength"] = maxMinus - 1;
                break;

            // m.NumProp < N → valid: prop >= N → minimum: N
            case BinaryExpression { NodeType: ExpressionType.LessThan } numLt
                when IsModelMember(numLt.Left) && IsNumericType(numLt.Left.Type)
                     && TryEvaluate(numLt.Right, out var numMin):
                constraints["minimum"] = numMin;
                break;

            // m.NumProp <= N → valid: prop > N → exclusiveMinimum: N
            case BinaryExpression { NodeType: ExpressionType.LessThanOrEqual } numLte
                when IsModelMember(numLte.Left) && IsNumericType(numLte.Left.Type)
                     && TryEvaluate(numLte.Right, out var exMin):
                constraints["exclusiveMinimum"] = exMin;
                break;

            // m.NumProp > N → valid: prop <= N → maximum: N
            case BinaryExpression { NodeType: ExpressionType.GreaterThan } numGt
                when IsModelMember(numGt.Left) && IsNumericType(numGt.Left.Type)
                     && TryEvaluate(numGt.Right, out var numMax):
                constraints["maximum"] = numMax;
                break;

            // m.NumProp >= N → valid: prop < N → exclusiveMaximum: N
            case BinaryExpression { NodeType: ExpressionType.GreaterThanOrEqual } numGte
                when IsModelMember(numGte.Left) && IsNumericType(numGte.Left.Type)
                     && TryEvaluate(numGte.Right, out var exMax):
                constraints["exclusiveMaximum"] = exMax;
                break;

            // Regex.IsMatch(...) → valid: NOT match → not: { pattern }
            case MethodCallExpression { Method.Name: "IsMatch" } regexFail
                when IsRegexMethod(regexFail) && TryExtractRegexPattern(regexFail, out var antiPattern):
                constraints["not"] = new Dictionary<string, object> { ["pattern"] = antiPattern };
                break;

            // cond1 && cond2 → both are failure conditions
            case BinaryExpression { NodeType: ExpressionType.AndAlso } and:
                ExtractFromFailureCondition(and.Left, constraints, ref required);
                ExtractFromFailureCondition(and.Right, constraints, ref required);
                break;

            // cond1 || cond2 → either is a failure condition (best-effort merge)
            case BinaryExpression { NodeType: ExpressionType.OrElse } or:
                ExtractFromFailureCondition(or.Left, constraints, ref required);
                ExtractFromFailureCondition(or.Right, constraints, ref required);
                break;
        }
    }

    /// <summary>
    /// Extracts JSON Schema constraints from an expression that is true when validation SUCCEEDS.
    /// </summary>
    private static void ExtractFromSuccessCondition(
        Expression condition,
        Dictionary<string, object> constraints,
        ref bool required)
    {
        condition = UnwrapConvert(condition);

        switch (condition)
        {
            // Regex.IsMatch(m.Prop, pattern) → pattern: ...
            case MethodCallExpression { Method.Name: "IsMatch" } regexCall
                when IsRegexMethod(regexCall) && TryExtractRegexPattern(regexCall, out var pattern):
                constraints["pattern"] = pattern;
                break;

            // m.Prop != null → required
            case BinaryExpression { NodeType: ExpressionType.NotEqual } neq
                when IsNullConstant(neq.Left) || IsNullConstant(neq.Right):
                required = true;
                break;

            // !(failureCondition) → delegate to failure extractor
            case UnaryExpression { NodeType: ExpressionType.Not } not:
                ExtractFromFailureCondition(not.Operand, constraints, ref required);
                break;

            // m.Prop.Length >= N → minLength: N
            case BinaryExpression { NodeType: ExpressionType.GreaterThanOrEqual } gte
                when IsStringLength(gte.Left) && TryEvaluateAs<int>(gte.Right, out var min):
                constraints["minLength"] = min;
                break;

            // m.Prop.Length > N → minLength: N + 1
            case BinaryExpression { NodeType: ExpressionType.GreaterThan } gt
                when IsStringLength(gt.Left) && TryEvaluateAs<int>(gt.Right, out var minPlus):
                constraints["minLength"] = minPlus + 1;
                break;

            // m.Prop.Length <= N → maxLength: N
            case BinaryExpression { NodeType: ExpressionType.LessThanOrEqual } lte
                when IsStringLength(lte.Left) && TryEvaluateAs<int>(lte.Right, out var max):
                constraints["maxLength"] = max;
                break;

            // m.Prop.Length < N → maxLength: N - 1
            case BinaryExpression { NodeType: ExpressionType.LessThan } lt
                when IsStringLength(lt.Left) && TryEvaluateAs<int>(lt.Right, out var maxMinus):
                constraints["maxLength"] = maxMinus - 1;
                break;

            // m.NumProp >= N → minimum: N
            case BinaryExpression { NodeType: ExpressionType.GreaterThanOrEqual } numGte
                when IsModelMember(numGte.Left) && IsNumericType(numGte.Left.Type)
                     && TryEvaluate(numGte.Right, out var numMin):
                constraints["minimum"] = numMin;
                break;

            // m.NumProp > N → exclusiveMinimum: N
            case BinaryExpression { NodeType: ExpressionType.GreaterThan } numGt
                when IsModelMember(numGt.Left) && IsNumericType(numGt.Left.Type)
                     && TryEvaluate(numGt.Right, out var exMin):
                constraints["exclusiveMinimum"] = exMin;
                break;

            // m.NumProp <= N → maximum: N
            case BinaryExpression { NodeType: ExpressionType.LessThanOrEqual } numLte
                when IsModelMember(numLte.Left) && IsNumericType(numLte.Left.Type)
                     && TryEvaluate(numLte.Right, out var numMax):
                constraints["maximum"] = numMax;
                break;

            // m.NumProp < N → exclusiveMaximum: N
            case BinaryExpression { NodeType: ExpressionType.LessThan } numLt
                when IsModelMember(numLt.Left) && IsNumericType(numLt.Left.Type)
                     && TryEvaluate(numLt.Right, out var exMax):
                constraints["exclusiveMaximum"] = exMax;
                break;

            // cond1 && cond2 → both are success conditions
            case BinaryExpression { NodeType: ExpressionType.AndAlso } and:
                ExtractFromSuccessCondition(and.Left, constraints, ref required);
                ExtractFromSuccessCondition(and.Right, constraints, ref required);
                break;
        }
    }

    #endregion

    #region Helpers

    private string ResolvePropertyName(MemberExpression? propertyExpression, LambdaExpression lambda)
    {
        if (propertyExpression?.Member != null)
            return FormatIdentifier(propertyExpression.Member.Name);

        var finder = new PropertyNameFinder(lambda.Parameters.FirstOrDefault());
        finder.Visit(lambda.Body);
        return finder.PropertyName != null
            ? FormatIdentifier(finder.PropertyName)
            : "unknown";
    }

    private static Expression UnwrapConvert(Expression expression)
    {
        while (expression is UnaryExpression { NodeType: ExpressionType.Convert } convert)
            expression = convert.Operand;
        return expression;
    }

    private static bool IsSuccessResult(Expression expression)
    {
        expression = UnwrapConvert(expression);

        return expression switch
        {
            MemberExpression { Member: { Name: "Success", DeclaringType: { } dt } }
                when dt == typeof(DryvValidationResult) => true,
            ConstantExpression { Value: null } => true,
            _ => false
        };
    }

    private static bool IsNullConstant(Expression expression) =>
        expression is ConstantExpression { Value: null }
        || (expression is DefaultExpression def && !def.Type.IsValueType);

    private static bool IsStringLength(Expression expression) =>
        expression is MemberExpression { Member.Name: "Length" } m
        && m.Expression?.Type == typeof(string);

    private static bool IsModelMember(Expression expression) =>
        expression is MemberExpression { Expression: ParameterExpression };

    private static bool IsNumericType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type == typeof(int) || type == typeof(long) || type == typeof(double)
            || type == typeof(float) || type == typeof(decimal) || type == typeof(short)
            || type == typeof(byte);
    }

    private static bool IsRegexMethod(MethodCallExpression call) =>
        call.Method.DeclaringType == typeof(Regex);

    private static bool TryEvaluate(Expression expression, out object? value)
    {
        try
        {
            var lambda = Expression.Lambda(expression);
            value = lambda.Compile().DynamicInvoke();
            return value != null;
        }
        catch
        {
            value = null;
            return false;
        }
    }

    private static bool TryEvaluateAs<T>(Expression expression, out T value)
    {
        if (expression is ConstantExpression { Value: T v })
        {
            value = v;
            return true;
        }

        if (TryEvaluate(expression, out var obj) && obj is T typed)
        {
            value = typed;
            return true;
        }

        value = default!;
        return false;
    }

    private static bool TryExtractRegexPattern(MethodCallExpression call, out string pattern)
    {
        // Instance: new Regex(pattern).IsMatch(input)
        if (!call.Method.IsStatic && call.Object is NewExpression newExpr
            && newExpr.Arguments.Count >= 1
            && TryEvaluateAs<string>(newExpr.Arguments[0], out var newPattern))
        {
            pattern = newPattern;
            return true;
        }

        // Instance from captured variable: regexVar.IsMatch(input)
        if (!call.Method.IsStatic && call.Object != null
            && TryEvaluate(call.Object, out var regexObj) && regexObj is Regex regex)
        {
            pattern = regex.ToString();
            return true;
        }

        // Static: Regex.IsMatch(input, pattern) — pattern is the evaluable non-model argument
        if (call.Method.IsStatic && call.Arguments.Count >= 2)
        {
            foreach (var arg in call.Arguments)
            {
                if (arg.Type == typeof(string) && !ContainsParameter(arg)
                    && TryEvaluateAs<string>(arg, out var staticPattern))
                {
                    pattern = staticPattern;
                    return true;
                }
            }
        }

        pattern = null!;
        return false;
    }

    private static bool ContainsParameter(Expression expression)
    {
        var finder = new ParameterFinder();
        finder.Visit(expression);
        return finder.Found;
    }

    #endregion

    #region Visitors

    private sealed class PropertyNameFinder(ParameterExpression? parameter) : ExpressionVisitor
    {
        public string? PropertyName { get; private set; }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (PropertyName == null && node.Expression == parameter)
                PropertyName = node.Member.Name;
            return base.VisitMember(node);
        }
    }

    private sealed class ParameterFinder : ExpressionVisitor
    {
        public bool Found { get; private set; }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            Found = true;
            return base.VisitParameter(node);
        }
    }

    #endregion
}
