using LottyAB.Domain.Entities;
using MediatR;

namespace LottyAB.Application.Queries.Autopilot;

public record GetRampPlanHistoryQuery(Guid ExperimentId) : IRequest<List<RampPlanHistoryEntity>>;