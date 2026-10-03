using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SariSariPOS.API.Data;
using SariSariPOS.API.DTOs;
using SariSariPOS.API.Entities;
using SariSariPOS.API.Interfaces;

namespace SariSariPOS.API.Services;

public class SaleService : ISaleService
{
    private readonly AppDbContext _db;
    public SaleService(AppDbContext db) => _db = db;

    public async Task<PagedResult<SaleDto>> ListAsync(DateTime? from, DateTime? to, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 50;

        var q = _db.Sales.Include(s => s.Items).Include(s => s.Customer).AsQueryable();
        if (from.HasValue) q = q.Where(s => s.ServerCreatedAt >= from.Value);
        if (to.HasValue) q = q.Where(s => s.ServerCreatedAt <= to.Value);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(s => s.ServerCreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        return new PagedResult<SaleDto>(items.Select(ToDto), total, page, pageSize);
    }

    public async Task<SaleDto> GetAsync(Guid id)
    {
        var s = await _db.Sales.Include(x => x.Items).Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("Sale not found");
        return ToDto(s);
    }

    public async Task<SaleDto> CreateAsync(CreateSaleRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.ClientId))
            throw new ArgumentException("ClientId required");

        if (req.Items == null || req.Items.Count == 0)
            throw new ArgumentException("Cart is empty");

        var existing = await _db.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.ClientId == req.ClientId);
        if (existing != null)
            return JsonSerializer.Deserialize<SaleDto>(existing.ResponseJson)!;

        if (!Enum.TryParse<PaymentMethod>(req.PaymentMethod, true, out var method))
            throw new ArgumentException("Invalid payment method");

        await using var tx = await _db.Database.BeginTransactionAsync();

        var productIds = req.Items.Select(i => i.ProductId).ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id) && p.IsActive)
            .ToDictionaryAsync(p => p.Id);

        if (products.Count != productIds.Distinct().Count())
            throw new KeyNotFoundException("One or more products not found");

        foreach (var item in req.Items)
        {
            if (item.Qty <= 0) throw new ArgumentException("Qty must be positive");
            var p = products[item.ProductId];
            if (p.StockQty < item.Qty)
                throw new InvalidOperationException($"Insufficient stock for {p.Name}");
        }

        var subtotal = req.Items.Sum(i => i.Qty * i.UnitPrice);
        var total = subtotal - req.Discount;

        if (method == PaymentMethod.Utang && !req.CustomerId.HasValue)
            throw new ArgumentException("Customer required for utang sale");

        if (method == PaymentMethod.Cash && req.AmountPaid < total)
            throw new ArgumentException("Amount paid less than total");

        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            ReceiptNo = await GenerateReceiptNoAsync(),
            ClientId = req.ClientId,
            CustomerId = req.CustomerId,
            Subtotal = subtotal,
            Discount = req.Discount,
            TotalAmount = total,
            PaymentMethod = method,
            AmountPaid = req.AmountPaid,
            Change = method == PaymentMethod.Cash ? req.AmountPaid - total : 0,
            PaymentRef = req.PaymentRef,
            Status = SaleStatus.Completed,
            Note = req.Note,
            CreatedAt = req.CreatedAt == default ? DateTime.UtcNow : req.CreatedAt,
            ServerCreatedAt = DateTime.UtcNow,
        };

        foreach (var item in req.Items)
        {
            var p = products[item.ProductId];
            sale.Items.Add(new SaleItem
            {
                Id = Guid.NewGuid(),
                ProductId = p.Id,
                ProductName = p.Name,
                Qty = item.Qty,
                UnitPrice = item.UnitPrice,
                Subtotal = item.Qty * item.UnitPrice,
            });

            p.StockQty -= item.Qty;
            p.UpdatedAt = DateTime.UtcNow;

            _db.StockMovements.Add(new StockMovement
            {
                Id = Guid.NewGuid(),
                ProductId = p.Id,
                Type = StockMovementType.Sale,
                QtyChange = -item.Qty,
                QtyAfter = p.StockQty,
                SaleId = sale.Id,
                CreatedAt = DateTime.UtcNow,
            });
        }

        if (method == PaymentMethod.Utang)
        {
            var customer = await _db.Customers.FindAsync(req.CustomerId!.Value)
                ?? throw new KeyNotFoundException("Customer not found");

            customer.CreditBalance += total;
            customer.UpdatedAt = DateTime.UtcNow;

            _db.CreditTransactions.Add(new CreditTransaction
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,
                Type = CreditTransactionType.Utang,
                Amount = total,
                BalanceAfter = customer.CreditBalance,
                SaleId = sale.Id,
                CreatedAt = DateTime.UtcNow,
            });
        }

        _db.Sales.Add(sale);
        await _db.SaveChangesAsync();

        var dto = ToDto(sale);
        _db.IdempotencyRecords.Add(new IdempotencyRecord
        {
            Id = Guid.NewGuid(),
            ClientId = req.ClientId,
            ResponseJson = JsonSerializer.Serialize(dto),
            CreatedAt = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();

        await tx.CommitAsync();
        return dto;
    }

    public async Task<SaleDto> VoidAsync(Guid id)
    {
        var sale = await _db.Sales.Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new KeyNotFoundException("Sale not found");

        if (sale.Status == SaleStatus.Voided)
            throw new InvalidOperationException("Sale already voided");

        await using var tx = await _db.Database.BeginTransactionAsync();

        sale.Status = SaleStatus.Voided;

        foreach (var item in sale.Items)
        {
            var p = await _db.Products.FindAsync(item.ProductId);
            if (p == null) continue;

            p.StockQty += item.Qty;
            p.UpdatedAt = DateTime.UtcNow;

            _db.StockMovements.Add(new StockMovement
            {
                Id = Guid.NewGuid(),
                ProductId = p.Id,
                Type = StockMovementType.Void,
                QtyChange = item.Qty,
                QtyAfter = p.StockQty,
                SaleId = sale.Id,
                Reason = "Sale voided",
                CreatedAt = DateTime.UtcNow,
            });
        }

        if (sale.PaymentMethod == PaymentMethod.Utang && sale.CustomerId.HasValue)
        {
            var c = await _db.Customers.FindAsync(sale.CustomerId.Value);
            if (c != null)
            {
                c.CreditBalance -= sale.TotalAmount;
                c.UpdatedAt = DateTime.UtcNow;

                _db.CreditTransactions.Add(new CreditTransaction
                {
                    Id = Guid.NewGuid(),
                    CustomerId = c.Id,
                    Type = CreditTransactionType.Adjustment,
                    Amount = -sale.TotalAmount,
                    BalanceAfter = c.CreditBalance,
                    SaleId = sale.Id,
                    Note = "Sale voided",
                    CreatedAt = DateTime.UtcNow,
                });
            }
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return await GetAsync(id);
    }

    private async Task<string> GenerateReceiptNoAsync()
    {
        var today = DateTime.UtcNow.Date;
        var prefix = today.ToString("yyyyMMdd");
        var count = await _db.Sales
            .Where(s => s.ServerCreatedAt >= today)
            .CountAsync();
        return $"{prefix}-{(count + 1):D4}";
    }

    private static SaleDto ToDto(Sale s) => new(
        s.Id, s.ReceiptNo, s.ClientId, s.CustomerId, s.Customer?.Name,
        s.Subtotal, s.Discount, s.TotalAmount, s.PaymentMethod.ToString(),
        s.AmountPaid, s.Change, s.PaymentRef, s.Status.ToString(), s.Note,
        s.CreatedAt, s.ServerCreatedAt,
        s.Items.Select(i => new SaleItemDto(
            i.Id, i.ProductId, i.ProductName, i.Qty, i.UnitPrice, i.Subtotal)).ToList());
}