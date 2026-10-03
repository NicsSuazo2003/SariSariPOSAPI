namespace SariSariPOS.API.Entities;

public class SaleItem
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public Sale Sale { get; set; } = default!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;
    public string ProductName { get; set; } = default!;
    public int Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
}