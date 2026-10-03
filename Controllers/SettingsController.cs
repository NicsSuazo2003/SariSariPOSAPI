using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SariSariPOS.API.DTOs;
using SariSariPOS.API.Interfaces;

namespace SariSariPOS.API.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _svc;
    public SettingsController(ISettingsService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> Get() => Ok(await _svc.GetAsync());

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateSettingsRequest req)
        => Ok(await _svc.UpdateAsync(req));
}