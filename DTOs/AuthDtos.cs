namespace AuthAPI.DTOs
{
    public class AuthDtos
    {
        // Register
        public class RegisterDto
        {
            // Personal Info
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string ConfirmPassword { get; set; } = string.Empty;
            public string MobileNumber { get; set; } = string.Empty;

            // Company Info
            public string CompanyName { get; set; } = string.Empty;
            public string? CompanyWebsite { get; set; }
            public string? VatNumber { get; set; }
            public string StreetAddress { get; set; } = string.Empty;
            public string City { get; set; } = string.Empty;
            public string Country { get; set; } = string.Empty;
            public string? State { get; set; }
            public string ZipCode { get; set; } = string.Empty;

            // Product interests
            public string ProductCategories { get; set; } = string.Empty;
        }

        // Login
        public class LoginDto
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        // Forgot Password
        public class ForgotPasswordDto
        {
            public string Email { get; set; } = string.Empty;
        }

        // Reset Password
        public class ResetPasswordDto
        {
            public string Email { get; set; } = string.Empty;
            public string Token { get; set; } = string.Empty;
            public string NewPassword { get; set; } = string.Empty;
        }
    }
}
