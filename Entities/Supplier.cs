namespace SariSariPOS.API.Entities;

public class Supplier
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Contact { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; }
}