using Microsoft.EntityFrameworkCore;
using SariSariPOS.API.Data;
using SariSariPOS.API.DTOs;
using SariSariPOS.API.Entities;
using SariSariPOS.API.Interfaces;


namespace SariSariPOS.API.Services;

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _db;
    public CustomerService(AppDbContext db) => _db = db;

    public async Task<List<CustomerDto>> ListAsync(string? search, bool? shortcuts)
    {
        var q = _db.Customers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(c => c.Name.ToLower().Contains(s)
                || (c.Phone != null && c.Phone.Contains(s)));
        }
        if (shortcuts == true) q = q.Where(c => c.IsShortcut);

        return await q.OrderByDescending(c => c.IsShortcut)
            .ThenBy(c => c.ShortcutOrder)
            .ThenBy(c => c.Name)
            .Select(c => ToDto(c))
            .ToListAsync();
    }

    public async Task<CustomerDto> GetAsync(Guid id)
    {
        var c = await _db.Customers.FindAsync(id)
            ?? throw new KeyNotFoundException("Customer not found");
        return ToDto(c);
    }

    public async Task<List<CreditTransactionDto>> GetTransactionsAsync(Guid id)
    {
        if (!await _db.Customers.AnyAsync(c => c.Id == id))
            throw new KeyNotFoundException("Customer not found");

        return await _db.CreditTransactions
            .Where(t => t.CustomerId == id)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new CreditTransactionDto(
                t.Id, t.Type.ToString(), t.Amount, t.BalanceAfter,
                t.SaleId, t.Method, t.Note, t.CreatedAt))
            .ToListAsync();
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) throw new ArgumentException("Name required");

        var c = new Customer
        {
            Id = Guid.NewGuid(),
            Name = req.Name.Trim(),
            Phone = req.Phone,
            Note = req.Note,
            QrCode = $"C-{ShortId.Generate(8)}",
            CreditBalance = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.Customers.Add(c);
        await _db.SaveChangesAsync();
        return ToDto(c);
    }

    public async Task<CustomerDto> UpdateAsync(Guid id, UpdateCustomerRequest req)
    {
        var c = await _db.Customers.FindAsync(id)
            ?? throw new KeyNotFoundException("Customer not found");
        c.Name = req.Name.Trim();
        c.Phone = req.Phone;
        c.Note = req.Note;
        c.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ToDto(c);
    }

    public async Task DeleteAsync(Guid id)
    {
        var c = await _db.Customers.FindAsync(id)
            ?? throw new KeyNotFoundException("Customer not found");
        if (c.CreditBalance > 0)
            throw new InvalidOperationException("Cannot delete customer with outstanding balance");
        _db.Customers.Remove(c);
        await _db.SaveChangesAsync();
    }

    public async Task<CustomerDto> ToggleShortcutAsync(Guid id, ToggleShortcutRequest req)
    {
        var c = await _db.Customers.FindAsync(id)
            ?? throw new KeyNotFoundException("Customer not found");
        c.IsShortcut = req.IsShortcut;
        c.ShortcutOrder = req.ShortcutOrder;
        c.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ToDto(c);
    }

    public async Task<CustomerDto> RecordPaymentAsync(Guid id, RecordPaymentRequest req)
    {
        if (req.Amount <= 0) throw new ArgumentException("Amount must be positive");

        var c = await _db.Customers.FindAsync(id)
            ?? throw new KeyNotFoundException("Customer not found");

        if (req.Amount > c.CreditBalance)
            throw new ArgumentException("Payment exceeds balance");

        c.CreditBalance -= req.Amount;
        c.UpdatedAt = DateTime.UtcNow;

        _db.CreditTransactions.Add(new CreditTransaction
        {
            Id = Guid.NewGuid(),
            CustomerId = c.Id,
            Type = CreditTransactionType.Payment,
            Amount = -req.Amount,
            BalanceAfter = c.CreditBalance,
            Method = req.Method ?? "cash",
            Note = req.Note,
            CreatedAt = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync();
        return ToDto(c);
    }

    private static CustomerDto ToDto(Customer c) => new(
        c.Id, c.Name, c.Phone, c.QrCode, c.CreditBalance,
        c.IsShortcut, c.ShortcutOrder, c.Note, c.CreatedAt);
}