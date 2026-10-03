using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SariSariPOS.API.DTOs;
using SariSariPOS.API.Interfaces;

namespace SariSariPOS.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    [HttpGet("needs-setup")]
    public async Task<IActionResult> NeedsSetup()
        => Ok(new { needsSetup = await _auth.NeedsSetupAsync() });

    [HttpPost("setup")]
    public async Task<IActionResult> Setup([FromBody] SetupRequest req)
        => Ok(await _auth.SetupAsync(req));

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
        => Ok(await _auth.LoginAsync(req));

    [HttpPost("logout")]
    public IActionResult Logout() => Ok(new { message = "Logged out" });

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("Invalid token");
        var userId = Guid.Parse(sub);
        return Ok(await _auth.GetCurrentUserAsync(userId));
    }
}