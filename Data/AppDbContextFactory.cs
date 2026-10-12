using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SariSariPOS.API.Interfaces;

namespace SariSariPOS.API.Data;

/// <summary>
/// Design-time factory used by `dotnet ef migrations` / `dotnet ef database update`.
///
/// Migrations run outside an HTTP request, so there is no JWT and no
/// ambient tenant. We inject a NullTenantContext (StoreId = Guid.Empty)
/// so the AppDbContext can be constructed. Global query filters are
/// effectively disabled during migrations, which is what we want.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connStr = Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? config.GetConnectionString("Default")
            ?? "Host=localhost;Database=sarisari_pos;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connStr)
            .Options;

        return new AppDbContext(options, new NullTenantContext());
    }
}

/// <summary>
/// Design-time only. Provides a "no tenant" context so the AppDbContext
/// constructor is satisfied when there is no HTTP request.
/// </summary>
internal sealed class NullTenantContext : ITenantContext
{
    public Guid StoreId => Guid.Empty;
    public Guid UserId => Guid.Empty;
    public bool IsAuthenticated => false;
}