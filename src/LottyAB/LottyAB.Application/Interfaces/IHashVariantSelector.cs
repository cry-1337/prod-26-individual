using LottyAB.Domain.Entities;

namespace LottyAB.Application.Interfaces;

public interface IHashVariantSelector
{
    VariantEntity? SelectVariant(string subjectId, ExperimentEntity experiment);
}