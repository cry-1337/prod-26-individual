namespace LottyAB.Application.Targeting.Operators;

public class NotEqualsOperator(IValueComparer comparer) : IComparisonOperator
{
    public bool Compare(object? leftValue, object? rightValue) => !comparer.AreEqual(leftValue, rightValue);
}