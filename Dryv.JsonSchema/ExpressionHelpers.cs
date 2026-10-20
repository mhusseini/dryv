using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace Dryv.JsonSchema;

/// <summary>
/// Static utility methods for inspecting and evaluating expression trees
/// during JSON Schema constraint extraction.
/// </summary>
internal static class ExpressionHelpers
{
    internal static Expression UnwrapConvert(Expression expression)
    {
        while (expression is UnaryExpression { NodeType: ExpressionType.Convert } convert)
        {
            expression = convert.Operand;
        }
        
        return expression;
    }

    /// <summary>
    /// Unwraps a chain of method calls like <c>.Trim()</c>, <c>.ToLower()</c>, etc.
    /// to reveal the underlying member access (e.g. <c>m.Prop</c>).
    /// </summary>
    private static Expression UnwrapStringTransforms(Expression expression)
    {
        while (expression is MethodCallExpression { Method.IsStatic: false, Object: not null } call
               && call.Method.DeclaringType == typeof(string)
               && call.Method.Name is "Trim" or "TrimStart" or "TrimEnd"
                   or "ToLower" or "ToLowerInvariant" or "ToUpper" or "ToUpperInvariant")
        {
            expression = call.Object;
        }

        return expression;
    }

    internal static bool IsSuccessResult(Expression expression)
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

    internal static bool IsNullConstant(Expression expression) =>
        expression is ConstantExpression { Value: null } or DefaultExpression { Type.IsValueType: false };

    internal static bool IsStringLength(Expression expression)
    {
        if (expression is not MemberExpression { Member.Name: "Length" } m)
        {
            return false;
        }

        // Direct: m.Prop.Length
        if (m.Expression?.Type == typeof(string))
        {
            return true;
        }

        // Through transforms: m.Prop.Trim().Length
        return m.Expression != null && UnwrapStringTransforms(m.Expression).Type == typeof(string);
    }

    internal static bool IsModelMember(Expression expression) =>
        expression is MemberExpression { Expression: ParameterExpression };

    /// <summary>
    /// Returns true if the expression accesses a model member, possibly through
    /// intermediate method calls like <c>.Trim()</c>.
    /// </summary>
    private static bool IsModelMemberOrTransform(Expression expression)
    {
        return IsModelMember(expression) || expression switch
        {
            // m.Prop.Trim(), m.Prop.ToLower(), etc.
            MethodCallExpression { Method.IsStatic: false, Object: not null } call => IsModelMemberOrTransform(call.Object),
            // m.Prop.Length, m.Prop.Count, etc.
            MemberExpression { Expression: { } inner } => IsModelMemberOrTransform(inner),
            _ => false
        };
    }

    internal static bool IsNumericType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type == typeof(int) || type == typeof(long) || type == typeof(double)
               || type == typeof(float) || type == typeof(decimal) || type == typeof(short)
               || type == typeof(byte);
    }

    internal static bool IsBooleanMember(Expression expression) =>
        expression is MemberExpression { Expression: ParameterExpression } m
        && (m.Type == typeof(bool) || m.Type == typeof(bool?));

    internal static bool IsCollectionCount(Expression expression)
    {
        if (expression is not MemberExpression { Member.Name: "Count" or "Length" } m)
        {
            return false;
        }

        var ownerType = m.Expression?.Type;
        if (ownerType == typeof(string))
        {
            return false; // string.Length is handled by IsStringLength
        }

        return ownerType != null && IsCollectionType(ownerType);
    }

    private static bool IsCollectionType(Type type)
    {
        if (type.IsArray)
        {
            return true;
        }

        if (!type.IsGenericType)
        {
            return typeof(System.Collections.ICollection).IsAssignableFrom(type)
                   || typeof(System.Collections.IEnumerable).IsAssignableFrom(type);
        }
        
        var genDef = type.GetGenericTypeDefinition();
        if (genDef == typeof(List<>) || genDef == typeof(IList<>)
                                     || genDef == typeof(ICollection<>) || genDef == typeof(IEnumerable<>)
                                     || genDef == typeof(IReadOnlyList<>) || genDef == typeof(IReadOnlyCollection<>)
                                     || genDef == typeof(HashSet<>))
        {
            return true;
        }

        return typeof(System.Collections.ICollection).IsAssignableFrom(type)
               || typeof(System.Collections.IEnumerable).IsAssignableFrom(type);
    }

    internal static bool IsRegexMethod(MethodCallExpression call) =>
        call.Method.DeclaringType == typeof(Regex);

    internal static bool IsStringMethodCall(MethodCallExpression call, string methodName) =>
        call.Method.DeclaringType == typeof(string) && call.Method.Name == methodName;

    internal static bool TryEvaluate(Expression expression, out object? value)
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

    internal static bool TryEvaluateAs<T>(Expression expression, out T value)
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

    internal static bool TryExtractRegexPattern(MethodCallExpression call, out string pattern)
    {
        switch (call.Method.IsStatic)
        {
            // Instance: new Regex(pattern).IsMatch(input)
            case false when call.Object is NewExpression { Arguments.Count: >= 1 } newExpr
                            && TryEvaluateAs<string>(newExpr.Arguments[0], out var newPattern):
                pattern = newPattern;
                return true;
            // Instance from captured variable: regexVar.IsMatch(input)
            case false when call.Object != null
                            && TryEvaluate(call.Object, out var regexObj) && regexObj is Regex regex:
                pattern = regex.ToString();
                return true;
            // Static: Regex.IsMatch(input, pattern) — pattern is the evaluable non-model argument
            case true when call.Arguments.Count >= 2:
            {
                foreach (var arg in call.Arguments)
                {
                    if (arg.Type != typeof(string) || ContainsParameter(arg)
                                                   || !TryEvaluateAs<string>(arg, out var staticPattern))
                    {
                        continue;
                    }

                    pattern = staticPattern;
                    return true;
                }

                break;
            }
        }

        pattern = null!;
        return false;
    }

    private static bool ContainsParameter(Expression expression)
    {
        var finder = new ExpressionVisitors.ParameterFinder();
        finder.Visit(expression);
        return finder.Found;
    }

    /// <summary>
    /// Attempts to extract the model property argument from a string instance method call
    /// like <c>m.Prop.Contains("@")</c>.
    /// </summary>
    internal static bool TryGetStringInstanceCallArg(
        MethodCallExpression call, string methodName,
        out Expression? objectExpr, out string argValue)
    {
        objectExpr = null;
        argValue = null!;

        if (call.Method.IsStatic || call.Method.Name != methodName
                                 || call.Method.DeclaringType != typeof(string))
        {
            return false;
        }

        if (call.Object == null || !IsModelMemberOrTransform(call.Object))
        {
            return false;
        }

        if (call.Arguments.Count < 1 || !TryEvaluateAs<string>(call.Arguments[0], out var val))
        {
            return false;
        }
        
        objectExpr = call.Object;
        argValue = val;
        return true;
    }
}