using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SariSariPOS.API.DTOs;
using SariSariPOS.API.Interfaces;

namespace SariSariPOS.API.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _svc;
    public InventoryController(IInventoryService svc) => _svc = svc;

    [HttpGet("movements")]
    public async Task<IActionResult> Movements(
        [FromQuery] Guid? productId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
        => Ok(await _svc.ListMovementsAsync(productId, from, to));

    [HttpPost("receive")]
    public async Task<IActionResult> Receive([FromBody] ReceiveStockRequest req)
        => Ok(await _svc.ReceiveAsync(req));

    [HttpPost("adjust")]
    public async Task<IActionResult> Adjust([FromBody] AdjustStockRequest req)
        => Ok(await _svc.AdjustAsync(req));

    [HttpGet("low-stock")]
    public async Task<IActionResult> LowStock() => Ok(await _svc.LowStockAsync());
}