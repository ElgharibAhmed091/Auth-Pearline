using AuthAPI.DTOs.Admin;
using AuthAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuthAPI.Controllers.Admin
{
    [Route("api/admin/profile")]
    [ApiController]
    // Allow both Admin and SuperAdmin to read profile
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AdminProfileController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminProfileController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // GET: api/admin/profile
        // Both Admin and SuperAdmin can view their profile
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var dto = new AdminProfileDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? "",
                MobileNumber = user.MobileNumber
            };

            return Ok(dto);
        }

        // PUT: api/admin/profile
        // ONLY SuperAdmin can update profile (Admins cannot)
        [HttpPut]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateProfile([FromBody] AdminUpdateProfileDto dto)
        {
            // If you prefer SuperAdmin to update any user's profile, accept an id param.
            // This implementation updates the SuperAdmin's own profile (i.e. the caller).
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.Email = dto.Email;
            user.UserName = dto.Email;
            user.MobileNumber = dto.MobileNumber;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { Message = "Profile updated successfully" });
        }

        // PUT: api/admin/profile/change-password
        // ONLY SuperAdmin can change password via this endpoint
        [HttpPut("change-password")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // using ChangePasswordAsync requires current password of the user being changed.
            // This endpoint assumes SuperAdmin changes their own password.
            // If you want SuperAdmin to change others' passwords, implement another endpoint that uses
            // RemovePasswordAsync / AddPasswordAsync or ResetPasswordAsync with a token.
            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { Message = "Password changed successfully" });
        }
    }
}
