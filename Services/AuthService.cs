using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<bool> NeedsSetupAsync()
        => !await _db.Users.AnyAsync();

    public async Task<AuthResponse> SetupAsync(SetupRequest req)
    {
        if (await _db.Users.AnyAsync())
            throw new InvalidOperationException("Setup already completed");

        if (string.IsNullOrWhiteSpace(req.StoreName))
            throw new ArgumentException("Store name required");

        if (string.IsNullOrWhiteSpace(req.OwnerName))
            throw new ArgumentException("Owner name required");

        if (string.IsNullOrWhiteSpace(req.Pin) || req.Pin.Length < 4 || req.Pin.Length > 6 || !req.Pin.All(char.IsDigit))
            throw new ArgumentException("PIN must be 4-6 digits");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = req.OwnerName.Trim(),
            StoreName = req.StoreName.Trim(),
            PinHash = BCrypt.Net.BCrypt.HashPassword(req.Pin),
            CreatedAt = DateTime.UtcNow,
        };

        _db.Users.Add(user);
        _db.AppSettings.Add(new AppSettings
        {
            Id = Guid.NewGuid(),
            UpdatedAt = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync();

        var token = GenerateToken(user);
        return new AuthResponse(token, ToDto(user));
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Pin))
            throw new UnauthorizedAccessException("PIN required");

        var user = await _db.Users.FirstOrDefaultAsync()
            ?? throw new UnauthorizedAccessException("No account exists");

        if (!BCrypt.Net.BCrypt.Verify(req.Pin, user.PinHash))
            throw new UnauthorizedAccessException("Invalid PIN");

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var token = GenerateToken(user);
        return new AuthResponse(token, ToDto(user));
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found");
        return ToDto(user);
    }

    private string GenerateToken(User user)
    {
        var secret = _config["JWT_SECRET"]
            ?? throw new InvalidOperationException("JWT_SECRET not configured");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, user.Name),
            new Claim("store", user.StoreName),
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
        new(u.Id, u.Name, u.StoreName, u.StoreAddress, u.Phone);
}