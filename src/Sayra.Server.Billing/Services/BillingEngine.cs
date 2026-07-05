using Sayra.Server.Billing.Models;
using Sayra.Server.Application.Interfaces;
using Sayra.Server.Domain.Entities;
using Sayra.Server.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Sayra.Server.Billing.Services;

public interface IBillingEngine
{
    decimal CalculateCost(DateTime start, DateTime? end, decimal ratePerHour, decimal minimumCharge);
    Task<BillingSession> StartSessionAsync(string siteId, string pcId, PricePlan plan);
    Task<BillingSession> EndSessionAsync(string sessionId, DateTime endTime);
}

public class BillingEngine : IBillingEngine
{
    private readonly IDbContextFactory<SayraDbContext> _dbFactory;

    public BillingEngine(IDbContextFactory<SayraDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public decimal CalculateCost(DateTime start, DateTime? end, decimal ratePerHour, decimal minimumCharge)
    {
        var actualEnd = end ?? DateTime.UtcNow;
        var duration = actualEnd - start;
        var hours = (decimal)duration.TotalHours;

        var cost = hours * ratePerHour;
        return Math.Max(cost, minimumCharge);
    }

    public async Task<BillingSession> StartSessionAsync(string siteId, string pcId, PricePlan plan)
    {
        using var context = await _dbFactory.CreateDbContextAsync();

        var session = new BillingSession
        {
            SessionId = Guid.NewGuid().ToString(),
            PcId = pcId,
            StartTime = DateTime.UtcNow,
            PlanId = plan.Id,
            IsPaid = false
        };

        // Persist to database for crash recovery
        var entity = new Sayra.Server.Persistence.Entities.SessionEntity
        {
            SessionId = session.SessionId,
            SiteId = siteId,
            PcId = pcId,
            StartTime = session.StartTime,
            Status = "Active",
            PricePlanId = plan.Id,
            RatePerHour = plan.RatePerHour
        };

        context.Sessions.Add(entity);
        await context.SaveChangesAsync();

        return session;
    }

    public async Task<BillingSession> EndSessionAsync(string sessionId, DateTime endTime)
    {
        using var context = await _dbFactory.CreateDbContextAsync();

        var entity = await context.Sessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
        if (entity == null) throw new KeyNotFoundException("Session not found");

        entity.EndTime = endTime;
        entity.Status = "Ended";
        entity.CurrentCost = CalculateCost(entity.StartTime, endTime, entity.RatePerHour, 0);

        await context.SaveChangesAsync();

        return new BillingSession
        {
            SessionId = entity.SessionId,
            PcId = entity.PcId,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            TotalAmount = entity.CurrentCost,
            PlanId = entity.PricePlanId ?? "",
            IsPaid = true
        };
    }
}
