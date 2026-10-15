
using Microsoft.EntityFrameworkCore;
using SariSariPOS.API.Data;
using SariSariPOS.API.DTOs;
using SariSariPOS.API.Entities;
using SariSariPOS.API.Interfaces;

namespace SariSariPOS.API.Services;

public class ReportService : IReportService
{
    private readonly AppDbContext _db;

    public ReportService(AppDbContext db) => _db = db;

    public async Task<DashboardDto> DashboardAsync()
    {
        var today = DateTime.UtcNow.Date;
        var thirtyDaysAgo = today.AddDays(-30);
        var sevenDaysAgo = today.AddDays(-6);

        // ── Today's sales (with line items for item count) ─────
        var todaySales = await _db.Sales
            .Include(s => s.Items)
            .Where(s => s.Status == SaleStatus.Completed
                     && s.ServerCreatedAt >= today)
            .ToListAsync();

        // ── Top products (last 30 days) ───────────────────────
        // Project aggregate results into an anonymous type first,
        // then construct DTOs after the database query completes.
        var topRaw = await _db.SaleItems
            .Where(si => si.Sale.Status == SaleStatus.Completed
                      && si.Sale.ServerCreatedAt >= thirtyDaysAgo)
            .GroupBy(si => new
            {
                si.ProductId,
                si.ProductName
            })
            .Select(g => new
            {
                ProductId = g.Key.ProductId,
                Name = g.Key.ProductName,
                Qty = g.Sum(x => x.Qty),
                Revenue = g.Sum(x => x.Subtotal)
            })
            .OrderByDescending(x => x.Revenue)
            .Take(10)
            .ToListAsync();

        var top = topRaw
            .Select(x => new TopProductDto(
                x.ProductId,
                x.Name,
                x.Qty,
                x.Revenue))
            .ToList();

        // ── Low stock items ───────────────────────────────────
        var low = await _db.Products
            .Where(p => p.IsActive && p.StockQty <= p.ReorderLevel)
            .OrderBy(p => p.StockQty)
            .Take(20)
            .Select(p => new LowStockDto(
                p.Id,
                p.Name,
                p.StockQty,
                p.ReorderLevel))
            .ToListAsync();

        // ── Outstanding utang ─────────────────────────────────
        var utang = await _db.Customers
            .Where(c => c.CreditBalance > 0)
            .SumAsync(c => (decimal?)c.CreditBalance) ?? 0;

        // ── Last 7 days sales chart ───────────────────────────
        // Load the filtered sales and group by calendar date in memory.
        var dailyRaw = await _db.Sales
            .Where(s => s.Status == SaleStatus.Completed
                     && s.ServerCreatedAt >= sevenDaysAgo)
            .Select(s => new
            {
                s.ServerCreatedAt,
                s.TotalAmount
            })
            .ToListAsync();

        var last7 = Enumerable.Range(0, 7)
            .Select(i => sevenDaysAgo.AddDays(i))
            .Select(d =>
            {
                var daySales = dailyRaw
                    .Where(x => x.ServerCreatedAt.Date == d)
                    .ToList();

                return new DailySalesPoint(
                    d.ToString("yyyy-MM-dd"),
                    daySales.Sum(x => x.TotalAmount),
                    daySales.Count);
            })
            .ToList();

        return new DashboardDto(
            todaySales.Sum(s => s.TotalAmount),
            todaySales.Count,
            todaySales.Sum(s => s.Items.Sum(i => i.Qty)),
            utang,
            low.Count,
            top,
            low,
            last7);
    }

    public async Task<SalesReportDto> SalesAsync(string period)
    {
        var now = DateTime.UtcNow;

        var (from, days, mode) = period?.ToLower() switch
        {
            "weekly" => (now.Date.AddDays(-27), 28, "day"),
            "monthly" => (now.Date.AddDays(-89), 90, "week"),
            _ => (now.Date.AddDays(-6), 7, "day")
        };

        var sales = await _db.Sales
            .Where(s => s.Status == SaleStatus.Completed
                     && s.ServerCreatedAt >= from)
            .ToListAsync();

        var points = new List<DailySalesPoint>();

        if (mode == "day")
        {
            for (int i = 0; i < days; i++)
            {
                var d = from.AddDays(i);

                var daySales = sales
                    .Where(s => s.ServerCreatedAt.Date == d)
                    .ToList();

                points.Add(new DailySalesPoint(
                    d.ToString("yyyy-MM-dd"),
                    daySales.Sum(s => s.TotalAmount),
                    daySales.Count));
            }
        }
        else
        {
            for (int i = 0; i < days; i += 7)
            {
                var wStart = from.AddDays(i);
                var wEnd = wStart.AddDays(7);

                var weekSales = sales
                    .Where(s => s.ServerCreatedAt >= wStart
                             && s.ServerCreatedAt < wEnd)
                    .ToList();

                points.Add(new DailySalesPoint(
                    wStart.ToString("yyyy-MM-dd"),
                    weekSales.Sum(s => s.TotalAmount),
                    weekSales.Count));
            }
        }

        var total = sales.Sum(s => s.TotalAmount);

        return new SalesReportDto(
            total,
            sales.Count,
            sales.Count == 0 ? 0 : total / sales.Count,
            points);
    }

    public async Task<List<UtangOutstandingDto>> UtangOutstandingAsync()
    {
        var customers = await _db.Customers
            .Where(c => c.CreditBalance > 0)
            .OrderByDescending(c => c.CreditBalance)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Phone,
                c.CreditBalance,
                LastTransactionAt = c.Transactions
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => (DateTime?)t.CreatedAt)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return customers
            .Select(c => new UtangOutstandingDto(
                c.Id,
                c.Name,
                c.Phone,
                c.CreditBalance,
                c.LastTransactionAt ?? DateTime.MinValue))
            .ToList();
    }
}
