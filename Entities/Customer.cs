namespace SariSariPOS.API.Entities;

public class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Phone { get; set; }
    public string QrCode { get; set; } = default!;
    public decimal CreditBalance { get; set; }
    public bool IsShortcut { get; set; }
    public int ShortcutOrder { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<CreditTransaction> Transactions { get; set; } = new List<CreditTransaction>();
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}