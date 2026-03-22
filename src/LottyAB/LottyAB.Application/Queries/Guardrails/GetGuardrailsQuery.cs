using LottyAB.Contracts.Responses;
using MediatR;

namespace LottyAB.Application.Queries.Guardrails;

public record GetGuardrailsQuery(Guid ExperimentId) : IRequest<List<GuardrailResponse>>;