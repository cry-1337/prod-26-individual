using LottyAB.Contracts.Responses;
using LottyAB.Domain.Entities;
using MediatR;

namespace LottyAB.Application.Queries.ApproverGroups;

public record GetApproverGroupsQuery(int Page, int Size) : IRequest<PagedResponse<ApproverGroupEntity>>;