namespace LottyAB.Application.Targeting;

public interface ICondition
{
    bool Evaluate(Dictionary<string, object> attributes);
}