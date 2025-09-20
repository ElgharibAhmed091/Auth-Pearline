namespace AuthAPI.DTOs
{
    public class ChangeEmailDto
    {
        public string CurrentPassword { get; set; }
        public string NewEmail { get; set; }
    }
}
