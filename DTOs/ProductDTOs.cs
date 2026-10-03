namespace SariSariPOS.API.DTOs;

public record ProductDto(
    Guid Id,
    string Name,
    string? Barcode,
    string QrCode,
    Guid? CategoryId,
    string? CategoryName,
    string Unit,
    decimal CostPrice,
    decimal SellingPrice,
    int StockQty,
    int ReorderLevel,
    DateOnly? ExpiryDate,
    string? ImageUrl,
    bool IsFavorite,
    int FavoriteOrder,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateProductRequest(
    string Name,
    string? Barcode,
    Guid? CategoryId,
    string Unit,
    decimal CostPrice,
    decimal SellingPrice,
    int StockQty,
    int ReorderLevel,
    DateOnly? ExpiryDate,
    string? ImageUrl);

public record UpdateProductRequest(
    string Name,
    string? Barcode,
    Guid? CategoryId,
    string Unit,
    decimal CostPrice,
    decimal SellingPrice,
    int ReorderLevel,
    DateOnly? ExpiryDate,
    string? ImageUrl,
    bool IsActive);

public record ToggleFavoriteRequest(bool IsFavorite, int FavoriteOrder);

public record PagedResult<T>(IEnumerable<T> Items, int Total, int Page, int PageSize);