namespace SariSariPOS.API.DTOs;

public record SettingsDto(
    string? ReceiptHeader,
    string? ReceiptFooter,
    int IdleLockMinutes,
    string? GcashNumber,
    string? MayaNumber,
    string Currency);

public record UpdateSettingsRequest(
    string? ReceiptHeader,
    string? ReceiptFooter,
    int? IdleLockMinutes,
    string? GcashNumber,
    string? MayaNumber,
    string? Currency);