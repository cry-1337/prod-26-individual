namespace LottyAB.Application.Targeting.Operators;

public class EqualsOperator(IValueComparer comparer) : IComparisonOperator
{
    public bool Compare(object? leftValue, object? rightValue) => comparer.AreEqual(leftValue, rightValue);
}