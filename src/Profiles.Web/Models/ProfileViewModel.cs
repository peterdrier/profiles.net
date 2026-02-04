using System.ComponentModel.DataAnnotations;

namespace Profiles.Web.Models;

public class ProfileViewModel
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "Country Code")]
    [StringLength(5)]
    [RegularExpression(@"^\+\d{1,4}$", ErrorMessage = "Please enter a valid country code (e.g., +34, +1)")]
    public string? PhoneCountryCode { get; set; }

    [Display(Name = "Phone Number")]
    [StringLength(20)]
    [RegularExpression(@"^[\d\s\-]+$", ErrorMessage = "Please enter a valid phone number")]
    public string? PhoneNumber { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [Display(Name = "Country")]
    [StringLength(2)]
    public string? CountryCode { get; set; }

    [StringLength(1000)]
    [DataType(DataType.MultilineText)]
    public string? Bio { get; set; }

    public string MembershipStatus { get; set; } = "None";
    public bool HasPendingConsents { get; set; }
    public int PendingConsentCount { get; set; }

    /// <summary>
    /// Gets the formatted phone number with country code.
    /// </summary>
    public string? FormattedPhoneNumber =>
        !string.IsNullOrEmpty(PhoneCountryCode) && !string.IsNullOrEmpty(PhoneNumber)
            ? $"{PhoneCountryCode} {PhoneNumber}"
            : PhoneNumber;
}
