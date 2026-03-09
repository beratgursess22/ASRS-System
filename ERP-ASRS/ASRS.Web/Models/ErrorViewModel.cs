namespace ASRS.Web.Models;

public class ErrorViewModel // hata sayfasında kullanılan model, hata bilgilerini tutar
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
