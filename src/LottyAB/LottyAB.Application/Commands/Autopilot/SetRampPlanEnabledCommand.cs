using LottyAB.Domain.Entities;
using MediatR;

namespace LottyAB.Application.Commands.Autopilot;

public record SetRampPlanEnabledCommand(Guid ExperimentId, bool IsEnabled) : IRequest<RampPlanEntity>;