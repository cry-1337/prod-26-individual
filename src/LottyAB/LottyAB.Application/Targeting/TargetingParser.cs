using LottyAB.Application.Exceptions;
using LottyAB.Application.Targeting.Conditions;
using LottyAB.Application.Targeting.Operators;

namespace LottyAB.Application.Targeting;

public class TargetingParser(IValueComparer valueComparer) : ITargetingParser
{
    public ICondition Parse(string rule) => string.IsNullOrWhiteSpace(rule)
        ? throw new UnprocessableEntityException("Rule cannot be empty", nameof(rule))
        : ParseExpression(rule.Trim());

    private ICondition ParseExpression(string expr)
    {
        while (true)
        {
            expr = expr.Trim();

            if (expr.StartsWith("NOT ", StringComparison.OrdinalIgnoreCase))
            {
                var innerExpr = expr[4..].Trim();
                return new NotCondition(ParseExpression(innerExpr));
            }

            if (expr.StartsWith('(') && expr.EndsWith(')'))
            {
                expr = expr.Substring(1, expr.Length - 2);
                continue;
            }

            var orParts = SplitByOperator(expr, " OR ");
            if (orParts.Count > 1)
            {
                var conditions = orParts.Select(ParseExpression);
                return new OrCondition(conditions);
            }

            var andParts = SplitByOperator(expr, " AND ");
            if (andParts.Count <= 1) return ParseComparison(expr);
            {
                var conditions = andParts.Select(ParseExpression);
                return new AndCondition(conditions);
            }
        }
    }

    private static List<string> SplitByOperator(string expr, string op)
    {
        var parts = new List<string>();
        var current = "";
        var depth = 0;
        var i = 0;

        while (i < expr.Length)
        {
            switch (expr[i])
            {
                case '(':
                    depth++;
                    current += expr[i];
                    i++;
                    break;
                case ')':
                    depth--;
                    current += expr[i];
                    i++;
                    break;
                default:
                    {
                        if (depth == 0 && i + op.Length <= expr.Length &&
                            expr.Substring(i, op.Length).Equals(op, StringComparison.OrdinalIgnoreCase))
                        {
                            parts.Add(current.Trim());
                            current = "";
                            i += op.Length;
                        }
                        else
                        {
                            current += expr[i];
                            i++;
                        }

                        break;
                    }
            }
        }

        if (!string.IsNullOrWhiteSpace(current))
            parts.Add(current.Trim());

        return parts.Count > 1 ? parts : [expr];
    }

    private ComparisonCondition ParseComparison(string comparison)
    {
        comparison = comparison.Trim();

        if (TrySplitComparison(comparison, " NOT IN ", out var notInParts))
        {
            var attributeName = notInParts[0].Trim();
            var value = ParseArrayValue(notInParts[1].Trim());
            var op = new NotInOperator(valueComparer);
            return new ComparisonCondition(attributeName, op, value);
        }

        if (TrySplitComparison(comparison, " IN ", out var inParts))
        {
            var attributeName = inParts[0].Trim();
            var value = ParseArrayValue(inParts[1].Trim());
            var op = new InOperator(valueComparer);
            return new ComparisonCondition(attributeName, op, value);
        }

        var operators = new[] { ">=", "<=", "==", "!=", ">", "<" };
        foreach (var opStr in operators)
        {
            if (!TrySplitComparison(comparison, $" {opStr} ", out var parts)) continue;

            var attributeName = parts[0].Trim();
            var value = ParseScalarValue(parts[1].Trim());
            var op = CreateOperator(opStr);

            return new ComparisonCondition(attributeName, op, value);
        }

        throw new UnprocessableEntityException($"Invalid comparison: {comparison}");
    }

    private static bool TrySplitComparison(string comparison, string op, out string[] parts)
    {
        if (comparison.Contains(op, StringComparison.OrdinalIgnoreCase))
        {
            parts = comparison.Split([op], StringSplitOptions.None);
            return parts.Length == 2;
        }

        parts = [];
        return false;
    }

    private IComparisonOperator CreateOperator(string op)
    {
        return op switch
        {
            "==" => new EqualsOperator(valueComparer),
            "!=" => new NotEqualsOperator(valueComparer),
            ">" => new GreaterThanOperator(valueComparer),
            ">=" => new GreaterThanOrEqualOperator(valueComparer),
            "<" => new LessThanOperator(valueComparer),
            "<=" => new LessThanOrEqualOperator(valueComparer),
            _ => throw new UnprocessableEntityException($"Unknown operator: {op}")
        };
    }

    private static List<object> ParseArrayValue(string value)
    {
        value = value.Trim();
        if (!value.StartsWith('[') || !value.EndsWith(']'))
            throw new UnprocessableEntityException("Array value must be enclosed in brackets");

        var content = value.Substring(1, value.Length - 2);
        var items = content.Split(',').Select(s => ParseScalarValue(s.Trim())).ToList();
        return items;
    }

    private static object ParseScalarValue(string value)
    {
        value = value.Trim();

        if (value.StartsWith('\"') && value.EndsWith('\"'))
            return value.Substring(1, value.Length - 2);

        if (bool.TryParse(value, out var boolValue))
            return boolValue;

        if (double.TryParse(value, out var doubleValue))
            return doubleValue;

        if (DateTime.TryParse(value, out var dateValue))
            return dateValue;

        return value;
    }
}