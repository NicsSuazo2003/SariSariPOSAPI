using SariSariPOS.API.DTOs;

namespace SariSariPOS.API.Interfaces;

public interface IInventoryService
{
    Task<List<StockMovementDto>> ListMovementsAsync(Guid? productId, DateTime? from, DateTime? to);
    Task<List<StockMovementDto>> ReceiveAsync(ReceiveStockRequest req);
    Task<StockMovementDto> AdjustAsync(AdjustStockRequest req);
    Task<List<LowStockDto>> LowStockAsync();
}