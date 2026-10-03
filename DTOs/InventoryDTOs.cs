namespace SariSariPOS.API.DTOs;

public record StockMovementDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string Type,
    int QtyChange,
    int QtyAfter,
    decimal? UnitCost,
    Guid? SupplierId,
    Guid? SaleId,
    string? Reason,
    string? Note,
    DateTime CreatedAt);

public record ReceiveStockItem(Guid ProductId, int Qty, decimal? Cost);
public record ReceiveStockRequest(Guid? SupplierId, List<ReceiveStockItem> Items, string? Note);
public record AdjustStockRequest(Guid ProductId, int NewQty, string Reason, string? Note);