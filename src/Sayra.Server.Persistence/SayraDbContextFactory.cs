using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sayra.Server.Persistence;

public class SayraDbContextFactory : IDesignTimeDbContextFactory<SayraDbContext>
{
    public SayraDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SayraDbContext>();
        // Use SqlServer for design-time (migrations) as requested in TECH STACK
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=SayraDb;Trusted_Connection=True;MultipleActiveResultSets=true");

        return new SayraDbContext(optionsBuilder.Options);
    }
}
