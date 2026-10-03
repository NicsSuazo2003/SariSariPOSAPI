using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SariSariPOS.API.Interfaces;

namespace SariSariPOS.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportController : ControllerBase
{
    private readonly IReportService _svc;
    public ReportController(IReportService svc) => _svc = svc;

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard() => Ok(await _svc.DashboardAsync());

    [HttpGet("sales")]
    public async Task<IActionResult> Sales([FromQuery] string period = "daily")
        => Ok(await _svc.SalesAsync(period));

    [HttpGet("utang-outstanding")]
    public async Task<IActionResult> UtangOutstanding()
        => Ok(await _svc.UtangOutstandingAsync());
}