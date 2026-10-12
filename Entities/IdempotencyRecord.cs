using SariSariPOS.API.Interfaces;

namespace SariSariPOS.API.Entities;

public class IdempotencyRecord : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public string ClientId { get; set; } = default!;
    public string ResponseJson { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}