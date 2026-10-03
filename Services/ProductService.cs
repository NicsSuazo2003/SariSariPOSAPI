using Microsoft.EntityFrameworkCore;
using SariSariPOS.API.Data;
using SariSariPOS.API.DTOs;
using SariSariPOS.API.Entities;
using SariSariPOS.API.Interfaces;

namespace SariSariPOS.API.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _db;
    public ProductService(AppDbContext db) => _db = db;

    public async Task<PagedResult<ProductDto>> ListAsync(string? search, Guid? categoryId, bool? favorites, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 50;

        var q = _db.Products.Include(p => p.Category).Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(p =>
                p.Name.ToLower().Contains(s) ||
                (p.Barcode != null && p.Barcode.Contains(s)));
        }

        if (categoryId.HasValue) q = q.Where(p => p.CategoryId == categoryId.Value);
        if (favorites == true) q = q.Where(p => p.IsFavorite);

        var total = await q.CountAsync();

        var items = await q
            .OrderByDescending(p => p.IsFavorite)
            .ThenBy(p => p.FavoriteOrder)
            .ThenBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => ToDto(p))
            .ToListAsync();

        return new PagedResult<ProductDto>(items, total, page, pageSize);
    }

    public async Task<ProductDto> GetAsync(Guid id)
    {
        var p = await _db.Products.Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive)
            ?? throw new KeyNotFoundException("Product not found");
        return ToDto(p);
    }

    public async Task<ProductDto> ScanAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code required");

        var p = await _db.Products.Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.IsActive && (x.Barcode == code || x.QrCode == code))
            ?? throw new KeyNotFoundException("Product not found");

        return ToDto(p);
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new ArgumentException("Name required");

        if (!string.IsNullOrWhiteSpace(req.Barcode))
        {
            var exists = await _db.Products.AnyAsync(p => p.Barcode == req.Barcode);
            if (exists) throw new InvalidOperationException("Barcode already in use");
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = req.Name.Trim(),
            Barcode = string.IsNullOrWhiteSpace(req.Barcode) ? null : req.Barcode.Trim(),
            QrCode = $"P-{ShortId.Generate(8)}",
            CategoryId = req.CategoryId,
            Unit = string.IsNullOrWhiteSpace(req.Unit) ? "pc" : req.Unit,
            CostPrice = req.CostPrice,
            SellingPrice = req.SellingPrice,
            StockQty = req.StockQty,
            ReorderLevel = req.ReorderLevel,
            ExpiryDate = req.ExpiryDate,
            ImageUrl = req.ImageUrl,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.Products.Add(product);

        if (product.StockQty > 0)
        {
            _db.StockMovements.Add(new StockMovement
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Type = StockMovementType.In,
                QtyChange = product.StockQty,
                QtyAfter = product.StockQty,
                UnitCost = product.CostPrice,
                Reason = "Initial stock",
                CreatedAt = DateTime.UtcNow,
            });
        }

        await _db.SaveChangesAsync();
        return await GetAsync(product.Id);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest req)
    {
        var p = await _db.Products.FindAsync(id)
            ?? throw new KeyNotFoundException("Product not found");

        if (!string.IsNullOrWhiteSpace(req.Barcode) && req.Barcode != p.Barcode)
        {
            var exists = await _db.Products.AnyAsync(x => x.Barcode == req.Barcode && x.Id != id);
            if (exists) throw new InvalidOperationException("Barcode already in use");
        }

        p.Name = req.Name.Trim();
        p.Barcode = string.IsNullOrWhiteSpace(req.Barcode) ? null : req.Barcode.Trim();
        p.CategoryId = req.CategoryId;
        p.Unit = req.Unit;
        p.CostPrice = req.CostPrice;
        p.SellingPrice = req.SellingPrice;
        p.ReorderLevel = req.ReorderLevel;
        p.ExpiryDate = req.ExpiryDate;
        p.ImageUrl = req.ImageUrl;
        p.IsActive = req.IsActive;
        p.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return await GetAsync(id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var p = await _db.Products.FindAsync(id)
            ?? throw new KeyNotFoundException("Product not found");
        p.IsActive = false;
        p.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<ProductDto> ToggleFavoriteAsync(Guid id, ToggleFavoriteRequest req)
    {
        var p = await _db.Products.FindAsync(id)
            ?? throw new KeyNotFoundException("Product not found");
        p.IsFavorite = req.IsFavorite;
        p.FavoriteOrder = req.FavoriteOrder;
        p.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return await GetAsync(id);
    }

    internal static ProductDto ToDto(Product p) => new(
        p.Id, p.Name, p.Barcode, p.QrCode,
        p.CategoryId, p.Category?.Name,
        p.Unit, p.CostPrice, p.SellingPrice,
        p.StockQty, p.ReorderLevel, p.ExpiryDate, p.ImageUrl,
        p.IsFavorite, p.FavoriteOrder, p.IsActive,
        p.CreatedAt, p.UpdatedAt);
}