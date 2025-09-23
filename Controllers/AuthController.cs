using AuthAPI.Data;
using AuthAPI.Helpers;
using AuthAPI.Models;
using AuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            new Claim(ClaimTypes.NameIdentifier, user.Id), // ✅ أساسي
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim(JwtRegisteredClaimNames.Sub, user.Email ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
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
    // ===== Forgot Password: generate & send OTP =====
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null) return Ok("If the email exists, an OTP will be sent."); // avoid user enumeration

        // Generate 6-digit OTP
        var random = new Random();
        var otp = random.Next(100000, 999999).ToString();

        // Save to DB (invalidate previous OTPs for same email)
        var existingOtps = await _db.OtpCodes.Where(x => x.Email == dto.Email && !x.IsUsed).ToListAsync();
        foreach (var x in existingOtps) x.IsUsed = true; // invalidate old ones
        await _db.SaveChangesAsync();

        var entry = new OtpCode
        {
            Email = dto.Email,
            Code = otp,
            ExpirationTimeUtc = DateTime.UtcNow.AddMinutes(5),
            IsUsed = false
        };
        await _db.OtpCodes.AddAsync(entry);
        await _db.SaveChangesAsync();

        // ===== Email body with logo + OTP =====
        string body = $@"
        <div style='font-family:Arial,sans-serif; text-align:center;'>
            <img src='https://i.ibb.co/gZSmfmRb/pearline-logo-png.jpg' width='120' alt='App Logo'/>
            <h2 style='color:#333;'>Reset Password OTP</h2>
  <h2 style='color:#333;'>Welcome to <span style='color:#007bff;'>Pearline</span></h2>
            <p>Your OTP code is:</p>
            <h1 style='color:#007bff; letter-spacing:5px;'>{otp}</h1>
            <p>This code will expire in <b>5 minutes</b>.</p>
        </div>
    ";

        await _emailService.SendEmailAsync(dto.Email, "Reset Password OTP", body);

        return Ok("If the email exists, an OTP will be sent.");
    }


    // ===== Verify OTP =====
    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp(VerifyOtpDto dto)
    {
        var otp = await _db.OtpCodes
            .Where(o => o.Email == dto.Email && o.Code == dto.Code && !o.IsUsed)
            .FirstOrDefaultAsync();

        if (otp is null || otp.ExpirationTimeUtc < DateTime.UtcNow)
            return BadRequest("Invalid or expired OTP.");

        otp.IsUsed = true; // single-use
        await _db.SaveChangesAsync();

        return Ok("OTP verified. You can now reset your password.");
    }

    // ===== Reset Password (after Verify) =====
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null) return BadRequest("User not found.");

        // Generate Identity reset token
        var identityToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, identityToken, dto.NewPassword);
        if (!result.Succeeded) return BadRequest(result.Errors);

        return Ok("Password has been reset successfully.");
    }


    // ===== Delete Account =====
    [Authorize]
    [HttpDelete("delete-account")]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized("Invalid token.");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound("User not found.");

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded) return BadRequest(result.Errors);

        return Ok("Account deleted successfully.");
    }

    // ===== Profile =====
    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized("Invalid token.");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound("User not found.");

        var profile = new
        {
            user.FirstName,
            user.LastName,
            user.Email,
            user.MobileNumber,
            user.PhoneNumber,
            user.CompanyName,
            user.CompanyWebsite,
            user.VatNumber,
            user.StreetAddress,
            user.City,
            user.State,
            user.ZipCode,
            user.Country
        };

        return Ok(profile);
    }

    // ===== Debug WhoAmI =====
    [Authorize]
    [HttpGet("whoami")]
    public IActionResult WhoAmI()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);

        return Ok(new { userId, email });
    }

    // ===== Get Countries =====
    [HttpGet("countries")]
    public IActionResult GetCountries()
    {
        return Ok(CountryHelper.Countries);
    }
}
