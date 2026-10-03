using SariSariPOS.API.DTOs;

namespace SariSariPOS.API.Interfaces;

public interface IProductService
{
    Task<PagedResult<ProductDto>> ListAsync(string? search, Guid? categoryId, bool? favorites, int page, int pageSize);
    Task<ProductDto> GetAsync(Guid id);
    Task<ProductDto> ScanAsync(string code);
    Task<ProductDto> CreateAsync(CreateProductRequest req);
    Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest req);
    Task DeleteAsync(Guid id);
    Task<ProductDto> ToggleFavoriteAsync(Guid id, ToggleFavoriteRequest req);
}