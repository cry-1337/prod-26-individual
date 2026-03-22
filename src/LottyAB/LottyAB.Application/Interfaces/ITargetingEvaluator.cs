namespace LottyAB.Application.Interfaces;

public interface ITargetingEvaluator
{
    bool EvaluateRule(string? targetingRule, Dictionary<string, object>? subjectAttributes);
}