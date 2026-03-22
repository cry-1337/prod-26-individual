using LottyAB.Contracts.Responses;
using MediatR;

namespace LottyAB.Application.Commands.Auth;

public record LoginCommand(string Email, string Password) : IRequest<LoginResponse>;