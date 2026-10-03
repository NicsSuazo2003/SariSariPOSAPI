using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SariSariPOS.API.DTOs;
using SariSariPOS.API.Interfaces;

namespace SariSariPOS.API.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _svc;
    public CategoryController(ICategoryService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> List() => Ok(await _svc.ListAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest req)
        => Ok(await _svc.CreateAsync(req));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryRequest req)
        => Ok(await _svc.UpdateAsync(id, req));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _svc.DeleteAsync(id);
        return NoContent();
    }
}