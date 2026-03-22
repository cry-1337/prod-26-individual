using LottyAB.Contracts.Responses;
using MediatR;

namespace LottyAB.Application.Commands.Auth;

public record RegisterCommand(string Name, string Email, string Password) : IRequest<LoginResponse>;