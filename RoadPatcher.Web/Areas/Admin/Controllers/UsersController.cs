using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoadPatcher.Application.Entities;
using RoadPatcher.Helpers;
using X.PagedList.Extensions;

namespace RoadPatcher.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class UsersController(
	UserManager<ApplicationUser> userManager,
	RoleManager<IdentityRole> roleManager,
	IConfiguration config
) : Controller
{

	private readonly int _pageSize = config.GetValue("Paging:UsersPageSize", 10);

	[HttpGet]
	public async Task<IActionResult> Index(string? search, string? role, int page = 1)
	{
		var canManageRoles = User.IsInRole("SuperAdmin");

		var allUsers = await userManager.Users
			.Include(u => u.UserData)
			.OrderBy(u => u.Email)
			.ToListAsync();

		var users = new List<AdminUserListItemDto>();

		foreach (var user in allUsers)
		{
			var roles = await userManager.GetRolesAsync(user);
			if (roles.Contains("SuperAdmin"))
				continue;

			var isDisabled = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
			var profileImage = ProfileHelper.GetProfileImage(user.UserData?.Image);

			users.Add(new AdminUserListItemDto
			{
				Id = user.Id,
				Email = user.Email ?? string.Empty,
				FirstName = user.UserData?.FirstName ?? "N/A",
				LastName = user.UserData?.LastName ?? "N/A",
				Address = user.UserData?.Address ?? "N/A",
				GoogleAddress = user.UserData?.GoogleAddress ?? "N/A",
				Image = profileImage,
				PrimaryRole = roles.FirstOrDefault() ?? "User",
				Roles = roles.ToList(),
				IsDisabled = isDisabled
			});
		}

		IEnumerable<AdminUserListItemDto> filteredUsers = users;

		if (!string.IsNullOrWhiteSpace(search))
		{
			filteredUsers = filteredUsers.Where(u =>
				u.Email.Contains(search, StringComparison.OrdinalIgnoreCase) ||
				u.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
				u.LastName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
				u.Address.Contains(search, StringComparison.OrdinalIgnoreCase) ||
				u.GoogleAddress.Contains(search, StringComparison.OrdinalIgnoreCase)
			);
		}

		if (canManageRoles && !string.IsNullOrWhiteSpace(role))
		{
			filteredUsers = filteredUsers.Where(u => u.Roles.Contains(role));
		}

		var pagedUsers = filteredUsers.ToPagedList(page, _pageSize);

		ViewData["Search"] = search;
		ViewData["RoleFilter"] = role;
		ViewBag.CanManageRoles = canManageRoles;
		ViewBag.AvailableRoles = canManageRoles ? await GetAvailableRolesAsync() : new List<string>();

		return View(pagedUsers);
	}

	[HttpGet]
	public async Task<IActionResult> Create(string? returnUrl = null)
	{
		await PrepareFormDataAsync("User");
		return View("CreateEdit", new AdminUserCreateEditDto
		{
			ReturnUrl = returnUrl
		});
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create(AdminUserCreateEditDto model)
	{
		await PrepareFormDataAsync(model.Role);

		if (!ModelState.IsValid)
		{
			return View("CreateEdit", model);
		}

		// Validate password is provided on create
		if (string.IsNullOrWhiteSpace(model.Password))
		{
			ModelState.AddModelError(nameof(model.Password), "Password is required.");
			return View("CreateEdit", model);
		}

		var canManageRoles = User.IsInRole("SuperAdmin");
		var roleToAssign = canManageRoles ? (string.IsNullOrWhiteSpace(model.Role) ? "User" : model.Role) : "User";

		if (canManageRoles && !await roleManager.RoleExistsAsync(roleToAssign))
		{
			ModelState.AddModelError(nameof(model.Role), "Selected role does not exist.");
			return View("CreateEdit", model);
		}

		// Check if email already exists
		var existingUser = await userManager.FindByEmailAsync(model.Email);
		if (existingUser != null)
		{
			ModelState.AddModelError(nameof(model.Email), "Email already in use.");
			return View("CreateEdit", model);
		}

		// Create the user
		var user = new ApplicationUser
		{
			UserName = model.Email,
			Email = model.Email,
			EmailConfirmed = true,
			LockoutEnabled = true
		};

		var result = await userManager.CreateAsync(user, model.Password!);
		if (!result.Succeeded)
		{
			foreach (var error in result.Errors)
				ModelState.AddModelError(string.Empty, error.Description);
			return View("CreateEdit", model);
		}

		var roleAssignment = canManageRoles ? roleToAssign : "User";
		await userManager.AddToRoleAsync(user, roleAssignment);

		TempData["Success"] = $"User '{model.Email}' created successfully.";
		if (Url.IsLocalUrl(model.ReturnUrl))
			return Redirect(model.ReturnUrl!);
		return RedirectToAction("Index");
	}

	[HttpGet]
	public async Task<IActionResult> Edit(string id, string? returnUrl = null)
	{
		var user = await userManager.FindByIdAsync(id);
		if (user == null)
			return NotFound();

		var userRoles = await userManager.GetRolesAsync(user);
		var canManageRoles = User.IsInRole("SuperAdmin");
		var currentRole = userRoles.FirstOrDefault() ?? "User";
		await PrepareFormDataAsync(currentRole);

		var model = new AdminUserCreateEditDto
		{
			Id = user.Id,
			Email = user.Email!,
			Role = currentRole,
			ReturnUrl = returnUrl
		};

		ViewBag.CanManageRoles = canManageRoles;
		ViewBag.UserRoles = userRoles;
		return View("CreateEdit", model);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Edit(string id, AdminUserCreateEditDto model)
	{
		var user = await userManager.FindByIdAsync(id);
		if (user == null)
			return NotFound();

		var targetRoles = await userManager.GetRolesAsync(user);
		var targetIsAdminOrAbove = targetRoles.Contains("Admin") || targetRoles.Contains("SuperAdmin");
		var requesterIsSuperAdmin = User.IsInRole("SuperAdmin");

		if (targetIsAdminOrAbove && !requesterIsSuperAdmin)
			return Forbid();

		var canManageRoles = User.IsInRole("SuperAdmin");
		await PrepareFormDataAsync(model.Role);

		if (!ModelState.IsValid)
		{
			var userRoles = await userManager.GetRolesAsync(user);
			ViewBag.CanManageRoles = canManageRoles;
			ViewBag.UserRoles = userRoles;
			return View("CreateEdit", model);
		}

		// Check if email is being changed and if it's already in use
		if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
		{
			var existingUser = await userManager.FindByEmailAsync(model.Email);
			if (existingUser != null)
			{
				ModelState.AddModelError(nameof(model.Email), "Email already in use.");
				var userRolesList = await userManager.GetRolesAsync(user);
				ViewBag.CanManageRoles = canManageRoles;
				ViewBag.UserRoles = userRolesList;
				return View("CreateEdit", model);
			}
		}

		// Update email
		await userManager.SetUserNameAsync(user, model.Email);
		await userManager.SetEmailAsync(user, model.Email);
		user.EmailConfirmed = true;

		// Update password if provided
		if (!string.IsNullOrWhiteSpace(model.Password))
		{
			var token = await userManager.GeneratePasswordResetTokenAsync(user);
			var resetResult = await userManager.ResetPasswordAsync(user, token, model.Password);
			if (!resetResult.Succeeded)
			{
				foreach (var error in resetResult.Errors)
					ModelState.AddModelError(string.Empty, error.Description);

				var userRolesList = await userManager.GetRolesAsync(user);
				ViewBag.CanManageRoles = canManageRoles;
				ViewBag.UserRoles = userRolesList;
				return View("CreateEdit", model);
			}
		}

		if (canManageRoles)
		{
			var currentRoles = await userManager.GetRolesAsync(user);
			var selectedRole = string.IsNullOrWhiteSpace(model.Role) ? "User" : model.Role;

			if (!await roleManager.RoleExistsAsync(selectedRole))
			{
				ModelState.AddModelError(nameof(model.Role), "Selected role does not exist.");
				var userRolesList = await userManager.GetRolesAsync(user);
				ViewBag.CanManageRoles = canManageRoles;
				ViewBag.UserRoles = userRolesList;
				return View("CreateEdit", model);
			}

			if (currentRoles.Count != 1 || !string.Equals(currentRoles[0], selectedRole, StringComparison.OrdinalIgnoreCase))
			{
				await userManager.RemoveFromRolesAsync(user, currentRoles);
				await userManager.AddToRoleAsync(user, selectedRole);
			}
		}

		var updateResult = await userManager.UpdateAsync(user);
		if (!updateResult.Succeeded)
		{
			foreach (var error in updateResult.Errors)
				ModelState.AddModelError(string.Empty, error.Description);

			var userRolesList = await userManager.GetRolesAsync(user);
			ViewBag.CanManageRoles = canManageRoles;
			ViewBag.UserRoles = userRolesList;
			return View("CreateEdit", model);
		}

		TempData["Success"] = $"User '{model.Email}' updated successfully.";
		if (Url.IsLocalUrl(model.ReturnUrl))
			return Redirect(model.ReturnUrl!);
		return RedirectToAction("Index");
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> ToggleStatus(string id)
	{
		var user = await userManager.FindByIdAsync(id);
		if (user == null)
			return Json(new { success = false, message = "User not found." });

		var targetRoles = await userManager.GetRolesAsync(user);
		var targetIsAdminOrAbove = targetRoles.Contains("Admin") || targetRoles.Contains("SuperAdmin");
		var requesterIsSuperAdmin = User.IsInRole("SuperAdmin");

		if (targetIsAdminOrAbove && !requesterIsSuperAdmin)
			return Json(new { success = false, message = "SuperAdmin only action." });

		var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (user.Id == currentUserId)
		{
			return Json(new { success = false, message = "You cannot disable your own account." });
		}

		var isDisabled = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
		user.LockoutEnabled = true;
		user.LockoutEnd = isDisabled ? null : DateTimeOffset.MaxValue;

		var result = await userManager.UpdateAsync(user);
		if (!result.Succeeded)
		{
			return Json(new { success = false, message = "Failed to update user status." });
		}

		return Json(new
		{
			success = true,
			isDisabled = !isDisabled,
			message = isDisabled
				? $"User '{user.Email}' enabled successfully."
				: $"User '{user.Email}' disabled successfully."
		});
	}

	private async Task PrepareFormDataAsync(string? selectedRole = null)
	{
		var canManageRoles = User.IsInRole("SuperAdmin");
		ViewBag.CanManageRoles = canManageRoles;

		if (canManageRoles)
		{
			ViewBag.AvailableRoles = await GetAvailableRolesAsync();
			ViewBag.SelectedRole = selectedRole;
		}
	}

	private async Task<List<string>> GetAvailableRolesAsync()
	{
		return await roleManager.Roles
			.Where(r => r.Name != "SuperAdmin")
			.OrderBy(r => r.Name)
			.Select(r => r.Name!)
			.ToListAsync();
	}
}

// DTOs
public class AdminUserListItemDto
{
	public string Id { get; set; } = null!;
	public string Email { get; set; } = null!;
	public string FirstName { get; set; } = null!;
	public string LastName { get; set; } = null!;
	public string Address { get; set; } = null!;
	public string GoogleAddress { get; set; } = null!;
	public string Image { get; set; } = null!;
	public string PrimaryRole { get; set; } = null!;
	public List<string> Roles { get; set; } = [];
	public bool IsDisabled { get; set; }
}

public class AdminUserCreateEditDto
{
	public string? Id { get; set; }
	public string Email { get; set; } = null!;
	public string? Password { get; set; }
	public string Role { get; set; } = "User";
	public string? ReturnUrl { get; set; }
}
