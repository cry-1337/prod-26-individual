namespace LottyAB.Application.Targeting.Conditions;

public class ComparisonCondition(string attributeName, IComparisonOperator comparisonOperator, object value) : ICondition
{
    public bool Evaluate(Dictionary<string, object> attributes) =>
        attributes.TryGetValue(attributeName, out var attributeValue) && comparisonOperator.Compare(attributeValue, value);
}