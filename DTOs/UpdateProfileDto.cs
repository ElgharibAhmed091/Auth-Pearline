namespace AuthAPI.DTOs
{
    public class UpdateProfileDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MobileNumber { get; set; }
        public string CompanyName { get; set; }
        public string? CompanyWebsite { get; set; }
        public string? VatNumber { get; set; }
        public string StreetAddress { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string? State { get; set; }
        public string ZipCode { get; set; }
    }
}
