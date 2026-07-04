using Sayra.Server.Persistence.Entities;

namespace Sayra.Server.Application.Interfaces;

public interface IClientRepository
{
    Task<ClientEntity?> GetByPcIdAsync(string pcId);
    Task<IEnumerable<ClientEntity>> GetAllAsync();
    Task UpsertAsync(ClientEntity client);
}

public interface ISessionRepository
{
    Task<SessionEntity?> GetByIdAsync(string sessionId);
    Task<IEnumerable<SessionEntity>> GetAllAsync();
    Task AddAsync(SessionEntity session);
    Task UpdateAsync(SessionEntity session);
}

public interface ICommandRepository
{
    Task AddAsync(CommandAuditEntity command);
    Task<IEnumerable<CommandAuditEntity>> GetByPcIdAsync(string pcId);
}

public interface ITelemetryRepository
{
    Task AddAsync(TelemetryEntity telemetry);
    Task<IEnumerable<TelemetryEntity>> GetByPcIdAsync(string pcId, int limit = 100);
}

public interface IAdminUserRepository
{
    Task<AdminUserEntity?> GetByUsernameAsync(string username);
}
