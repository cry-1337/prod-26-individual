using LottyAB.Application.Interfaces;
using LottyAB.Application.Targeting;

namespace LottyAB.Application.Services;

public class TargetingEvaluatorService(ITargetingParser parser) : ITargetingEvaluator
{
    public bool EvaluateRule(string? targetingRule, Dictionary<string, object>? subjectAttributes)
    {
        if (string.IsNullOrWhiteSpace(targetingRule))
            return true;

        if (subjectAttributes == null || subjectAttributes.Count == 0)
            return false;

        try
        {
            var condition = parser.Parse(targetingRule);
            return condition.Evaluate(subjectAttributes);
        }
        catch
        {
            return false;
        }
    }
}