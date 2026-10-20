using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace Dryv.JsonSchema;

/// <summary>
/// Walks the body of a Dryv validation lambda and extracts JSON Schema
/// constraints (minLength, pattern, minimum, required, etc.) from the
/// condition expressions.
/// </summary>
internal static class ConstraintExtractor
{
    internal static void AnalyzeBody(
        Expression body,
        Dictionary<string, object?> constraints,
        ref bool required)
    {
        body = ExpressionHelpers.UnwrapConvert(body);

        switch (body)
        {
            case ConditionalExpression conditional:
                AnalyzeConditional(conditional, constraints, ref required);
                break;

            // Nested conditionals / if-else-if chains:
            // The compiler sometimes emits Block expressions for complex rules.
            case BlockExpression block:
                foreach (var expr in block.Expressions)
                {
                    AnalyzeBody(expr, constraints, ref required);
                }
                break;
        }
    }

    private static void AnalyzeConditional(
        ConditionalExpression conditional,
        Dictionary<string, object?> constraints,
        ref bool required)
    {
        // Standard Dryv pattern: failureCondition ? "error" : DryvValidationResult.Success
        if (ExpressionHelpers.IsSuccessResult(conditional.IfFalse))
        {
            ExtractFromFailureCondition(conditional.Test, constraints, ref required);
        }
        // Inverted: successCondition ? DryvValidationResult.Success : "error"
        else if (ExpressionHelpers.IsSuccessResult(conditional.IfTrue))
        {
            ExtractFromSuccessCondition(conditional.Test, constraints, ref required);
        }

        // Nested ternaries in IfTrue/IfFalse branches
        if (conditional.IfTrue is ConditionalExpression nestedTrue)
        {
            AnalyzeConditional(nestedTrue, constraints, ref required);
        }
        
        if (conditional.IfFalse is ConditionalExpression nestedFalse)
        {
            AnalyzeConditional(nestedFalse, constraints, ref required);
        }
    }

    /// <summary>
    /// Extracts JSON Schema constraints from an expression that is true when validation FAILS.
    /// The extracted constraints represent the negated (valid) case.
    /// </summary>
    private static void ExtractFromFailureCondition(
        Expression condition,
        Dictionary<string, object?> constraints,
        ref bool required)
    {
        condition = ExpressionHelpers.UnwrapConvert(condition);

        switch (condition)
        {
            // ── String emptiness ────────────────────────────────────────
            // string.IsNullOrEmpty(m.Prop) → valid: not empty → minLength: 1
            case MethodCallExpression { Method: { Name: "IsNullOrEmpty" or "IsNullOrWhiteSpace", IsStatic: true } }:
                constraints["minLength"] = 1;
                break;

            // ── Null checks ─────────────────────────────────────────────
            // m.Prop == null → valid: required
            case BinaryExpression { NodeType: ExpressionType.Equal } eq
                when ExpressionHelpers.IsNullConstant(eq.Left) || ExpressionHelpers.IsNullConstant(eq.Right):
                required = true;
                break;

            // ── Negation ────────────────────────────────────────────────
            // !(successCondition) → delegate to success extractor
            case UnaryExpression { NodeType: ExpressionType.Not } not:
                ExtractFromSuccessCondition(not.Operand, constraints, ref required);
                break;

            // ── Boolean property ────────────────────────────────────────
            // !m.BoolProp expressed as the bare member in failure position
            // (i.e. "if m.BoolProp is false then fail") → valid: must be true
            case MemberExpression boolMember when ExpressionHelpers.IsBooleanMember(boolMember):
                // The expression is the failure condition, and it's just reading m.BoolProp.
                // This means "fails when m.BoolProp is truthy" — rarely used this way.
                // More commonly the test is `!m.BoolProp` which arrives as Not → success path.
                // Here the condition says "when m.BoolProp is true, it fails" → valid: false
                constraints["const"] = false;
                break;

            // ── String length (with Trim unwrapping) ────────────────────
            // m.Prop.Length < N  OR  m.Prop.Trim().Length < N → minLength: N
            case BinaryExpression { NodeType: ExpressionType.LessThan } lt
                when ExpressionHelpers.IsStringLength(lt.Left) && ExpressionHelpers.TryEvaluateAs<int>(lt.Right, out var minLen):
                constraints["minLength"] = minLen;
                break;

            // m.Prop.Length <= N → minLength: N + 1
            case BinaryExpression { NodeType: ExpressionType.LessThanOrEqual } lte
                when ExpressionHelpers.IsStringLength(lte.Left) && ExpressionHelpers.TryEvaluateAs<int>(lte.Right, out var minLenPlus):
                constraints["minLength"] = minLenPlus + 1;
                break;

            // m.Prop.Length > N → maxLength: N
            case BinaryExpression { NodeType: ExpressionType.GreaterThan } gt
                when ExpressionHelpers.IsStringLength(gt.Left) && ExpressionHelpers.TryEvaluateAs<int>(gt.Right, out var maxLen):
                constraints["maxLength"] = maxLen;
                break;

            // m.Prop.Length >= N → maxLength: N - 1
            case BinaryExpression { NodeType: ExpressionType.GreaterThanOrEqual } gte
                when ExpressionHelpers.IsStringLength(gte.Left) && ExpressionHelpers.TryEvaluateAs<int>(gte.Right, out var maxLenMinus):
                constraints["maxLength"] = maxLenMinus - 1;
                break;

            // m.Prop.Length == N → fails when length equals N → valid: length != N
            // Not directly expressible; skip (best-effort: no constraint).

            // ── Inverted string length (constant on left) ───────────────
            // N > m.Prop.Length → same as m.Prop.Length < N → minLength: N
            case BinaryExpression { NodeType: ExpressionType.GreaterThan } gtInv
                when ExpressionHelpers.IsStringLength(gtInv.Right) && ExpressionHelpers.TryEvaluateAs<int>(gtInv.Left, out var minLenInv):
                constraints["minLength"] = minLenInv;
                break;

            // N < m.Prop.Length → same as m.Prop.Length > N → maxLength: N
            case BinaryExpression { NodeType: ExpressionType.LessThan } ltInv
                when ExpressionHelpers.IsStringLength(ltInv.Right) && ExpressionHelpers.TryEvaluateAs<int>(ltInv.Left, out var maxLenInv):
                constraints["maxLength"] = maxLenInv;
                break;

            // ── Numeric comparisons ─────────────────────────────────────
            // m.NumProp < N → valid: prop >= N → minimum: N
            case BinaryExpression { NodeType: ExpressionType.LessThan } numLt
                when ExpressionHelpers.IsModelMember(numLt.Left) && ExpressionHelpers.IsNumericType(numLt.Left.Type)
                     && ExpressionHelpers.TryEvaluate(numLt.Right, out var numMin):
                constraints["minimum"] = numMin;
                break;

            // m.NumProp <= N → valid: prop > N → exclusiveMinimum: N
            case BinaryExpression { NodeType: ExpressionType.LessThanOrEqual } numLte
                when ExpressionHelpers.IsModelMember(numLte.Left) && ExpressionHelpers.IsNumericType(numLte.Left.Type)
                     && ExpressionHelpers.TryEvaluate(numLte.Right, out var exMin):
                constraints["exclusiveMinimum"] = exMin;
                break;

            // m.NumProp > N → valid: prop <= N → maximum: N
            case BinaryExpression { NodeType: ExpressionType.GreaterThan } numGt
                when ExpressionHelpers.IsModelMember(numGt.Left) && ExpressionHelpers.IsNumericType(numGt.Left.Type)
                     && ExpressionHelpers.TryEvaluate(numGt.Right, out var numMax):
                constraints["maximum"] = numMax;
                break;

            // m.NumProp >= N → valid: prop < N → exclusiveMaximum: N
            case BinaryExpression { NodeType: ExpressionType.GreaterThanOrEqual } numGte
                when ExpressionHelpers.IsModelMember(numGte.Left) && ExpressionHelpers.IsNumericType(numGte.Left.Type)
                     && ExpressionHelpers.TryEvaluate(numGte.Right, out var exMax):
                constraints["exclusiveMaximum"] = exMax;
                break;

            // ── Inverted numeric (constant on left) ─────────────────────
            // N > m.NumProp → same as m.NumProp < N → minimum: N
            case BinaryExpression { NodeType: ExpressionType.GreaterThan } numGtInv
                when ExpressionHelpers.IsModelMember(numGtInv.Right) && ExpressionHelpers.IsNumericType(numGtInv.Right.Type)
                     && ExpressionHelpers.TryEvaluate(numGtInv.Left, out var numMinInv):
                constraints["minimum"] = numMinInv;
                break;

            // N < m.NumProp → same as m.NumProp > N → maximum: N
            case BinaryExpression { NodeType: ExpressionType.LessThan } numLtInv
                when ExpressionHelpers.IsModelMember(numLtInv.Right) && ExpressionHelpers.IsNumericType(numLtInv.Right.Type)
                     && ExpressionHelpers.TryEvaluate(numLtInv.Left, out var numMaxInv):
                constraints["maximum"] = numMaxInv;
                break;

            // N >= m.NumProp → same as m.NumProp <= N → exclusiveMinimum: N
            case BinaryExpression { NodeType: ExpressionType.GreaterThanOrEqual } numGteInv
                when ExpressionHelpers.IsModelMember(numGteInv.Right) && ExpressionHelpers.IsNumericType(numGteInv.Right.Type)
                     && ExpressionHelpers.TryEvaluate(numGteInv.Left, out var exMinInv):
                constraints["exclusiveMinimum"] = exMinInv;
                break;

            // N <= m.NumProp → same as m.NumProp >= N → exclusiveMaximum: N
            case BinaryExpression { NodeType: ExpressionType.LessThanOrEqual } numLteInv
                when ExpressionHelpers.IsModelMember(numLteInv.Right) && ExpressionHelpers.IsNumericType(numLteInv.Right.Type)
                     && ExpressionHelpers.TryEvaluate(numLteInv.Left, out var exMaxInv):
                constraints["exclusiveMaximum"] = exMaxInv;
                break;

            // ── String equality → const ─────────────────────────────────
            // m.Prop != "value" → valid: equals "value" → const: "value"
            case BinaryExpression { NodeType: ExpressionType.NotEqual } neqStr
                when ExpressionHelpers.IsModelMember(neqStr.Left) && neqStr.Left.Type == typeof(string)
                     && ExpressionHelpers.TryEvaluateAs<string>(neqStr.Right, out var constVal):
                constraints["const"] = constVal;
                break;

            case BinaryExpression { NodeType: ExpressionType.NotEqual } neqStrR
                when ExpressionHelpers.IsModelMember(neqStrR.Right) && neqStrR.Right.Type == typeof(string)
                     && ExpressionHelpers.TryEvaluateAs<string>(neqStrR.Left, out var constValR):
                constraints["const"] = constValR;
                break;

            // ── Collection .Count / .Length ──────────────────────────────
            // m.Items.Count < N → valid: count >= N → minItems: N
            case BinaryExpression { NodeType: ExpressionType.LessThan } colLt
                when ExpressionHelpers.IsCollectionCount(colLt.Left) && ExpressionHelpers.TryEvaluateAs<int>(colLt.Right, out var minItems):
                constraints["minItems"] = minItems;
                break;

            // m.Items.Count > N → valid: count <= N → maxItems: N
            case BinaryExpression { NodeType: ExpressionType.GreaterThan } colGt
                when ExpressionHelpers.IsCollectionCount(colGt.Left) && ExpressionHelpers.TryEvaluateAs<int>(colGt.Right, out var maxItems):
                constraints["maxItems"] = maxItems;
                break;

            // m.Items.Count <= N → valid: count > N → minItems: N + 1
            case BinaryExpression { NodeType: ExpressionType.LessThanOrEqual } colLte
                when ExpressionHelpers.IsCollectionCount(colLte.Left) && ExpressionHelpers.TryEvaluateAs<int>(colLte.Right, out var minItemsP):
                constraints["minItems"] = minItemsP + 1;
                break;

            // m.Items.Count >= N → valid: count < N → maxItems: N - 1
            case BinaryExpression { NodeType: ExpressionType.GreaterThanOrEqual } colGte
                when ExpressionHelpers.IsCollectionCount(colGte.Left) && ExpressionHelpers.TryEvaluateAs<int>(colGte.Right, out var maxItemsM):
                constraints["maxItems"] = maxItemsM - 1;
                break;

            // ── Regex → not: { pattern } ────────────────────────────────
            // Regex.IsMatch(...) → valid: NOT match → not: { pattern }
            case MethodCallExpression { Method.Name: "IsMatch" } regexFail
                when ExpressionHelpers.IsRegexMethod(regexFail) && ExpressionHelpers.TryExtractRegexPattern(regexFail, out var antiPattern):
                constraints["not"] = new Dictionary<string, object> { ["pattern"] = antiPattern };
                break;

            // ── String.Contains → pattern (best-effort) ─────────────────
            // !m.Email.Contains("@") arrives as Not → success extractor.
            // But m.Email.Contains("@") as failure condition means "fails when contains" → not: pattern
            case MethodCallExpression containsCall
                when ExpressionHelpers.TryGetStringInstanceCallArg(containsCall, "Contains", out _, out var containsVal):
                constraints["not"] = new Dictionary<string, object>
                {
                    ["pattern"] = ".*" + Regex.Escape(containsVal) + ".*"
                };
                break;

            // ── String.StartsWith → not: { pattern } ───────────────────
            case MethodCallExpression startsCall
                when ExpressionHelpers.TryGetStringInstanceCallArg(startsCall, "StartsWith", out _, out var startsVal):
                constraints["not"] = new Dictionary<string, object>
                {
                    ["pattern"] = "^" + Regex.Escape(startsVal)
                };
                break;

            // ── String.EndsWith → not: { pattern } ─────────────────────
            case MethodCallExpression endsCall
                when ExpressionHelpers.TryGetStringInstanceCallArg(endsCall, "EndsWith", out _, out var endsVal):
                constraints["not"] = new Dictionary<string, object>
                {
                    ["pattern"] = Regex.Escape(endsVal) + "$"
                };
                break;

            // ── Logical combinators ─────────────────────────────────────
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
        Dictionary<string, object?> constraints,
        ref bool required)
    {
        condition = ExpressionHelpers.UnwrapConvert(condition);

        switch (condition)
        {
            // ── Regex → pattern ─────────────────────────────────────────
            // Regex.IsMatch(m.Prop, pattern) → pattern: ...
            case MethodCallExpression { Method.Name: "IsMatch" } regexCall
                when ExpressionHelpers.IsRegexMethod(regexCall) && ExpressionHelpers.TryExtractRegexPattern(regexCall, out var pattern):
                constraints["pattern"] = pattern;
                FormatHints.TryApplyFormatHint(pattern, constraints);
                break;

            // ── Null checks ─────────────────────────────────────────────
            // m.Prop != null → required
            case BinaryExpression { NodeType: ExpressionType.NotEqual } neq
                when ExpressionHelpers.IsNullConstant(neq.Left) || ExpressionHelpers.IsNullConstant(neq.Right):
                required = true;
                break;

            // ── Negation ────────────────────────────────────────────────
            // !(failureCondition) → delegate to failure extractor
            case UnaryExpression { NodeType: ExpressionType.Not } not:
                ExtractFromFailureCondition(not.Operand, constraints, ref required);
                break;

            // ── Boolean property ────────────────────────────────────────
            // m.BoolProp (success when true) → const: true
            case MemberExpression boolMember when ExpressionHelpers.IsBooleanMember(boolMember):
                constraints["const"] = true;
                break;

            // ── String length (with Trim unwrapping) ────────────────────
            // m.Prop.Length >= N → minLength: N
            case BinaryExpression { NodeType: ExpressionType.GreaterThanOrEqual } gte
                when ExpressionHelpers.IsStringLength(gte.Left) && ExpressionHelpers.TryEvaluateAs<int>(gte.Right, out var min):
                constraints["minLength"] = min;
                break;

            // m.Prop.Length > N → minLength: N + 1
            case BinaryExpression { NodeType: ExpressionType.GreaterThan } gt
                when ExpressionHelpers.IsStringLength(gt.Left) && ExpressionHelpers.TryEvaluateAs<int>(gt.Right, out var minPlus):
                constraints["minLength"] = minPlus + 1;
                break;

            // m.Prop.Length <= N → maxLength: N
            case BinaryExpression { NodeType: ExpressionType.LessThanOrEqual } lte
                when ExpressionHelpers.IsStringLength(lte.Left) && ExpressionHelpers.TryEvaluateAs<int>(lte.Right, out var max):
                constraints["maxLength"] = max;
                break;

            // m.Prop.Length < N → maxLength: N - 1
            case BinaryExpression { NodeType: ExpressionType.LessThan } lt
                when ExpressionHelpers.IsStringLength(lt.Left) && ExpressionHelpers.TryEvaluateAs<int>(lt.Right, out var maxMinus):
                constraints["maxLength"] = maxMinus - 1;
                break;

            // m.Prop.Length == N → exact length → minLength + maxLength
            case BinaryExpression { NodeType: ExpressionType.Equal } eqLen
                when ExpressionHelpers.IsStringLength(eqLen.Left) && ExpressionHelpers.TryEvaluateAs<int>(eqLen.Right, out var exactLen):
                constraints["minLength"] = exactLen;
                constraints["maxLength"] = exactLen;
                break;

            // ── Inverted string length (constant on left) ───────────────
            // N <= m.Prop.Length → same as m.Prop.Length >= N → minLength: N
            case BinaryExpression { NodeType: ExpressionType.LessThanOrEqual } lteInv
                when ExpressionHelpers.IsStringLength(lteInv.Right) && ExpressionHelpers.TryEvaluateAs<int>(lteInv.Left, out var minInv):
                constraints["minLength"] = minInv;
                break;

            // N >= m.Prop.Length → same as m.Prop.Length <= N → maxLength: N
            case BinaryExpression { NodeType: ExpressionType.GreaterThanOrEqual } gteInv
                when ExpressionHelpers.IsStringLength(gteInv.Right) && ExpressionHelpers.TryEvaluateAs<int>(gteInv.Left, out var maxInv):
                constraints["maxLength"] = maxInv;
                break;

            // ── Numeric comparisons ─────────────────────────────────────
            // m.NumProp >= N → minimum: N
            case BinaryExpression { NodeType: ExpressionType.GreaterThanOrEqual } numGte
                when ExpressionHelpers.IsModelMember(numGte.Left) && ExpressionHelpers.IsNumericType(numGte.Left.Type)
                     && ExpressionHelpers.TryEvaluate(numGte.Right, out var numMin):
                constraints["minimum"] = numMin;
                break;

            // m.NumProp > N → exclusiveMinimum: N
            case BinaryExpression { NodeType: ExpressionType.GreaterThan } numGt
                when ExpressionHelpers.IsModelMember(numGt.Left) && ExpressionHelpers.IsNumericType(numGt.Left.Type)
                     && ExpressionHelpers.TryEvaluate(numGt.Right, out var exMin):
                constraints["exclusiveMinimum"] = exMin;
                break;

            // m.NumProp <= N → maximum: N
            case BinaryExpression { NodeType: ExpressionType.LessThanOrEqual } numLte
                when ExpressionHelpers.IsModelMember(numLte.Left) && ExpressionHelpers.IsNumericType(numLte.Left.Type)
                     && ExpressionHelpers.TryEvaluate(numLte.Right, out var numMax):
                constraints["maximum"] = numMax;
                break;

            // m.NumProp < N → exclusiveMaximum: N
            case BinaryExpression { NodeType: ExpressionType.LessThan } numLt
                when ExpressionHelpers.IsModelMember(numLt.Left) && ExpressionHelpers.IsNumericType(numLt.Left.Type)
                     && ExpressionHelpers.TryEvaluate(numLt.Right, out var exMax):
                constraints["exclusiveMaximum"] = exMax;
                break;

            // ── Inverted numeric (constant on left) ─────────────────────
            // N <= m.NumProp → same as m.NumProp >= N → minimum: N
            case BinaryExpression { NodeType: ExpressionType.LessThanOrEqual } numLteInv
                when ExpressionHelpers.IsModelMember(numLteInv.Right) && ExpressionHelpers.IsNumericType(numLteInv.Right.Type)
                     && ExpressionHelpers.TryEvaluate(numLteInv.Left, out var numMinInv):
                constraints["minimum"] = numMinInv;
                break;

            // N < m.NumProp → same as m.NumProp > N → exclusiveMinimum: N
            case BinaryExpression { NodeType: ExpressionType.LessThan } numLtInv
                when ExpressionHelpers.IsModelMember(numLtInv.Right) && ExpressionHelpers.IsNumericType(numLtInv.Right.Type)
                     && ExpressionHelpers.TryEvaluate(numLtInv.Left, out var exMinInv):
                constraints["exclusiveMinimum"] = exMinInv;
                break;

            // N >= m.NumProp → same as m.NumProp <= N → maximum: N
            case BinaryExpression { NodeType: ExpressionType.GreaterThanOrEqual } numGteInv
                when ExpressionHelpers.IsModelMember(numGteInv.Right) && ExpressionHelpers.IsNumericType(numGteInv.Right.Type)
                     && ExpressionHelpers.TryEvaluate(numGteInv.Left, out var numMaxInv):
                constraints["maximum"] = numMaxInv;
                break;

            // N > m.NumProp → same as m.NumProp < N → exclusiveMaximum: N
            case BinaryExpression { NodeType: ExpressionType.GreaterThan } numGtInv
                when ExpressionHelpers.IsModelMember(numGtInv.Right) && ExpressionHelpers.IsNumericType(numGtInv.Right.Type)
                     && ExpressionHelpers.TryEvaluate(numGtInv.Left, out var exMaxInv):
                constraints["exclusiveMaximum"] = exMaxInv;
                break;

            // ── Numeric equality → const ────────────────────────────────
            // m.NumProp == N → const: N
            case BinaryExpression { NodeType: ExpressionType.Equal } eqNum
                when ExpressionHelpers.IsModelMember(eqNum.Left) && ExpressionHelpers.IsNumericType(eqNum.Left.Type)
                     && ExpressionHelpers.TryEvaluate(eqNum.Right, out var constNum):
                constraints["const"] = constNum;
                break;

            case BinaryExpression { NodeType: ExpressionType.Equal } eqNumR
                when ExpressionHelpers.IsModelMember(eqNumR.Right) && ExpressionHelpers.IsNumericType(eqNumR.Right.Type)
                     && ExpressionHelpers.TryEvaluate(eqNumR.Left, out var constNumR):
                constraints["const"] = constNumR;
                break;

            // ── String equality → const ─────────────────────────────────
            // m.Prop == "value" → const: "value"
            case BinaryExpression { NodeType: ExpressionType.Equal } eqStr
                when ExpressionHelpers.IsModelMember(eqStr.Left) && eqStr.Left.Type == typeof(string)
                     && ExpressionHelpers.TryEvaluateAs<string>(eqStr.Right, out var strConst):
                constraints["const"] = strConst;
                break;

            case BinaryExpression { NodeType: ExpressionType.Equal } eqStrR
                when ExpressionHelpers.IsModelMember(eqStrR.Right) && eqStrR.Right.Type == typeof(string)
                     && ExpressionHelpers.TryEvaluateAs<string>(eqStrR.Left, out var strConstR):
                constraints["const"] = strConstR;
                break;

            // ── Collection .Count / .Length ──────────────────────────────
            // m.Items.Count >= N → minItems: N
            case BinaryExpression { NodeType: ExpressionType.GreaterThanOrEqual } colGte
                when ExpressionHelpers.IsCollectionCount(colGte.Left) && ExpressionHelpers.TryEvaluateAs<int>(colGte.Right, out var minItems):
                constraints["minItems"] = minItems;
                break;

            // m.Items.Count > N → minItems: N + 1
            case BinaryExpression { NodeType: ExpressionType.GreaterThan } colGt
                when ExpressionHelpers.IsCollectionCount(colGt.Left) && ExpressionHelpers.TryEvaluateAs<int>(colGt.Right, out var minItemsP):
                constraints["minItems"] = minItemsP + 1;
                break;

            // m.Items.Count <= N → maxItems: N
            case BinaryExpression { NodeType: ExpressionType.LessThanOrEqual } colLte
                when ExpressionHelpers.IsCollectionCount(colLte.Left) && ExpressionHelpers.TryEvaluateAs<int>(colLte.Right, out var maxItems):
                constraints["maxItems"] = maxItems;
                break;

            // m.Items.Count < N → maxItems: N - 1
            case BinaryExpression { NodeType: ExpressionType.LessThan } colLt
                when ExpressionHelpers.IsCollectionCount(colLt.Left) && ExpressionHelpers.TryEvaluateAs<int>(colLt.Right, out var maxItemsM):
                constraints["maxItems"] = maxItemsM - 1;
                break;

            // ── String.Contains → pattern (best-effort) ─────────────────
            // m.Email.Contains("@") → pattern: ".*@.*"
            case MethodCallExpression containsCall
                when ExpressionHelpers.TryGetStringInstanceCallArg(containsCall, "Contains", out _, out var containsVal):
                constraints["pattern"] = ".*" + Regex.Escape(containsVal) + ".*";
                break;

            // ── String.StartsWith → pattern ─────────────────────────────
            case MethodCallExpression startsCall
                when ExpressionHelpers.TryGetStringInstanceCallArg(startsCall, "StartsWith", out _, out var startsVal):
                constraints["pattern"] = "^" + Regex.Escape(startsVal);
                break;

            // ── String.EndsWith → pattern ───────────────────────────────
            case MethodCallExpression endsCall
                when ExpressionHelpers.TryGetStringInstanceCallArg(endsCall, "EndsWith", out _, out var endsVal):
                constraints["pattern"] = Regex.Escape(endsVal) + "$";
                break;

            // ── Logical combinators ─────────────────────────────────────
            // cond1 && cond2 → both are success conditions
            case BinaryExpression { NodeType: ExpressionType.AndAlso } and:
                ExtractFromSuccessCondition(and.Left, constraints, ref required);
                ExtractFromSuccessCondition(and.Right, constraints, ref required);
                break;

            // cond1 || cond2 → either succeeds (best-effort: extract from both)
            case BinaryExpression { NodeType: ExpressionType.OrElse } or:
                ExtractFromSuccessCondition(or.Left, constraints, ref required);
                ExtractFromSuccessCondition(or.Right, constraints, ref required);
                break;
        }
    }
}
