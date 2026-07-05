using Microsoft.EntityFrameworkCore;
using Sayra.Server.Persistence.Entities;
using Sayra.Server.MultiSite.Interfaces;

namespace Sayra.Server.Persistence;

public class SayraDbContext : DbContext
{
    private readonly ISiteContext? _siteContext;

    public SayraDbContext(DbContextOptions<SayraDbContext> options, ISiteContext? siteContext = null) : base(options)
    {
        _siteContext = siteContext;
    }

    public DbSet<ClientEntity> Clients => Set<ClientEntity>();
    public DbSet<SessionEntity> Sessions => Set<SessionEntity>();
    public DbSet<CommandAuditEntity> CommandAudits => Set<CommandAuditEntity>();
    public DbSet<TelemetryEntity> Telemetries => Set<TelemetryEntity>();
    public DbSet<AdminUserEntity> AdminUsers => Set<AdminUserEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var currentSiteId = _siteContext?.CurrentSiteId;

        modelBuilder.Entity<ClientEntity>(entity =>
        {
            entity.HasKey(e => e.PcId);
            entity.Property(e => e.MacAddress).IsRequired();
            entity.HasQueryFilter(e => _siteContext == null || e.SiteId == _siteContext.CurrentSiteId);
        });

        modelBuilder.Entity<SessionEntity>(entity =>
        {
            entity.HasKey(e => e.SessionId);
            entity.HasOne(e => e.Client)
                .WithMany()
                .HasForeignKey(e => e.PcId);
            entity.HasQueryFilter(e => _siteContext == null || e.SiteId == _siteContext.CurrentSiteId);
        });

        modelBuilder.Entity<CommandAuditEntity>(entity =>
        {
            entity.HasKey(e => e.CommandId);
            entity.HasQueryFilter(e => _siteContext == null || e.SiteId == _siteContext.CurrentSiteId);
        });

        modelBuilder.Entity<TelemetryEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasQueryFilter(e => _siteContext == null || e.SiteId == _siteContext.CurrentSiteId);
        });

        modelBuilder.Entity<AdminUserEntity>(entity =>
        {
            entity.HasKey(e => e.AdminId);
            entity.HasIndex(e => e.Username).IsUnique();
        });
    }
}
