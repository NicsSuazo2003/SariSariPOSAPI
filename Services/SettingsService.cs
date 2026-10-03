using Microsoft.EntityFrameworkCore;
using SariSariPOS.API.Data;
using SariSariPOS.API.DTOs;
using SariSariPOS.API.Interfaces;

namespace SariSariPOS.API.Services;

public class SettingsService : ISettingsService
{
    private readonly AppDbContext _db;
    public SettingsService(AppDbContext db) => _db = db;

    public async Task<SettingsDto> GetAsync()
    {
        var s = await _db.AppSettings.FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Settings not found");
        return ToDto(s);
    }

    public async Task<SettingsDto> UpdateAsync(UpdateSettingsRequest req)
    {
        var s = await _db.AppSettings.FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Settings not found");

        if (req.ReceiptHeader is not null) s.ReceiptHeader = req.ReceiptHeader;
        if (req.ReceiptFooter is not null) s.ReceiptFooter = req.ReceiptFooter;
        if (req.IdleLockMinutes.HasValue) s.IdleLockMinutes = req.IdleLockMinutes.Value;
        if (req.GcashNumber is not null) s.GcashNumber = req.GcashNumber;
        if (req.MayaNumber is not null) s.MayaNumber = req.MayaNumber;
        if (req.Currency is not null) s.Currency = req.Currency;

        s.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ToDto(s);
    }

    private static SettingsDto ToDto(Entities.AppSettings s) =>
        new(s.ReceiptHeader, s.ReceiptFooter, s.IdleLockMinutes, s.GcashNumber, s.MayaNumber, s.Currency);
}