namespace SariSariPOS.API.Interfaces;

public interface IQRCodeService
{
    byte[] GenerateProductQr(Guid productId);
    byte[] GenerateReceiptQr(Guid saleId);
}