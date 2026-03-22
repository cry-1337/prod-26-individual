namespace LottyAB.Application.Targeting.Conditions;

public class OrCondition(IEnumerable<ICondition> conditions) : ICondition
{
    public bool Evaluate(Dictionary<string, object> attributes) => conditions.Any(c => c.Evaluate(attributes));
}