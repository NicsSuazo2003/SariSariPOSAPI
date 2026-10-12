using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SariSariPOS.API.Data;
using SariSariPOS.API.DTOs;
using SariSariPOS.API.Entities;
using SariSariPOS.API.Interfaces;

namespace SariSariPOS.API.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext db, IConfiguration config, ILogger<AuthService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────
    // Setup — provisions the first tenant (store + owner user)
    // ─────────────────────────────────────────────────────────────
    public async Task<bool> NeedsSetupAsync()
        => !await _db.Stores.AnyAsync();

    public async Task<AuthResponse> SetupAsync(SetupRequest req)
    {
        if (await _db.Stores.AnyAsync())
            throw new InvalidOperationException("Setup already completed");

        if (string.IsNullOrWhiteSpace(req.StoreName))
            throw new ArgumentException("Store name required");

        if (string.IsNullOrWhiteSpace(req.OwnerName))
            throw new ArgumentException("Owner name required");

        if (string.IsNullOrWhiteSpace(req.Pin) || req.Pin.Length < 4 || req.Pin.Length > 6 || !req.Pin.All(char.IsDigit))
            throw new ArgumentException("PIN must be 4-6 digits");

        var storeCode = await GenerateUniqueStoreCodeAsync();

        var store = new Store
        {
            Id = Guid.NewGuid(),
            Code = storeCode,
            Name = req.StoreName.Trim(),
            CreatedAt = DateTime.UtcNow,
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            StoreId = store.Id,
            Name = req.OwnerName.Trim(),
            StoreName = store.Name,
            StoreCode = store.Code,
            PinHash = BCrypt.Net.BCrypt.HashPassword(req.Pin),
            Role = UserRole.Owner,
            CreatedAt = DateTime.UtcNow,
        };

        store.OwnerUserId = user.Id;

        _db.Stores.Add(store);
        _db.Users.Add(user);
        _db.AppSettings.Add(new AppSettings
        {
            Id = Guid.NewGuid(),
            StoreId = store.Id,
            Currency = "PHP",
            IdleLockMinutes = 5,
            UpdatedAt = DateTime.UtcNow,
        });

        // Pre-auth insert: the tenant context is empty, so StoreId is set
        // explicitly above for every row. StampTenant() will no-op.
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Store provisioned: {StoreName} (Code: {StoreCode})",
            store.Name, store.Code);

        var token = GenerateToken(user);
        return new AuthResponse(token, ToDto(user));
    }

    // ─────────────────────────────────────────────────────────────
    // Login — scoped to a store via StoreCode
    // ─────────────────────────────────────────────────────────────
    public async Task<AuthResponse> LoginAsync(LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.StoreCode))
            throw new UnauthorizedAccessException("Store code required");

        if (string.IsNullOrWhiteSpace(req.Pin))
            throw new UnauthorizedAccessException("PIN required");

        var code = NormalizeStoreCode(req.StoreCode);

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.StoreCode == code)
            ?? throw new UnauthorizedAccessException("Invalid store code or PIN");

        if (!BCrypt.Net.BCrypt.Verify(req.Pin, user.PinHash))
            throw new UnauthorizedAccessException("Invalid store code or PIN");

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "User {UserId} ({Name}) logged in to store {StoreCode}",
            user.Id, user.Name, code);

        var token = GenerateToken(user);
        return new AuthResponse(token, ToDto(user));
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found");
        return ToDto(user);
    }

    // ─────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a short, URL-safe, human-readable store code.
    /// Format: 3-letter prefix + 4 random alphanumerics, e.g. "SAR7K2Q".
    /// Excludes ambiguous chars (0/O, 1/I/L) for cashier readability.
    /// </summary>
    private async Task<string> GenerateUniqueStoreCodeAsync()
    {
        const string alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789"; // no 0/O/1/I/L
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

    public static string NormalizeStoreCode(string raw)
        => raw.Trim().ToUpperInvariant().Replace(" ", "").Replace("-", "");

    private string GenerateToken(User user)
    {
        var secret = _config["JWT_SECRET"]
            ?? throw new InvalidOperationException("JWT_SECRET not configured");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, user.Name),
            new Claim("store_id", user.StoreId.ToString()),
            new Claim("store_code", user.StoreCode),
            new Claim("store", user.StoreName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: "sarisari-pos",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UserDto ToDto(User u) =>
        new(u.Id, u.Name, u.StoreName, u.StoreCode, u.StoreAddress, u.Phone, u.Role.ToString());
}