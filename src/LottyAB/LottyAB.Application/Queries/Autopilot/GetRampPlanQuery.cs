using LottyAB.Domain.Entities;
using MediatR;

namespace LottyAB.Application.Queries.Autopilot;

public record GetRampPlanQuery(Guid ExperimentId) : IRequest<RampPlanEntity>;