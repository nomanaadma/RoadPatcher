using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoadPatcher.Application.Data;
using RoadPatcher.Application.Entities;
using RoadPatcher.Helpers;
using RoadPatcher.Models;
using X.PagedList;
using X.PagedList.Extensions;

namespace RoadPatcher.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class UsersController(
	UserManager<ApplicationUser> userManager,
	RoleManager<IdentityRole> roleManager,
	IConfiguration configuration
) : Controller
{
	[HttpGet]
	public async Task<IActionResult> Index(string? search, int page = 1)
	{
		var pageSize = int.Parse(configuration["Paging:PageSize"] ?? "5");

		// Get all users
		var query = userManager.Users.AsQueryable();

		// Apply search filter
		if (!string.IsNullOrWhiteSpace(search))
		{
			var searchLower = search.ToLower();
			query = query.Where(u =>
				u.Email!.ToLower().Contains(searchLower) ||
				u.UserData!.FirstName.ToLower().Contains(searchLower) ||
				u.UserData!.LastName.ToLower().Contains(searchLower) ||
				u.UserData!.Address.ToLower().Contains(searchLower) ||
				u.UserData!.GoogleAddress.ToLower().Contains(searchLower)
			);
		}

		// Fetch users with their roles and profile data
		var users = await query
			.Include(u => u.UserData)
			.OrderByDescending(u => u.Id)
			.ToListAsync();

		// Project to DTO with role information
		var userDtos = new List<AdminUserListItemDto>();
		foreach (var user in users)
		{
			var roles = await userManager.GetRolesAsync(user);
			var profileImage = user.UserData != null 
				? ProfileHelper.GetProfileImage(user.UserData.Image) 
				: ProfileHelper.GetProfileImage(null);

			userDtos.Add(new AdminUserListItemDto
			{
				Id = user.Id,
				Email = user.Email!,
				FirstName = user.UserData?.FirstName ?? "N/A",
				LastName = user.UserData?.LastName ?? "N/A",
				Address = user.UserData?.Address ?? "N/A",
				GoogleAddress = user.UserData?.GoogleAddress ?? "N/A",
				Image = profileImage,
				Role = roles.FirstOrDefault() ?? "No Role",
				AllRoles = string.Join(", ", roles),
				CreatedAt = user.Id // We don't have CreatedAt on ApplicationUser, so we'll use Id
			});
		}

		// Paginate
		var pagedUsers = userDtos.ToPagedList(page, pageSize);

		// Store search for view
		ViewData["Search"] = search;

		return View(pagedUsers);
	}

	[HttpGet]
	public async Task<IActionResult> Create()
	{
		var roles = await roleManager.Roles.ToListAsync();
		ViewBag.Roles = roles;
		return View("CreateEdit");
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create(AdminUserCreateEditDto model)
	{
		if (!ModelState.IsValid)
		{
			var roles = await roleManager.Roles.ToListAsync();
			ViewBag.Roles = roles;
			return View("CreateEdit", model);
		}

		// Check if email already exists
		var existingUser = await userManager.FindByEmailAsync(model.Email);
		if (existingUser != null)
		{
			ModelState.AddModelError(nameof(model.Email), "Email already in use.");
			var roles = await roleManager.Roles.ToListAsync();
			ViewBag.Roles = roles;
			return View("CreateEdit", model);
		}

		// Create the user
		var user = new ApplicationUser
		{
			UserName = model.Email,
			Email = model.Email,
			EmailConfirmed = true // Admin-created users are pre-confirmed
		};

		var result = await userManager.CreateAsync(user, model.Password!);
		if (!result.Succeeded)
		{
			foreach (var error in result.Errors)
				ModelState.AddModelError(string.Empty, error.Description);

			var roleList = await roleManager.Roles.ToListAsync();
			ViewBag.Roles = roleList;
			return View("CreateEdit", model);
		}

		// Assign role (default to "User" if not specified)
		var roleToAssign = !string.IsNullOrWhiteSpace(model.Role) ? model.Role : "User";
		var roleExists = await roleManager.RoleExistsAsync(roleToAssign);
		if (roleExists)
		{
			await userManager.AddToRoleAsync(user, roleToAssign);
		}
		else if (!string.IsNullOrWhiteSpace(model.Role))
		{
			// If a role was specified but doesn't exist, log or warn (for now, just skip)
			ModelState.AddModelError(string.Empty, $"Role '{model.Role}' does not exist.");
		}

		TempData["Success"] = $"User '{model.Email}' created successfully.";
		return RedirectToAction("Index");
	}

	[HttpGet]
	public async Task<IActionResult> Edit(string id)
	{
		var user = await userManager.FindByIdAsync(id);
		if (user == null)
			return NotFound();

		var roles = await roleManager.Roles.ToListAsync();
		var userRoles = await userManager.GetRolesAsync(user);

		var model = new AdminUserCreateEditDto
		{
			Id = user.Id,
			Email = user.Email!,
			Role = userRoles.FirstOrDefault() ?? "User"
		};

		ViewBag.Roles = roles;
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

		if (!ModelState.IsValid)
		{
			var roles = await roleManager.Roles.ToListAsync();
			var userRoles = await userManager.GetRolesAsync(user);
			ViewBag.Roles = roles;
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
				var rolesList = await roleManager.Roles.ToListAsync();
				var userRolesList = await userManager.GetRolesAsync(user);
				ViewBag.Roles = rolesList;
				ViewBag.UserRoles = userRolesList;
				return View("CreateEdit", model);
			}
		}

		// Update email
		user.Email = model.Email;
		user.UserName = model.Email;

		// Update password if provided
		if (!string.IsNullOrWhiteSpace(model.Password))
		{
			var token = await userManager.GeneratePasswordResetTokenAsync(user);
			var resetResult = await userManager.ResetPasswordAsync(user, token, model.Password);
			if (!resetResult.Succeeded)
			{
				foreach (var error in resetResult.Errors)
					ModelState.AddModelError(string.Empty, error.Description);

				var rolesList = await roleManager.Roles.ToListAsync();
				var userRolesList = await userManager.GetRolesAsync(user);
				ViewBag.Roles = rolesList;
				ViewBag.UserRoles = userRolesList;
				return View("CreateEdit", model);
			}
		}

		// Update role
		var currentRoles = await userManager.GetRolesAsync(user);
		if (!currentRoles.Contains(model.Role))
		{
			await userManager.RemoveFromRolesAsync(user, currentRoles);
			var roleExists = await roleManager.RoleExistsAsync(model.Role);
			if (roleExists)
				await userManager.AddToRoleAsync(user, model.Role);
		}

		var updateResult = await userManager.UpdateAsync(user);
		if (!updateResult.Succeeded)
		{
			foreach (var error in updateResult.Errors)
				ModelState.AddModelError(string.Empty, error.Description);

			var rolesList = await roleManager.Roles.ToListAsync();
			var userRolesList = await userManager.GetRolesAsync(user);
			ViewBag.Roles = rolesList;
			ViewBag.UserRoles = userRolesList;
			return View("CreateEdit", model);
		}

		TempData["Success"] = $"User '{model.Email}' updated successfully.";
		return RedirectToAction("Index");
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Delete(string id)
	{
		var user = await userManager.FindByIdAsync(id);
		if (user == null)
			return NotFound();

		// Prevent deleting self
		var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (user.Id == currentUserId)
		{
			TempData["Error"] = "You cannot delete your own account.";
			return RedirectToAction("Index");
		}

		var result = await userManager.DeleteAsync(user);
		if (!result.Succeeded)
		{
			TempData["Error"] = "Failed to delete user.";
			return RedirectToAction("Index");
		}

		TempData["Success"] = $"User '{user.Email}' deleted successfully.";
		return RedirectToAction("Index");
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
	public string Role { get; set; } = null!;
	public string AllRoles { get; set; } = null!;
	public string CreatedAt { get; set; } = null!;
}

public class AdminUserCreateEditDto
{
	public string? Id { get; set; }
	public string Email { get; set; } = null!;
	public string? Password { get; set; }
	public string Role { get; set; } = "User";
}
