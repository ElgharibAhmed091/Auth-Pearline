using AuthAPI.DTOs;
using AuthAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthAPI.Controllers
{
    [Route("api/admin/users")]
    [ApiController]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AdminUsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminUsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // ✅ Get All Users (includes roles)
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userManager.Users.ToListAsync();

            var userDtos = new List<UserWithRolesDto>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                userDtos.Add(new UserWithRolesDto
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email ?? "",
                    Roles = roles.ToList()
                });
            }

            return Ok(new ApiResponse<List<UserWithRolesDto>>
            {
                Success = true,
                Message = "Users retrieved successfully",
                Data = userDtos
            });
        }

        // ✅ Get User By Id (includes roles)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound(new ApiResponse<string>
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            var roles = await _userManager.GetRolesAsync(user);

            var userDto = new UserWithRolesDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? "",
                Roles = roles.ToList()
            };

            return Ok(new ApiResponse<UserWithRolesDto>
            {
                Success = true,
                Message = "User retrieved successfully",
                Data = userDto
            });
        }

        // ✅ Delete User
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound(new ApiResponse<string>
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            // If the target user is Admin, only SuperAdmin can delete them
            var isTargetAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            var currentUser = await _userManager.GetUserAsync(User);
            var isCurrentSuper = currentUser != null && await _userManager.IsInRoleAsync(currentUser, "SuperAdmin");

            if (isTargetAdmin && !isCurrentSuper)
            {
                return Forbid(); // Admins cannot delete Admins
            }

            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Failed to delete user"
                });
            }

            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = "User deleted successfully"
            });
        }

        // SuperAdmin-only: Assign Admin role
        [HttpPost("{id}/assign-admin")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> AssignAdminRole(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new ApiResponse<string> { Success = false, Message = "User not found" });

            if (!await _roleManager.RoleExistsAsync("Admin"))
                await _roleManager.CreateAsync(new IdentityRole("Admin"));

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
                return BadRequest(new ApiResponse<string> { Success = false, Message = "User already has Admin role" });

            var res = await _userManager.AddToRoleAsync(user, "Admin");
            if (!res.Succeeded)
                return BadRequest(new ApiResponse<string> { Success = false, Message = "Failed to assign Admin role" });

            return Ok(new ApiResponse<string> { Success = true, Message = "Admin role assigned" });
        }

        // SuperAdmin-only: Revoke Admin role
        [HttpPost("{id}/revoke-admin")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> RevokeAdminRole(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new ApiResponse<string> { Success = false, Message = "User not found" });

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Admin"))
                return BadRequest(new ApiResponse<string> { Success = false, Message = "User is not an Admin" });

            var res = await _userManager.RemoveFromRoleAsync(user, "Admin");
            if (!res.Succeeded)
                return BadRequest(new ApiResponse<string> { Success = false, Message = "Failed to revoke Admin role" });

            return Ok(new ApiResponse<string> { Success = true, Message = "Admin role revoked" });
        }
    }

    public class UserWithRolesDto
    {
        public string Id { get; set; } = default!;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
    }
}
