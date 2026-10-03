using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SariSariPOS.API.Interfaces;

namespace SariSariPOS.API.Controllers;

[ApiController]
[Route("api/qr")]
[Authorize]
public class QRController : ControllerBase
{
    private readonly IQRCodeService _svc;
    public QRController(IQRCodeService svc) => _svc = svc;

    [HttpGet("product/{id:guid}")]
    public IActionResult ProductQr(Guid id)
        => File(_svc.GenerateProductQr(id), "image/png");

    [HttpGet("receipt/{id:guid}")]
    public IActionResult ReceiptQr(Guid id)
        => File(_svc.GenerateReceiptQr(id), "image/png");
}