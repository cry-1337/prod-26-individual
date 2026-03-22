using LottyAB.Contracts.Responses;
using MediatR;

namespace LottyAB.Application.Queries.Guardrails;

public record GetGuardrailTriggerHistoryQuery(Guid ExperimentId) : IRequest<List<GuardrailTriggerHistoryResponse>>;