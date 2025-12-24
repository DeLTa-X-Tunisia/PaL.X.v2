using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PaL.X.Shared.Enums;

namespace PaL.X.Shared.Models
{
    public class UserProfile
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;
        public VisibilityLevel VisibilityFirstName { get; set; } = VisibilityLevel.Public;

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;
        public VisibilityLevel VisibilityLastName { get; set; } = VisibilityLevel.Public;

        [Required]
        [MaxLength(50)]
        public string DisplayedName { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }
        public VisibilityLevel VisibilityDateOfBirth { get; set; } = VisibilityLevel.Public;

        [Required]
        [MaxLength(20)]
        public string Gender { get; set; } = string.Empty;
        public VisibilityLevel VisibilityGender { get; set; } = VisibilityLevel.Public;

        [Required]
        [MaxLength(50)]
        public string Country { get; set; } = string.Empty;
        public VisibilityLevel VisibilityCountry { get; set; } = VisibilityLevel.Public;

        public byte[]? ProfilePicture { get; set; }
        public VisibilityLevel VisibilityProfilePicture { get; set; } = VisibilityLevel.Public;
    }
}
