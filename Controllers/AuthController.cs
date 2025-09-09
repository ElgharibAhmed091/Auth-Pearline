using AuthAPI.Data;
using AuthAPI.Helpers;
using AuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static AuthAPI.DTOs.AuthDtos;

namespace AuthAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _config;
    private readonly ApplicationDbContext _db;
    private readonly IEmailService _emailService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration config,
        ApplicationDbContext db,
        IEmailService emailService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _config = config;
        _db = db;
        _emailService = emailService;
    }

    // ===== Register =====
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (dto.Password != dto.ConfirmPassword)
            return BadRequest("Passwords do not match.");

        var existing = await _userManager.FindByEmailAsync(dto.Email);
        if (existing is not null) return BadRequest("Email already registered.");

        if (!CountryHelper.Countries.Contains(dto.Country))
            return BadRequest("Invalid country selected.");

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            MobileNumber = dto.MobileNumber,
            PhoneNumber = dto.PhoneNumber,
            CompanyName = dto.CompanyName,
            CompanyWebsite = dto.CompanyWebsite,
            VatNumber = dto.VatNumber,
            StreetAddress = dto.StreetAddress,
            City = dto.City,
            Country = dto.Country,
            State = dto.State,
            ZipCode = dto.ZipCode
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded) return BadRequest(result.Errors);

        return Ok("User registered successfully.");
    }

    // ===== Login =====
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null) return Unauthorized("Invalid credentials.");

        var check = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: false);
        if (!check.Succeeded) return Unauthorized("Invalid credentials.");

        var claims = new List<Claim>
{
    new(JwtRegisteredClaimNames.Sub, user.Email ?? ""),
    new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
    new(ClaimTypes.NameIdentifier, user.Id),
    new(ClaimTypes.Email, user.Email ?? "")
};


        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return Ok(new { token = jwt });
    }

    // ===== Forgot Password =====
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null) return Ok("If the email exists, a reset link will be sent.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetLink = $"{_config["App:ClientUrl"]}/reset-password?email={user.Email}&token={Uri.EscapeDataString(token)}";

        var body = $@"
<html>
  <body style='font-family: Arial, sans-serif; color:#333; background-color:#ffffff; padding:20px; margin:0;'>
    <div style='max-width:600px; margin:0 auto; background:#fff;'>
      
      <!-- Logo -->
      <div style='margin-bottom:20px;'>
        <img src='https://i.ibb.co/JwJ5pVjL/logo-Blueeee.jpg' alt='Pearline Logo' style='max-width:180px;' />
      </div>

      <!-- Greeting -->
      <p style='font-size:15px; color:#333; margin-bottom:20px;'>
        Hello {user.UserName},
      </p>

      <!-- Message -->
      <p style='font-size:15px; color:#333; margin-bottom:15px;'>
        There was recently a request to change the password for your account.
      </p>
      <p style='font-size:15px; color:#333; margin-bottom:25px;'>
        If you requested this change, set a new password here:
      </p>

      <!-- Button -->
      <a href='{resetLink}' 
         style='display:inline-block; padding:12px 24px; background-color:#0B6C63; color:#fff; 
                text-decoration:none; font-size:15px; font-weight:bold; border-radius:4px;'>
        Set a New Password
      </a>

      <!-- Footer -->
      <p style='font-size:14px; color:#555; margin-top:30px; line-height:1.4;'>
        If you did not make this request, you can ignore this email and your password will remain the same.
      </p>

      <p style='font-size:14px; color:#333; margin-top:20px;'>
        Thank you, Pearline Team
      </p>
    </div>
  </body>
</html>";



        await _emailService.SendEmailAsync(
            dto.Email,
            "Reset Your Password - Pearline",
            body
        );

        return Ok("If the email exists, a reset link will be sent.");
    }


    // ===== Reset Password =====
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null) return BadRequest("User not found.");

        var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
        if (!result.Succeeded) return BadRequest(result.Errors);

        return Ok("Password has been reset successfully.");
    }

    // ===== Get Countries =====
    [HttpGet("countries")]
    public IActionResult GetCountries()
    {
        return Ok(CountryHelper.Countries);
    }

    [Authorize]
    [HttpDelete("delete-account")]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);

        ApplicationUser? user = null;

        if (!string.IsNullOrEmpty(userId))
        {
            user = await _userManager.FindByIdAsync(userId);
        }

        if (user == null && !string.IsNullOrEmpty(email))
        {
            user = await _userManager.FindByEmailAsync(email);
        }

        if (user == null) return NotFound("User not found.");

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded) return BadRequest(result.Errors);

        return Ok("Account deleted successfully.");
    }


}
