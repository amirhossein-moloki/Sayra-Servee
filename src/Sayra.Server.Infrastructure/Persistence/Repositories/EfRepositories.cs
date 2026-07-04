using Microsoft.EntityFrameworkCore;
using Sayra.Server.Application.Interfaces;
using Sayra.Server.Persistence;
using Sayra.Server.Persistence.Entities;

namespace Sayra.Server.Infrastructure.Persistence.Repositories;

public class ClientRepository(SayraDbContext context) : IClientRepository
{
    public async Task<ClientEntity?> GetByPcIdAsync(string pcId) =>
        await context.Clients.FindAsync(pcId);

    public async Task<IEnumerable<ClientEntity>> GetAllAsync() =>
        await context.Clients.ToListAsync();

    public async Task UpsertAsync(ClientEntity client)
    {
        var existing = await context.Clients.FindAsync(client.PcId);
        if (existing == null)
        {
            await context.Clients.AddAsync(client);
        }
        else
        {
            context.Entry(existing).CurrentValues.SetValues(client);
        }
        await context.SaveChangesAsync();
    }
}

public class SessionRepository(SayraDbContext context) : ISessionRepository
{
    public async Task<SessionEntity?> GetByIdAsync(string sessionId) =>
        await context.Sessions.Include(s => s.Client).FirstOrDefaultAsync(s => s.SessionId == sessionId);

    public async Task<IEnumerable<SessionEntity>> GetAllAsync() =>
        await context.Sessions.Include(s => s.Client).ToListAsync();

    public async Task AddAsync(SessionEntity session)
    {
        await context.Sessions.AddAsync(session);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(SessionEntity session)
    {
        context.Sessions.Update(session);
        await context.SaveChangesAsync();
    }
}

public class CommandRepository(SayraDbContext context) : ICommandRepository
{
    public async Task AddAsync(CommandAuditEntity command)
    {
        await context.CommandAudits.AddAsync(command);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<CommandAuditEntity>> GetByPcIdAsync(string pcId) =>
        await context.CommandAudits.Where(c => c.PcId == pcId).OrderByDescending(c => c.Timestamp).ToListAsync();
}

public class TelemetryRepository(SayraDbContext context) : ITelemetryRepository
{
    public async Task AddAsync(TelemetryEntity telemetry)
    {
        await context.Telemetries.AddAsync(telemetry);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<TelemetryEntity>> GetByPcIdAsync(string pcId, int limit = 100) =>
        await context.Telemetries.Where(t => t.PcId == pcId).OrderByDescending(t => t.Timestamp).Take(limit).ToListAsync();
}

public class AdminUserRepository(SayraDbContext context) : IAdminUserRepository
{
    public async Task<AdminUserEntity?> GetByUsernameAsync(string username) =>
        await context.AdminUsers.FirstOrDefaultAsync(u => u.Username == username);
}
