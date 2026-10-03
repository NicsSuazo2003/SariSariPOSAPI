using SariSariPOS.API.DTOs;

namespace SariSariPOS.API.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> ListAsync();
    Task<CategoryDto> CreateAsync(CreateCategoryRequest req);
    Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest req);
    Task DeleteAsync(Guid id);
}