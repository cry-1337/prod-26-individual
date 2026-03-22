namespace LottyAB.Application.Targeting;

public interface IComparisonOperator
{
    bool Compare(object? leftValue, object? rightValue);
}