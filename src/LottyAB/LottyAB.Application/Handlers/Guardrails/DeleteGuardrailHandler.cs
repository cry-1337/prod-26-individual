using LottyAB.Application.Commands.Guardrails;
using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Guardrails;

public class DeleteGuardrailHandler(IApplicationDbContext dbContext)
    : IRequestHandler<DeleteGuardrailCommand>
{
    public async Task Handle(DeleteGuardrailCommand request, CancellationToken cancellationToken)
    {
        var guardrail = await dbContext.Guardrails
            .Include(g => g.Experiment)
            .FirstOrDefaultAsync(g => g.Id == request.GuardrailId, cancellationToken);

        if (guardrail == null)
            throw new NotFoundException($"Guardrail with ID '{request.GuardrailId}' not found");

        dbContext.Guardrails.Remove(guardrail);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}