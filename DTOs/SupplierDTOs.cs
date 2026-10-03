namespace SariSariPOS.API.DTOs;

public record SupplierDto(Guid Id, string Name, string? Contact, string? Address, DateTime CreatedAt);
public record CreateSupplierRequest(string Name, string? Contact, string? Address);
public record UpdateSupplierRequest(string Name, string? Contact, string? Address);