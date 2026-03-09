using System.ComponentModel.DataAnnotations;

namespace ASRS.Core.DTOs;

public class CreateUserDto // kullanıcı oluşturmak için kullanılan DTO
{
	[Required(ErrorMessage = "Ad soyad zorunludur.")]
	public string FullName { get; set; } = string.Empty;

	[Required(ErrorMessage = "E-posta zorunludur.")]
	[EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
	public string Email { get; set; } = string.Empty;

	[Required(ErrorMessage = "Şifre zorunludur.")]
	[MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
	public string Password { get; set; } = string.Empty;

	[Required(ErrorMessage = "Rol seçmek zorunludur.")]
	public string Role { get; set; } = string.Empty;
}
