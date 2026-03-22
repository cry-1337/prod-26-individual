namespace LottyAB.Application.Targeting.Operators;

public class GreaterThanOrEqualOperator(IValueComparer comparer) : IComparisonOperator
{
    public bool Compare(object? leftValue, object? rightValue) => comparer.Compare(leftValue, rightValue) >= 0;
}