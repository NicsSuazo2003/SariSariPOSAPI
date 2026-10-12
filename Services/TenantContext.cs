using System.Security.Claims;
using SariSariPOS.API.Interfaces;

namespace SariSariPOS.API.Services;

public sealed class TenantContext : ITenantContext
{
    public Guid StoreId { get; }
    public Guid UserId { get; }
    public bool IsAuthenticated { get; }

    public TenantContext(IHttpContextAccessor accessor)
    {
        var user = accessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            StoreId = Guid.Empty;
            UserId = Guid.Empty;
            IsAuthenticated = false;
            return;
        }

        var storeClaim = user.FindFirstValue("store_id");
        var subClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? user.FindFirstValue("sub");

        StoreId = Guid.TryParse(storeClaim, out var sid) ? sid : Guid.Empty;
        UserId = Guid.TryParse(subClaim, out var uid) ? uid : Guid.Empty;
        IsAuthenticated = StoreId != Guid.Empty;
    }
}