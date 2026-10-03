using SariSariPOS.API.DTOs;

namespace SariSariPOS.API.Interfaces;

public interface ISettingsService
{
    Task<SettingsDto> GetAsync();
    Task<SettingsDto> UpdateAsync(UpdateSettingsRequest req);
}