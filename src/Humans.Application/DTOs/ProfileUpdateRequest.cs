using System.ComponentModel.DataAnnotations;

namespace Humans.Application.DTOs;

/// <summary>
/// Request to update a member profile.
/// </summary>
public record ProfileUpdateRequest
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
    public required string FirstName { get; init; }

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
    public required string LastName { get; init; }

    /// <summary>
    /// Birthday month (1-12). Year is not stored.
    /// </summary>
    [Range(1, 12)]
    public int? BirthdayMonth { get; init; }

    /// <summary>
    /// Birthday day (1-31). Year is not stored.
    /// </summary>
    [Range(1, 31)]
    public int? BirthdayDay { get; init; }

    [StringLength(256, ErrorMessage = "Address line 1 cannot exceed 256 characters")]
    public string? AddressLine1 { get; init; }

    [StringLength(256, ErrorMessage = "Address line 2 cannot exceed 256 characters")]
    public string? AddressLine2 { get; init; }

    [StringLength(100, ErrorMessage = "City cannot exceed 100 characters")]
    public string? City { get; init; }

    [StringLength(20, ErrorMessage = "Postal code cannot exceed 20 characters")]
    public string? PostalCode { get; init; }

    [StringLength(2, ErrorMessage = "Country code must be 2 characters")]
    [RegularExpression("^[A-Z]{2}$", ErrorMessage = "Country code must be a valid ISO 3166-1 alpha-2 code")]
    public string? CountryCode { get; init; }

    [StringLength(1000, ErrorMessage = "Bio cannot exceed 1000 characters")]
    public string? Bio { get; init; }
}
