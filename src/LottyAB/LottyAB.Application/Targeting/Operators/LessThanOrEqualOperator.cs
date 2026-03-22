namespace LottyAB.Application.Targeting.Operators;

public class LessThanOrEqualOperator(IValueComparer comparer) : IComparisonOperator
{
    public bool Compare(object? leftValue, object? rightValue) => comparer.Compare(leftValue, rightValue) <= 0;
}