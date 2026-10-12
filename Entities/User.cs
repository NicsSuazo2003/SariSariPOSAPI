namespace SariSariPOS.API.Entities;

public enum UserRole
{
    Owner = 0,
    Cashier = 1
}

public class User
{
    public Guid Id { get; set; }

    /// <summary>Tenant FK. Not query-filtered — User is read pre-auth at login.</summary>
    public Guid StoreId { get; set; }

    public string Name { get; set; } = default!;

    /// <summary>Denormalized from Store.Name for receipt convenience.</summary>
    public string StoreName { get; set; } = default!;

    /// <summary>Denormalized from Store.Code. Indexed for fast login lookup.</summary>
    public string StoreCode { get; set; } = default!;

    public string? StoreAddress { get; set; }
    public string? Phone { get; set; }

    public string PinHash { get; set; } = default!;

    public UserRole Role { get; set; } = UserRole.Owner;

    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public Store Store { get; set; } = default!;
}