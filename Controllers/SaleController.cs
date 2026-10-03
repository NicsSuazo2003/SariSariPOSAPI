using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SariSariPOS.API.DTOs;
using SariSariPOS.API.Interfaces;

namespace SariSariPOS.API.Controllers;

[ApiController]
[Route("api/sales")]
[Authorize]
public class SaleController : ControllerBase
{
    private readonly ISaleService _svc;
    public SaleController(ISaleService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
        => Ok(await _svc.ListAsync(from, to, page, pageSize));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id) => Ok(await _svc.GetAsync(id));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSaleRequest req)
        => Ok(await _svc.CreateAsync(req));

    [HttpPost("{id:guid}/void")]
    public async Task<IActionResult> Void(Guid id) => Ok(await _svc.VoidAsync(id));
}