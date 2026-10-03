using SariSariPOS.API.DTOs;

namespace SariSariPOS.API.Interfaces;

public interface ICustomerService
{
    Task<List<CustomerDto>> ListAsync(string? search, bool? shortcuts);
    Task<CustomerDto> GetAsync(Guid id);
    Task<List<CreditTransactionDto>> GetTransactionsAsync(Guid id);
    Task<CustomerDto> CreateAsync(CreateCustomerRequest req);
    Task<CustomerDto> UpdateAsync(Guid id, UpdateCustomerRequest req);
    Task DeleteAsync(Guid id);
    Task<CustomerDto> ToggleShortcutAsync(Guid id, ToggleShortcutRequest req);
    Task<CustomerDto> RecordPaymentAsync(Guid id, RecordPaymentRequest req);
}