using LottyAB.Contracts.Request.Events;
using LottyAB.Contracts.Responses;
using LottyAB.Contracts.Responses.Events;
using MediatR;

namespace LottyAB.Application.Commands.Events;

public record SendEventsCommand(SendEventsRequest Request) : IRequest<SendEventsResponse>;