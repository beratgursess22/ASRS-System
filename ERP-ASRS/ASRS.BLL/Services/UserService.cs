using ASRS.Core.DTOs;
using ASRS.Core.Entities;
using ASRS.Core.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace ASRS.BLL.Services;

public class UserService : IUserService // kullanıcı işlemlerini gerçekleştiren servis sınıfı
{
	private readonly UserManager<AppUser> _userManager;
	private readonly SignInManager<AppUser> _signInManager;
	private readonly RoleManager<AppRole> _roleManager;

	public UserService(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, RoleManager<AppRole> roleManager)
	{
		_userManager = userManager;
		_signInManager = signInManager;
		_roleManager = roleManager;
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

	public async Task<IEnumerable<UserListDto>> GetAllUsersAsync()
	{
		var users = _userManager.Users.ToList();
		var result = new List<UserListDto>();

		foreach (var user in users)
		{
			var roles = await _userManager.GetRolesAsync(user);
			result.Add(new UserListDto
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

	public async Task<UserListDto?> GetUserByIdAsync(string id)
	{
		var user = await _userManager.FindByIdAsync(id);
		if (user == null)
			return null;

		var roles = await _userManager.GetRolesAsync(user);
		return new UserListDto
		{
			Id = user.Id,
			FullName = user.FullName,
			Email = user.Email!,
			Role = roles.FirstOrDefault() ?? string.Empty,
			IsActive = user.IsActive
		};
	}

	public async Task<bool> CreateUserAsync(CreateUserDto dto)
	{
		var parts = dto.FullName.Trim().Split(' ', 2);

		string lastName = string.Empty;
		if (parts.Length > 1)
			lastName = parts[1];

		var user = new AppUser
		{
			FirstName = parts[0],
			LastName = lastName,
			Email = dto.Email,
			UserName = dto.Email,
			IsActive = true
		};

		var result = await _userManager.CreateAsync(user, dto.Password);
		if (!result.Succeeded)
			return false;

		await _userManager.AddToRoleAsync(user, dto.Role);
		return true;
	}

	public async Task<bool> UpdateUserAsync(string id, string fullName, string role, bool isActive)
	{
		var user = await _userManager.FindByIdAsync(id);
		if (user == null)
			return false;

		var parts = fullName.Trim().Split(' ', 2);
		user.FirstName = parts[0];

		if (parts.Length > 1)
			user.LastName = parts[1];
		else
			user.LastName = string.Empty;
		user.IsActive = isActive;

		var currentRoles = await _userManager.GetRolesAsync(user);
		await _userManager.RemoveFromRolesAsync(user, currentRoles);
		await _userManager.AddToRoleAsync(user, role);

		var result = await _userManager.UpdateAsync(user);
		return result.Succeeded;
	}

	public async Task<bool> DeleteUserAsync(string id)
	{
		var user = await _userManager.FindByIdAsync(id);
		if (user == null)
			return false;

		var result = await _userManager.DeleteAsync(user);
		return result.Succeeded;
	}

	public async Task<IEnumerable<string>> GetRolesAsync()
	{
		return await Task.FromResult(_roleManager.Roles.Select(r => r.Name!).ToList());
	}
}