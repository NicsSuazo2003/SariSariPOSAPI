namespace SariSariPOS.API.DTOs;

public record CreateSaleItemRequest(Guid ProductId, int Qty, decimal UnitPrice);
public record CreateSaleRequest(
    string ClientId,
    List<CreateSaleItemRequest> Items,
    Guid? CustomerId,
    string PaymentMethod,
    decimal AmountPaid,
    decimal Discount,
    string? PaymentRef,
    string? Note,
    DateTime CreatedAt);

public record SaleItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Qty,
    decimal UnitPrice,
    decimal Subtotal);

public record SaleDto(
    Guid Id,
    string ReceiptNo,
    string ClientId,
    Guid? CustomerId,
    string? CustomerName,
    decimal Subtotal,
    decimal Discount,
    decimal TotalAmount,
    string PaymentMethod,
    decimal AmountPaid,
    decimal Change,
    string? PaymentRef,
    string Status,
    string? Note,
    DateTime CreatedAt,
    DateTime ServerCreatedAt,
    List<SaleItemDto> Items);