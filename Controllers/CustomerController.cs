using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SariSariPOS.API.DTOs;
using SariSariPOS.API.Interfaces;

namespace SariSariPOS.API.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomerController : ControllerBase
{
    private readonly ICustomerService _svc;
    public CustomerController(ICustomerService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] bool? shortcuts)
        => Ok(await _svc.ListAsync(search, shortcuts));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id) => Ok(await _svc.GetAsync(id));

    [HttpGet("{id:guid}/transactions")]
    public async Task<IActionResult> Transactions(Guid id)
        => Ok(await _svc.GetTransactionsAsync(id));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest req)
        => Ok(await _svc.CreateAsync(req));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerRequest req)
        => Ok(await _svc.UpdateAsync(id, req));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _svc.DeleteAsync(id);
        return NoContent();
    }

    [HttpPatch("{id:guid}/shortcut")]
    public async Task<IActionResult> ToggleShortcut(Guid id, [FromBody] ToggleShortcutRequest req)
        => Ok(await _svc.ToggleShortcutAsync(id, req));

    [HttpPost("{id:guid}/payments")]
    public async Task<IActionResult> RecordPayment(Guid id, [FromBody] RecordPaymentRequest req)
        => Ok(await _svc.RecordPaymentAsync(id, req));
}