using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    // Personal Info
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
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
}
