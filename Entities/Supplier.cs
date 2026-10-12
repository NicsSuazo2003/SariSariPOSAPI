using SariSariPOS.API.Interfaces;

namespace SariSariPOS.API.Entities;

public class Supplier : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public string Name { get; set; } = default!;
    public string? Contact { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; }
}