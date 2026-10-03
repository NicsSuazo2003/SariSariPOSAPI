using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SariSariPOS.API.DTOs;
using SariSariPOS.API.Interfaces;

namespace SariSariPOS.API.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductController : ControllerBase
{
    private readonly IProductService _svc;
    public ProductController(IProductService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] bool? favorites,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
        => Ok(await _svc.ListAsync(search, categoryId, favorites, page, pageSize));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id) => Ok(await _svc.GetAsync(id));

    [HttpGet("scan/{code}")]
    public async Task<IActionResult> Scan(string code) => Ok(await _svc.ScanAsync(code));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest req)
    {
        var result = await _svc.CreateAsync(req);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest req)
        => Ok(await _svc.UpdateAsync(id, req));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _svc.DeleteAsync(id);
        return NoContent();
    }

    [HttpPatch("{id:guid}/favorite")]
    public async Task<IActionResult> ToggleFavorite(Guid id, [FromBody] ToggleFavoriteRequest req)
        => Ok(await _svc.ToggleFavoriteAsync(id, req));
}