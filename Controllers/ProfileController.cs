using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Security.Claims;
using AuthAPI.Models;   // مكان ما عندك ApplicationUser
using AuthAPI.DTOs;
using AuthAPI.DTOs.Admin;     // DTOs بتاعتك

namespace AuthAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // لازم يكون المستخدم مسجل دخول
    public class ProfileController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // ✅ Get current user profile
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized("User not found.");

            return Ok(new
            {
                user.FirstName,
                user.LastName,
                user.Email,
                user.MobileNumber,
                user.CompanyName,
                user.CompanyWebsite,
                user.VatNumber,
                user.StreetAddress,
                user.City,
                user.State,
                user.Country,
                user.ZipCode
            });
        }

        // ✅ Update profile info (no password required)
        [HttpPut("update")]
        public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized("User not found.");

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.MobileNumber = dto.MobileNumber;
            user.CompanyName = dto.CompanyName;
            user.CompanyWebsite = dto.CompanyWebsite;
            user.VatNumber = dto.VatNumber;
            user.StreetAddress = dto.StreetAddress;
            user.City = dto.City;
            user.State = dto.State;
            user.Country = dto.Country;
            user.ZipCode = dto.ZipCode;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok("Profile updated successfully.");
        }

        // ✅ Change email (requires current password)
        [HttpPut("change-email")]
        public async Task<IActionResult> ChangeEmail(ChangeEmailDto dto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized("User not found.");

            var checkPassword = await _userManager.CheckPasswordAsync(user, dto.CurrentPassword);
            if (!checkPassword) return BadRequest("Invalid current password.");

            var token = await _userManager.GenerateChangeEmailTokenAsync(user, dto.NewEmail);
            var result = await _userManager.ChangeEmailAsync(user, dto.NewEmail, token);

            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok("Email changed successfully.");
        }

        // ✅ Change password (requires current password)
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized("User not found.");

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok("Password changed successfully.");
        }
    }
}
