namespace SariSariPOS.API.Entities;

public class IdempotencyRecord
{
    public Guid Id { get; set; }
    public string ClientId { get; set; } = default!;
    public string ResponseJson { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}