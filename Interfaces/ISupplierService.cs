using SariSariPOS.API.DTOs;

namespace SariSariPOS.API.Interfaces;

public interface ISupplierService
{
    Task<List<SupplierDto>> ListAsync();
    Task<SupplierDto> CreateAsync(CreateSupplierRequest req);
    Task<SupplierDto> UpdateAsync(Guid id, UpdateSupplierRequest req);
    Task DeleteAsync(Guid id);
}