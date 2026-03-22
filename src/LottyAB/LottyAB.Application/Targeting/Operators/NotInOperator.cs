using System.Collections;

namespace LottyAB.Application.Targeting.Operators;

public class NotInOperator(IValueComparer comparer) : IComparisonOperator
{
    public bool Compare(object? leftValue, object? rightValue) =>
        rightValue is IEnumerable enumerable && enumerable.Cast<object?>()
            .All(item => !comparer.AreEqual(leftValue, item));
}