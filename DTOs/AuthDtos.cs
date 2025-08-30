namespace AuthAPI.DTOs
{
    public class AuthDtos
    {
        public class RegisterDto
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class LoginDto
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class ForgotPasswordDto
        {
            public string Email { get; set; } = string.Empty;
        }

        public class VerifyOtpDto
        {
            public string Email { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;
        }

        public class ResetPasswordDto
        {
            public string Email { get; set; } = string.Empty;
            public string NewPassword { get; set; } = string.Empty;
        }

    }
}
