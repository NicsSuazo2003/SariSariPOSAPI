using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SariSariPOS.API.DTOs;
using SariSariPOS.API.Interfaces;

namespace SariSariPOS.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "PlatformAdmin")]   // ⭐ entire controller is admin-only
public class AdminController : ControllerBase
{
    private readonly IAdminService _admin;

    public AdminController(IAdminService admin) => _admin = admin;

    /// <summary>
    /// Creates a new tenant (store + owner). Returns the generated store code.
    /// </summary>
    [HttpPost("stores")]
    public async Task<IActionResult> CreateStore([FromBody] CreateStoreRequest req)
        => Ok(await _admin.CreateStoreAsync(req));

    /// <summary>
    /// Lists all tenants with light usage stats.
    /// </summary>
    [HttpGet("stores")]
    public async Task<IActionResult> ListStores()
        => Ok(await _admin.ListStoresAsync());
}