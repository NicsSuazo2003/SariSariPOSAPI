using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SariSariPOS.API.Data;
using SariSariPOS.API.DTOs;
using SariSariPOS.API.Entities;
using SariSariPOS.API.Interfaces;

namespace SariSariPOS.API.Services;

public class AdminService : IAdminService
{
    private readonly AppDbContext _db;
    private readonly ILogger<AdminService> _logger;

    public AdminService(AppDbContext db, ILogger<AdminService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<CreateStoreResponse> CreateStoreAsync(CreateStoreRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.StoreName))
            throw new ArgumentException("Store name is required");

        if (string.IsNullOrWhiteSpace(req.OwnerName))
            throw new ArgumentException("Owner name is required");

        if (string.IsNullOrWhiteSpace(req.OwnerPin)
            || req.OwnerPin.Length < 4
            || req.OwnerPin.Length > 6
            || !req.OwnerPin.All(char.IsDigit))
        {
            throw new ArgumentException("Owner PIN must be 4-6 digits");
        }

        var storeCode = await GenerateUniqueStoreCodeAsync();

        var store = new Store
        {
            Id = Guid.NewGuid(),
            Code = storeCode,
            Name = req.StoreName.Trim(),
            CreatedAt = DateTime.UtcNow,
        };

        var owner = new User
        {
            Id = Guid.NewGuid(),
            StoreId = store.Id,
            Name = req.OwnerName.Trim(),
            StoreName = store.Name,
            StoreCode = store.Code,
            PinHash = BCrypt.Net.BCrypt.HashPassword(req.OwnerPin),
            Role = UserRole.Owner,
            CreatedAt = DateTime.UtcNow,
        };

        store.OwnerUserId = owner.Id;

        var settings = new AppSettings
        {
            Id = Guid.NewGuid(),
            StoreId = store.Id,
            Currency = "PHP",
            IdleLockMinutes = 5,
            UpdatedAt = DateTime.UtcNow,
        };

        // Pre-auth-ish context: StoreId is set explicitly on every row,
        // so StampTenant() will no-op.
        _db.Stores.Add(store);
        _db.Users.Add(owner);
        _db.AppSettings.Add(settings);

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "PlatformAdmin created store {StoreName} ({StoreCode}) with owner {OwnerName}",
            store.Name, store.Code, owner.Name);

        return new CreateStoreResponse(
            store.Id,
            store.Code,
            store.Name,
            owner.Id,
            owner.Name);
    }

    public async Task<List<StoreSummaryDto>> ListStoresAsync()
    {
        // Store table has no global query filter — safe to read normally.
        // Users, Products, Sales DO have query filters (well, Users doesn't,
        // but Products and Sales do), so cross-tenant counts need
        // IgnoreQueryFilters().
        var stores = await _db.Stores
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new
            {
                s.Id,
                s.Code,
                s.Name,
                s.CreatedAt,
                UserCount = _db.Users.Count(u => u.StoreId == s.Id),
                ProductCount = _db.Products.IgnoreQueryFilters().Count(p => p.StoreId == s.Id),
                SaleCount = _db.Sales.IgnoreQueryFilters().Count(sa => sa.StoreId == s.Id),
            })
            .ToListAsync();

        return stores
            .Select(s => new StoreSummaryDto(
                s.Id, s.Code, s.Name, s.CreatedAt,
                s.UserCount, s.ProductCount, s.SaleCount))
            .ToList();
    }

    // ──────────────────────────────────────────────────────────
    // Matches the generator in AuthService
    // ──────────────────────────────────────────────────────────
    private async Task<string> GenerateUniqueStoreCodeAsync()
    {
        const string alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        const int suffixLen = 4;
        const int maxAttempts = 20;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var suffix = new char[suffixLen];
            for (int i = 0; i < suffixLen; i++)
                suffix[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];

            var candidate = $"SAR{new string(suffix)}";

            if (!await _db.Stores.AnyAsync(s => s.Code == candidate))
                return candidate;
        }

        throw new InvalidOperationException(
            "Unable to generate a unique store code. Please try again.");
    }
}