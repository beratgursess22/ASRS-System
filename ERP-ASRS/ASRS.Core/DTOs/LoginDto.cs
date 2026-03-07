using System.ComponentModel.DataAnnotations;

namespace ASRS.Core.DTOs;

public class LoginDto
{
    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = false; // kullanıcının oturumunun uzun süre açık kalmasını sağlamak
}
