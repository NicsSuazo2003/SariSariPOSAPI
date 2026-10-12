namespace SariSariPOS.API.Interfaces;

/// <summary>
/// Marks an entity as belonging to a single store.
/// AppDbContext applies a global query filter on StoreId and
/// auto-stamps StoreId on insert from ITenantContext.
/// </summary>
public interface ITenantEntity
{
    Guid StoreId { get; set; }
}