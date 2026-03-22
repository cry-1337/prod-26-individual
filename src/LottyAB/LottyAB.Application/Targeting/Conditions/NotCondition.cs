namespace LottyAB.Application.Targeting.Conditions;

public class NotCondition(ICondition condition) : ICondition
{
    public bool Evaluate(Dictionary<string, object> attributes) => !condition.Evaluate(attributes);
}