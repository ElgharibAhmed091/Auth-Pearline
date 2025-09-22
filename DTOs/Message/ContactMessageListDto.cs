namespace AuthAPI.DTOs.Message
{
    public class ContactMessageListDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
