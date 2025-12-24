using System;
using PaL.X.Shared.Enums;

namespace PaL.X.Shared.DTOs
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    public string DisplayedName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public byte[]? ProfilePicture { get; set; }
        public bool IsComplete { get; set; }
        public bool IsOnline { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsPending { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsAdmin { get; set; }
        public UserStatus CurrentStatus { get; set; } = UserStatus.Offline;

        // Visibility Settings (Only populated if IsMe)
        public VisibilityLevel VisibilityFirstName { get; set; }
        public VisibilityLevel VisibilityLastName { get; set; }
        public VisibilityLevel VisibilityDateOfBirth { get; set; }
        public VisibilityLevel VisibilityGender { get; set; }
        public VisibilityLevel VisibilityCountry { get; set; }
        public VisibilityLevel VisibilityProfilePicture { get; set; }
    }

    public class UpdateProfileDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string DisplayedName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public byte[]? ProfilePicture { get; set; }

        public VisibilityLevel VisibilityFirstName { get; set; }
        public VisibilityLevel VisibilityLastName { get; set; }
        public VisibilityLevel VisibilityDateOfBirth { get; set; }
        public VisibilityLevel VisibilityGender { get; set; }
        public VisibilityLevel VisibilityCountry { get; set; }
        public VisibilityLevel VisibilityProfilePicture { get; set; }
    }
}
