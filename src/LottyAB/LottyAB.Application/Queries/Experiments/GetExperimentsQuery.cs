using LottyAB.Contracts.Responses;
using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using MediatR;

namespace LottyAB.Application.Queries.Experiments;

public record GetExperimentsQuery(
    int PageNumber,
    int PageSize,
    EExperimentStatus? Status = null,
    Guid? FeatureFlagId = null) : IRequest<PagedResponse<ExperimentEntity>>;