using LottyAB.Domain.Entities;
using MediatR;

namespace LottyAB.Application.Commands.Experiments;

public record SubmitForReviewCommand(Guid ExperimentId) : IRequest<ExperimentEntity>;