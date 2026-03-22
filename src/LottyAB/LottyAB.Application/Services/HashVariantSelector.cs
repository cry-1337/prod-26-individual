using System.Security.Cryptography;
using System.Text;
using LottyAB.Application.Interfaces;
using LottyAB.Domain.Entities;

namespace LottyAB.Application.Services;

public class HashVariantSelector : IHashVariantSelector
{
    public VariantEntity? SelectVariant(string subjectId, ExperimentEntity experiment)
    {
        if (experiment.Variants.Count == 0) return null;

        var hashInput = $"{subjectId}:{experiment.Id}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(hashInput));

        var hashValue = BitConverter.ToUInt64(hashBytes, 0);
        var normalizedHash = hashValue / (double)ulong.MaxValue;

        if (normalizedHash >= experiment.AudienceFraction) return null;

        var totalWeight = experiment.Variants.Sum(v => v.Weight);
        if (totalWeight <= 0) return null;

        var targetValue = normalizedHash * totalWeight;

        var cumulativeWeight = 0.0;
        foreach (var variant in experiment.Variants.OrderBy(v => v.Id))
        {
            cumulativeWeight += variant.Weight;
            if (targetValue < cumulativeWeight)
                return variant;
        }

        return experiment.Variants.OrderBy(v => v.Id).Last();
    }
}