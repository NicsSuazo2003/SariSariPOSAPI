namespace SariSariPOS.API.DTOs;

public record TopProductDto(Guid ProductId, string Name, int Qty, decimal Revenue);
public record LowStockDto(Guid ProductId, string Name, int StockQty, int ReorderLevel);
public record DailySalesPoint(string Date, decimal Total, int Txns);

public record DashboardDto(
    decimal TodaySales,
    int TodayTxns,
    int TodayItems,
    decimal UtangOutstanding,
    int LowStockCount,
    List<TopProductDto> TopProducts,
    List<LowStockDto> LowStockItems,
    List<DailySalesPoint> Last7Days);

public record SalesReportDto(
    decimal TotalSales,
    int TotalTxns,
    decimal AverageSale,
    List<DailySalesPoint> Points);

public record UtangOutstandingDto(
    Guid CustomerId,
    string Name,
    string? Phone,
    decimal Balance,
    DateTime LastTransactionAt);