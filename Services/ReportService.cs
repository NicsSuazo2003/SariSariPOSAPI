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

        var todaySales = await _db.Sales
            .Include(s => s.Items)
            .Where(s => s.Status == SaleStatus.Completed && s.ServerCreatedAt >= today)
            .ToListAsync();

        var top = await _db.SaleItems
            .Where(si => si.Sale.Status == SaleStatus.Completed
                      && si.Sale.ServerCreatedAt >= today.AddDays(-30))
            .GroupBy(si => si.ProductId)
            .Select(g => new TopProductDto(
                g.Key,
                g.First().ProductName,
                g.Sum(x => x.Qty),
                g.Sum(x => x.Subtotal)))
            .OrderByDescending(x => x.Revenue)
            .Take(10)
            .ToListAsync();

        var low = await _db.Products
            .Where(p => p.IsActive && p.StockQty <= p.ReorderLevel)
            .OrderBy(p => p.StockQty)
            .Take(20)
            .Select(p => new LowStockDto(p.Id, p.Name, p.StockQty, p.ReorderLevel))
            .ToListAsync();

        var utang = await _db.Customers
            .Where(c => c.CreditBalance > 0)
            .SumAsync(c => (decimal?)c.CreditBalance) ?? 0;

        var from = today.AddDays(-6);
        var dailyRaw = await _db.Sales
            .Where(s => s.Status == SaleStatus.Completed && s.ServerCreatedAt >= from)
            .GroupBy(s => s.ServerCreatedAt.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(s => s.TotalAmount), Txns = g.Count() })
            .ToListAsync();

        var last7 = Enumerable.Range(0, 7)
            .Select(i => from.AddDays(i))
            .Select(d =>
            {
                var match = dailyRaw.FirstOrDefault(x => x.Date == d);
                return new DailySalesPoint(
                    d.ToString("yyyy-MM-dd"),
                    match?.Total ?? 0,
                    match?.Txns ?? 0);
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
            _ => (now.Date.AddDays(-6), 7, "day"),
        };

        var sales = await _db.Sales
            .Where(s => s.Status == SaleStatus.Completed && s.ServerCreatedAt >= from)
            .ToListAsync();

        var points = new List<DailySalesPoint>();

        if (mode == "day")
        {
            for (int i = 0; i < days; i++)
            {
                var d = from.AddDays(i);
                var daySales = sales.Where(s => s.ServerCreatedAt.Date == d).ToList();
                points.Add(new DailySalesPoint(d.ToString("yyyy-MM-dd"),
                    daySales.Sum(s => s.TotalAmount), daySales.Count));
            }
        }
        else
        {
            for (int i = 0; i < days; i += 7)
            {
                var wStart = from.AddDays(i);
                var wEnd = wStart.AddDays(7);
                var weekSales = sales.Where(s => s.ServerCreatedAt >= wStart && s.ServerCreatedAt < wEnd).ToList();
                points.Add(new DailySalesPoint(wStart.ToString("yyyy-MM-dd"),
                    weekSales.Sum(s => s.TotalAmount), weekSales.Count));
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
        => await _db.Customers
            .Where(c => c.CreditBalance > 0)
            .OrderByDescending(c => c.CreditBalance)
            .Select(c => new UtangOutstandingDto(
                c.Id, c.Name, c.Phone, c.CreditBalance,
                c.Transactions.OrderByDescending(t => t.CreatedAt)
                    .Select(t => t.CreatedAt).FirstOrDefault()))
            .ToListAsync();
}