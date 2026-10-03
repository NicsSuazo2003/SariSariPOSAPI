using SariSariPOS.API.DTOs;

namespace SariSariPOS.API.Interfaces;

public interface IReportService
{
    Task<DashboardDto> DashboardAsync();
    Task<SalesReportDto> SalesAsync(string period);
    Task<List<UtangOutstandingDto>> UtangOutstandingAsync();
}