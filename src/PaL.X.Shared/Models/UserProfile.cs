using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaL.X.Shared.Models;

[Table("UserProfiles")]
public class UserProfile
{
    [Key]
    [ForeignKey("User")]
    public int UserId { get; set; }
    
    public virtual User User { get; set; } = null!;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    public VisibilityLevel EmailVisibility { get; set; } = VisibilityLevel.MeOnly;

    [Required]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    public string LastName { get; set; } = string.Empty;
    public VisibilityLevel NameVisibility { get; set; } = VisibilityLevel.Public;

    [Required]
    public string Gender { get; set; } = "Non spécifié";
    public VisibilityLevel GenderVisibility { get; set; } = VisibilityLevel.Public;

    [Required]
    public DateTime BirthDate { get; set; }
    public VisibilityLevel BirthDateVisibility { get; set; } = VisibilityLevel.Friends;

    [Required]
    public string Country { get; set; } = string.Empty;
    public VisibilityLevel CountryVisibility { get; set; } = VisibilityLevel.Public;

    public string? PhoneNumber { get; set; }
    public VisibilityLevel PhoneNumberVisibility { get; set; } = VisibilityLevel.Friends;

    public string? ProfilePictureUrl { get; set; }
    public VisibilityLevel ProfilePictureVisibility { get; set; } = VisibilityLevel.Public;

    public bool IsComplete()
    {
        return !string.IsNullOrWhiteSpace(Email) &&
               !string.IsNullOrWhiteSpace(FirstName) &&
               !string.IsNullOrWhiteSpace(LastName) &&
               !string.IsNullOrWhiteSpace(Gender) &&
               !string.IsNullOrWhiteSpace(Country) &&
               BirthDate != default;
    }
}
