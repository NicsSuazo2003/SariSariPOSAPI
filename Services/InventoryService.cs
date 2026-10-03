using Microsoft.EntityFrameworkCore;
using SariSariPOS.API.Data;
using SariSariPOS.API.DTOs;
using SariSariPOS.API.Entities;
using SariSariPOS.API.Interfaces;

namespace SariSariPOS.API.Services;

public class InventoryService : IInventoryService
{
    private readonly AppDbContext _db;
    public InventoryService(AppDbContext db) => _db = db;

    public async Task<List<StockMovementDto>> ListMovementsAsync(Guid? productId, DateTime? from, DateTime? to)
    {
        var q = _db.StockMovements.Include(m => m.Product).AsQueryable();
        if (productId.HasValue) q = q.Where(m => m.ProductId == productId.Value);
        if (from.HasValue) q = q.Where(m => m.CreatedAt >= from.Value);
        if (to.HasValue) q = q.Where(m => m.CreatedAt <= to.Value);

        return await q.OrderByDescending(m => m.CreatedAt)
            .Take(500)
            .Select(m => new StockMovementDto(
                m.Id, m.ProductId, m.Product.Name, m.Type.ToString(),
                m.QtyChange, m.QtyAfter, m.UnitCost, m.SupplierId, m.SaleId,
                m.Reason, m.Note, m.CreatedAt))
            .ToListAsync();
    }

    public async Task<List<StockMovementDto>> ReceiveAsync(ReceiveStockRequest req)
    {
        if (req.Items == null || req.Items.Count == 0)
            throw new ArgumentException("At least one item required");

        await using var tx = await _db.Database.BeginTransactionAsync();
        var created = new List<StockMovement>();

        foreach (var item in req.Items)
        {
            if (item.Qty <= 0) throw new ArgumentException($"Qty must be positive for {item.ProductId}");

            var p = await _db.Products.FindAsync(item.ProductId)
                ?? throw new KeyNotFoundException($"Product not found: {item.ProductId}");

            p.StockQty += item.Qty;
            p.UpdatedAt = DateTime.UtcNow;
            if (item.Cost.HasValue && item.Cost.Value > 0)
                p.CostPrice = item.Cost.Value;

            var movement = new StockMovement
            {
                Id = Guid.NewGuid(),
                ProductId = p.Id,
                Type = StockMovementType.In,
                QtyChange = item.Qty,
                QtyAfter = p.StockQty,
                UnitCost = item.Cost,
                SupplierId = req.SupplierId,
                Note = req.Note,
                CreatedAt = DateTime.UtcNow,
            };
            _db.StockMovements.Add(movement);
            created.Add(movement);
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        var ids = created.Select(m => m.Id).ToHashSet();
        return await _db.StockMovements
            .Include(m => m.Product)
            .Where(m => ids.Contains(m.Id))
            .Select(m => new StockMovementDto(
                m.Id, m.ProductId, m.Product.Name, m.Type.ToString(),
                m.QtyChange, m.QtyAfter, m.UnitCost, m.SupplierId, m.SaleId,
                m.Reason, m.Note, m.CreatedAt))
            .ToListAsync();
    }

    public async Task<StockMovementDto> AdjustAsync(AdjustStockRequest req)
    {
        var p = await _db.Products.FindAsync(req.ProductId)
            ?? throw new KeyNotFoundException("Product not found");

        if (req.NewQty < 0) throw new ArgumentException("New qty cannot be negative");

        var delta = req.NewQty - p.StockQty;
        p.StockQty = req.NewQty;
        p.UpdatedAt = DateTime.UtcNow;

        var m = new StockMovement
        {
            Id = Guid.NewGuid(),
            ProductId = p.Id,
            Type = StockMovementType.Adjust,
            QtyChange = delta,
            QtyAfter = p.StockQty,
            Reason = req.Reason,
            Note = req.Note,
            CreatedAt = DateTime.UtcNow,
        };
        _db.StockMovements.Add(m);
        await _db.SaveChangesAsync();

        return new StockMovementDto(
            m.Id, m.ProductId, p.Name, m.Type.ToString(),
            m.QtyChange, m.QtyAfter, m.UnitCost, m.SupplierId, m.SaleId,
            m.Reason, m.Note, m.CreatedAt);
    }

    public async Task<List<LowStockDto>> LowStockAsync()
        => await _db.Products
            .Where(p => p.IsActive && p.StockQty <= p.ReorderLevel)
            .OrderBy(p => p.StockQty)
            .Select(p => new LowStockDto(p.Id, p.Name, p.StockQty, p.ReorderLevel))
            .ToListAsync();
}