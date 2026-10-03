using Microsoft.EntityFrameworkCore;
using SariSariPOS.API.Data;
using SariSariPOS.API.DTOs;
using SariSariPOS.API.Entities;
using SariSariPOS.API.Interfaces;

namespace SariSariPOS.API.Services;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _db;
    public CategoryService(AppDbContext db) => _db = db;

    public async Task<List<CategoryDto>> ListAsync()
        => await _db.Categories
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Description, c.SortOrder))
            .ToListAsync();

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) throw new ArgumentException("Name required");
        if (await _db.Categories.AnyAsync(c => c.Name == req.Name))
            throw new InvalidOperationException("Category name already exists");

        var c = new Category
        {
            Id = Guid.NewGuid(),
            Name = req.Name.Trim(),
            Description = req.Description,
            SortOrder = req.SortOrder,
            CreatedAt = DateTime.UtcNow,
        };
        _db.Categories.Add(c);
        await _db.SaveChangesAsync();
        return new CategoryDto(c.Id, c.Name, c.Description, c.SortOrder);
    }

    public async Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest req)
    {
        var c = await _db.Categories.FindAsync(id)
            ?? throw new KeyNotFoundException("Category not found");
        c.Name = req.Name.Trim();
        c.Description = req.Description;
        c.SortOrder = req.SortOrder;
        await _db.SaveChangesAsync();
        return new CategoryDto(c.Id, c.Name, c.Description, c.SortOrder);
    }

    public async Task DeleteAsync(Guid id)
    {
        var c = await _db.Categories.FindAsync(id)
            ?? throw new KeyNotFoundException("Category not found");
        _db.Categories.Remove(c);
        await _db.SaveChangesAsync();
    }
}