namespace SariSariPOS.API.Entities;

public enum CreditTransactionType { Utang, Payment, Adjustment }

public class CreditTransaction
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;
    public CreditTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public Guid? SaleId { get; set; }
    public string? Method { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}