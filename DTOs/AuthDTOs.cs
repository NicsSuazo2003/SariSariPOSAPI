namespace SariSariPOS.API.DTOs;

public record SetupRequest(string StoreName, string OwnerName, string Pin);
public record LoginRequest(string Pin);
public record AuthResponse(string Token, UserDto User);
public record UserDto(Guid Id, string Name, string StoreName, string? StoreAddress, string? Phone);