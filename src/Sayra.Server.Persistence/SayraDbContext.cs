using Microsoft.EntityFrameworkCore;
using Sayra.Server.Persistence.Entities;

namespace Sayra.Server.Persistence;

public class SayraDbContext : DbContext
{
    public SayraDbContext(DbContextOptions<SayraDbContext> options) : base(options)
    {
    }

    public DbSet<ClientEntity> Clients => Set<ClientEntity>();
    public DbSet<SessionEntity> Sessions => Set<SessionEntity>();
    public DbSet<CommandAuditEntity> CommandAudits => Set<CommandAuditEntity>();
    public DbSet<TelemetryEntity> Telemetries => Set<TelemetryEntity>();
    public DbSet<AdminUserEntity> AdminUsers => Set<AdminUserEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ClientEntity>(entity =>
        {
            entity.HasKey(e => e.PcId);
            entity.Property(e => e.MacAddress).IsRequired();
        });

        modelBuilder.Entity<SessionEntity>(entity =>
        {
            entity.HasKey(e => e.SessionId);
            entity.HasOne(e => e.Client)
                .WithMany()
                .HasForeignKey(e => e.PcId);
        });

        modelBuilder.Entity<CommandAuditEntity>(entity =>
        {
            entity.HasKey(e => e.CommandId);
        });

        modelBuilder.Entity<TelemetryEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<AdminUserEntity>(entity =>
        {
            entity.HasKey(e => e.AdminId);
            entity.HasIndex(e => e.Username).IsUnique();
        });
    }
}
