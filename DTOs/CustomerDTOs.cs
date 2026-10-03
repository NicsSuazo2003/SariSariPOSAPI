namespace SariSariPOS.API.DTOs;

public record CustomerDto(
    Guid Id,
    string Name,
    string? Phone,
    string QrCode,
    decimal CreditBalance,
    bool IsShortcut,
    int ShortcutOrder,
    string? Note,
    DateTime CreatedAt);

public record CreateCustomerRequest(string Name, string? Phone, string? Note);
public record UpdateCustomerRequest(string Name, string? Phone, string? Note);
public record ToggleShortcutRequest(bool IsShortcut, int ShortcutOrder);
public record RecordPaymentRequest(decimal Amount, string? Method, string? Note);
public record CreditTransactionDto(
    Guid Id,
    string Type,
    decimal Amount,
    decimal BalanceAfter,
    Guid? SaleId,
    string? Method,
    string? Note,
    DateTime CreatedAt);