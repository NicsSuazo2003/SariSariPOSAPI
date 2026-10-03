using SariSariPOS.API.DTOs;

namespace SariSariPOS.API.Interfaces;

public interface ISaleService
{
    Task<PagedResult<SaleDto>> ListAsync(DateTime? from, DateTime? to, int page, int pageSize);
    Task<SaleDto> GetAsync(Guid id);
    Task<SaleDto> CreateAsync(CreateSaleRequest req);
    Task<SaleDto> VoidAsync(Guid id);
}