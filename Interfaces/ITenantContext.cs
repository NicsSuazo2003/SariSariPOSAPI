namespace SariSariPOS.API.Interfaces;

/// <summary>
/// Ambient tenant identity for the current HTTP request.
/// Resolved from JWT claims ("store_id"). Returns Guid.Empty when the
/// request is unauthenticated (e.g., during login/setup), which is the
/// signal for global query filters to return no rows.
/// </summary>
public interface ITenantContext
{
    /// <summary>The current store's Id. Guid.Empty when unauthenticated.</summary>
    Guid StoreId { get; }

    /// <summary>The current user's Id. Guid.Empty when unauthenticated.</summary>
    Guid UserId { get; }

    /// <summary>True when a valid tenant has been resolved from the JWT.</summary>
    bool IsAuthenticated { get; }
}