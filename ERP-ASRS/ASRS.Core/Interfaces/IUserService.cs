using ASRS.Core.DTOs;

namespace ASRS.Core.Interfaces;

public interface IUserService
{
    Task<UserDto?> LoginAsync(LoginDto dto); 
    Task<IEnumerable<UserListDto>> GetAllUsersAsync();
    Task<UserListDto?> GetUserByIdAsync(string id);
    Task<bool> CreateUserAsync(CreateUserDto dto);
    Task<bool> UpdateUserAsync(string id, string fullName, string role, bool isActive);
    Task<bool> DeleteUserAsync(string id);
    Task<IEnumerable<string>> GetRolesAsync();
}