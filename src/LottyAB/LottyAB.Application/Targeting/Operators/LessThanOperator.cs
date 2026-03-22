namespace LottyAB.Application.Targeting.Operators;

public class LessThanOperator(IValueComparer comparer) : IComparisonOperator
{
    public bool Compare(object? leftValue, object? rightValue) => comparer.Compare(leftValue, rightValue) < 0;
}