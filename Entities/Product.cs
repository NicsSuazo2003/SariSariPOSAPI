using System.Net.ServerSentEvents;

namespace SariSariPOS.API.Entities;

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Barcode { get; set; }
    public string QrCode { get; set; } = default!;
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }
    public string Unit { get; set; } = "pc";
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int StockQty { get; set; }
    public int ReorderLevel { get; set; } = 5;
    public DateOnly? ExpiryDate { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsFavorite { get; set; }
    public int FavoriteOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}