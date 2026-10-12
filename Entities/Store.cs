namespace SariSariPOS.API.Entities;

/// <summary>
/// Tenant root. One row per sari-sari store.
/// NOT an ITenantEntity — it *defines* the tenant.
/// </summary>
public class Store
{
    public Guid Id { get; set; }

    /// <summary>Short, human-typeable code, e.g. "SAR1A2B3". Unique globally.</summary>
    public string Code { get; set; } = default!;

    public string Name { get; set; } = default!;

    /// <summary>Owner's User.Id (convenience FK for "who created this store").</summary>
    public Guid OwnerUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}