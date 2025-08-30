using AuthAPI.Data;
using AuthAPI.Models;
using AuthAPI.Services;
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
        var existing = await _userManager.FindByEmailAsync(dto.Email);
        if (existing is not null) return BadRequest("Email already registered.");

        var user = new ApplicationUser { UserName = dto.Email, Email = dto.Email };
        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded) return BadRequest(result.Errors);

        return Ok("User registered successfully.");
    }

    // ===== Login (JWT) =====
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
            new(ClaimTypes.NameIdentifier, user.Id)
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

        await _emailService.SendEmailAsync(dto.Email, "Reset Password OTP", $"Your OTP code is <b>{otp}</b>. It expires in 5 minutes.");

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
}
