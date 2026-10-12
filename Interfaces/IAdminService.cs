using SariSariPOS.API.DTOs;

namespace SariSariPOS.API.Interfaces;

public interface IAdminService
{
    /// <summary>
    /// Creates a new tenant (store + owner user + default settings).
    /// Returns the generated store code for hand-off to the new owner.
    /// </summary>
    Task<CreateStoreResponse> CreateStoreAsync(CreateStoreRequest req);

    /// <summary>
    /// Lists all tenants with light usage stats for the admin dashboard.
    /// </summary>
    Task<List<StoreSummaryDto>> ListStoresAsync();
}