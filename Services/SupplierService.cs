using Microsoft.EntityFrameworkCore;
using SariSariPOS.API.Data;
using SariSariPOS.API.DTOs;
using SariSariPOS.API.Entities;
using SariSariPOS.API.Interfaces;

namespace SariSariPOS.API.Services;

public class SupplierService : ISupplierService
{
    private readonly AppDbContext _db;
    public SupplierService(AppDbContext db) => _db = db;

    public async Task<List<SupplierDto>> ListAsync()
        => await _db.Suppliers.OrderBy(s => s.Name)
            .Select(s => new SupplierDto(s.Id, s.Name, s.Contact, s.Address, s.CreatedAt))
            .ToListAsync();

    public async Task<SupplierDto> CreateAsync(CreateSupplierRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) throw new ArgumentException("Name required");
        var s = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = req.Name.Trim(),
            Contact = req.Contact,
            Address = req.Address,
            CreatedAt = DateTime.UtcNow,
        };
        _db.Suppliers.Add(s);
        await _db.SaveChangesAsync();
        return new SupplierDto(s.Id, s.Name, s.Contact, s.Address, s.CreatedAt);
    }

    public async Task<SupplierDto> UpdateAsync(Guid id, UpdateSupplierRequest req)
    {
        var s = await _db.Suppliers.FindAsync(id)
            ?? throw new KeyNotFoundException("Supplier not found");
        s.Name = req.Name.Trim();
        s.Contact = req.Contact;
        s.Address = req.Address;
        await _db.SaveChangesAsync();
        return new SupplierDto(s.Id, s.Name, s.Contact, s.Address, s.CreatedAt);
    }

    public async Task DeleteAsync(Guid id)
    {
        var s = await _db.Suppliers.FindAsync(id)
            ?? throw new KeyNotFoundException("Supplier not found");
        _db.Suppliers.Remove(s);
        await _db.SaveChangesAsync();
    }
}