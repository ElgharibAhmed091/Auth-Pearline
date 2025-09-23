namespace AuthAPI.Models
{
    public class OtpCode
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTime ExpirationTimeUtc { get; set; }
        public bool IsUsed { get; set; } = false;
    }
}
