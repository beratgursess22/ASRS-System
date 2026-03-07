using ASRS.Core.DTOs;

namespace ASRS.Core.Interfaces;

public interface IUserService
{
	// Bir sınıfın hangi fonksiyonları yapmak zorunda olduğunu tanımlar.
    Task<UserDto?> LoginAsync(LoginDto dto); //Task --> Bu fonksiyon asynchronous (eş zamanlı olmayan) çalışır. program beklemez ve diğer işlemleri yapmaya devam eder. Login işlemi tamamlandığında sonucu döner.
    Task<UserDto?> GetUserByIdAsync(string id);
    Task<IEnumerable<UserDto>> GetAllUsersAsync(); // IEnumerable --> Birden fazla veri dönecek.
    Task<bool> CreateUserAsync(string firstName, string lastName, string email, string password, string role, int? departmentId);
    Task<bool> UpdateUserAsync(string id, string firstName, string lastName, int? departmentId, bool isActive);
    Task<bool> DeleteUserAsync(string id);
}