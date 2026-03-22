namespace LottyAB.Application.Targeting.Conditions;

public class AndCondition(IEnumerable<ICondition> conditions) : ICondition
{
    public bool Evaluate(Dictionary<string, object> attributes) => conditions.All(c => c.Evaluate(attributes));
}