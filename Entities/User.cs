namespace SariSariPOS.API.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string StoreName { get; set; } = default!;
    public string? StoreAddress { get; set; }
    public string? Phone { get; set; }
    public string PinHash { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}