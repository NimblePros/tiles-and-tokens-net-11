using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TilesAndTokens.Persistence;

/// <summary>Lets <c>dotnet ef migrations add</c> build the model without a running database.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TilesAndTokensDbContext>
{
    public TilesAndTokensDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<TilesAndTokensDbContext>()
            .UseNpgsql("Host=design-time-only")
            .Options);
}
