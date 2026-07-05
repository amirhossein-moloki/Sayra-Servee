using Sayra.Server.Application.Interfaces;
using Sayra.Server.Persistence.Entities;
using Sayra.Server.ProductionHardening.CircuitBreaker;

namespace Sayra.Server.ProductionHardening.CircuitBreaker;

public class SessionRepositoryDecorator : ISessionRepository
{
    private readonly ISessionRepository _inner;
    private readonly DbCircuitBreaker _circuitBreaker;

    public SessionRepositoryDecorator(ISessionRepository inner, DbCircuitBreaker circuitBreaker)
    {
        _inner = inner;
        _circuitBreaker = circuitBreaker;
    }

    public Task AddAsync(SessionEntity session) =>
        _circuitBreaker.ExecuteAsync(() => _inner.AddAsync(session));

    public Task<SessionEntity?> GetByIdAsync(string sessionId) =>
        _circuitBreaker.ExecuteAsync(() => _inner.GetByIdAsync(sessionId));

    public Task<IEnumerable<SessionEntity>> GetAllAsync() =>
        _circuitBreaker.ExecuteAsync(() => _inner.GetAllAsync());

    public Task UpdateAsync(SessionEntity session) =>
        _circuitBreaker.ExecuteAsync(() => _inner.UpdateAsync(session));
}
