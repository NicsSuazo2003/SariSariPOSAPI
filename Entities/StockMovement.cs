namespace SariSariPOS.API.Entities;

public enum StockMovementType { In, Out, Adjust, Spoil, Return, Sale, Void }

public class StockMovement
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;
    public StockMovementType Type { get; set; }
    public int QtyChange { get; set; }
    public int QtyAfter { get; set; }
    public decimal? UnitCost { get; set; }
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public Guid? SaleId { get; set; }
    public string? Reason { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}