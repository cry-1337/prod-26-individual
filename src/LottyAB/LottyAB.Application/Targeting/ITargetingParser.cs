namespace LottyAB.Application.Targeting;

public interface ITargetingParser
{
    ICondition Parse(string rule);
}