using System.Text.Json;
using QRCoder;
using SariSariPOS.API.Interfaces;

namespace SariSariPOS.API.Services;

public class QRCodeService : IQRCodeService
{
    public byte[] GenerateProductQr(Guid productId)
    {
        var payload = JsonSerializer.Serialize(new { t = "P", id = productId.ToString() });
        return Generate(payload);
    }

    public byte[] GenerateReceiptQr(Guid saleId)
    {
        var payload = JsonSerializer.Serialize(new { t = "R", id = saleId.ToString() });
        return Generate(payload);
    }

    private static byte[] Generate(string payload)
    {
        using var gen = new QRCodeGenerator();
        using var data = gen.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        using var qr = new PngByteQRCode(data);
        return qr.GetGraphic(20);
    }
}