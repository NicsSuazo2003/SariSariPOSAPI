using System.Net.ServerSentEvents;

namespace SariSariPOS.API.Entities;

public enum PaymentMethod { Cash, GCash, Maya, Utang }
public enum SaleStatus { Completed, Voided }

public class Sale
{
    public Guid Id { get; set; }
    public string ReceiptNo { get; set; } = default!;
    public string ClientId { get; set; } = default!;
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Change { get; set; }
    public string? PaymentRef { get; set; }
    public SaleStatus Status { get; set; } = SaleStatus.Completed;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ServerCreatedAt { get; set; }
    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
}