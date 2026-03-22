using System.Collections;

namespace LottyAB.Application.Targeting.Operators;

public class InOperator(IValueComparer comparer) : IComparisonOperator
{
    public bool Compare(object? leftValue, object? rightValue) =>
        rightValue is IEnumerable enumerable && enumerable.Cast<object?>()
            .Any(item => comparer.AreEqual(leftValue, item));
}