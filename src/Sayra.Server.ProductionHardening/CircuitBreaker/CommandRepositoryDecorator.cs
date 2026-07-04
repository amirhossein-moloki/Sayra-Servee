using Sayra.Server.Application.Interfaces;
using Sayra.Server.Persistence.Entities;
using Sayra.Server.ProductionHardening.CircuitBreaker;

namespace Sayra.Server.ProductionHardening.CircuitBreaker;

public class CommandRepositoryDecorator : ICommandRepository
{
    private readonly ICommandRepository _inner;
    private readonly DbCircuitBreaker _circuitBreaker;

    public CommandRepositoryDecorator(ICommandRepository inner, DbCircuitBreaker circuitBreaker)
    {
        _inner = inner;
        _circuitBreaker = circuitBreaker;
    }

    public Task AddAsync(CommandAuditEntity command) =>
        _circuitBreaker.ExecuteAsync(() => _inner.AddAsync(command));

    public Task<IEnumerable<CommandAuditEntity>> GetByPcIdAsync(string pcId) =>
        _circuitBreaker.ExecuteAsync(() => _inner.GetByPcIdAsync(pcId));
}
