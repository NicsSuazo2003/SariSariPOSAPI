namespace SariSariPOS.API.DTOs;

public record CreateStoreRequest(
    string StoreName,
    string OwnerName,
    string OwnerPin);

public record CreateStoreResponse(
    Guid StoreId,
    string StoreCode,
    string StoreName,
    Guid OwnerUserId,
    string OwnerName);

public record StoreSummaryDto(
    Guid Id,
    string Code,
    string Name,
    DateTime CreatedAt,
    int UserCount,
    int ProductCount,
    int SaleCount);
