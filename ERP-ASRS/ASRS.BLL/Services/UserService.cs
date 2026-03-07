using ASRS.Core.DTOs;
using ASRS.Core.Entities;
using ASRS.Core.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace ASRS.BLL.Services;

public class UserService : IUserService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;

    public UserService(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<UserDto?> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null || !user.IsActive)
            return null;

        var result = await _signInManager.PasswordSignInAsync(user, dto.Password, dto.RememberMe, false);
        if (!result.Succeeded)
            return null;

		var roles = await _userManager.GetRolesAsync(user);
		return new UserDto
		{
			Id = user.Id,
			FullName = user.FullName,
			Email = user.Email!,
			Role = roles.FirstOrDefault() ?? string.Empty,
			IsActive = user.IsActive
		};
	}

	public async Task<UserDto?> GetUserByIdAsync(string id)
	{
		var user = await _userManager.FindByIdAsync(id);
		if (user == null)
			return null;

		var roles = await _userManager.GetRolesAsync(user);
		return new UserDto
		{
			Id = user.Id,
			FullName = user.FullName,
			Email = user.Email!,
			Role = roles.FirstOrDefault() ?? string.Empty,
			IsActive = user.IsActive
		};
	}
	public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
	{
		var users = _userManager.Users.Where(u => u.IsActive).ToList();
		var result = new List<UserDto>();

		foreach (var user in users)
		{
			var roles = await _userManager.GetRolesAsync(user);
			result.Add(new UserDto
			{
				Id = user.Id,
				FullName = user.FullName,
				Email = user.Email!,
				Role = roles.FirstOrDefault() ?? string.Empty,
				IsActive = user.IsActive
			});
		}
		return result;
	}
	public async Task<bool> CreateUserAsync(string firstName, string lastName, string email, string password, string role, int? departmentId)
	{
		var user = new AppUser
		{
			FirstName = firstName,
			LastName = lastName,
			Email = email,
			UserName = email,
			DepartmentId = departmentId
		};

		var result = await _userManager.CreateAsync(user, password);
		if (!result.Succeeded) 
			return false;

		await _userManager.AddToRoleAsync(user, role);
		return true;
	}
	public async Task<bool> UpdateUserAsync(string id, string firstName, string lastName, int? departmentId, bool isActive)
	{
		var user = await _userManager.FindByIdAsync(id);
		if (user == null) 
			return false;

		user.FirstName = firstName;
		user.LastName = lastName;
		user.DepartmentId = departmentId;
		user.IsActive = isActive;

		var result = await _userManager.UpdateAsync(user);
		return result.Succeeded;
	}

	public async Task<bool> DeleteUserAsync(string id)
	{
		var user = await _userManager.FindByIdAsync(id);
		if (user == null) 
			return false;

		user.IsActive = false;
		var result = await _userManager.UpdateAsync(user);
		return result.Succeeded;
	}
}
