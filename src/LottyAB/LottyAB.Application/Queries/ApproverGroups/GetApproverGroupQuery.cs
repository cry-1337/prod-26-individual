using LottyAB.Domain.Entities;
using MediatR;

namespace LottyAB.Application.Queries.ApproverGroups;

public record GetApproverGroupQuery(Guid Id) : IRequest<ApproverGroupEntity>;