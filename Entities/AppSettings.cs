namespace SariSariPOS.API.Entities;

public class AppSettings
{
    public Guid Id { get; set; }
    public string? ReceiptHeader { get; set; }
    public string? ReceiptFooter { get; set; }
    public int IdleLockMinutes { get; set; } = 5;
    public string? GcashNumber { get; set; }
    public string? MayaNumber { get; set; }
    public string Currency { get; set; } = "PHP";
    public DateTime UpdatedAt { get; set; }
}