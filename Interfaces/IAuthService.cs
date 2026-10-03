using SariSariPOS.API.DTOs;

namespace SariSariPOS.API.Interfaces;

public interface IAuthService
{
    Task<bool> NeedsSetupAsync();
    Task<AuthResponse> SetupAsync(SetupRequest req);
    Task<AuthResponse> LoginAsync(LoginRequest req);
    Task<UserDto> GetCurrentUserAsync(Guid userId);
}