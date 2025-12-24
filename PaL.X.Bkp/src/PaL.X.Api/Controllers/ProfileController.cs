using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using PaL.X.Data;
using PaL.X.Shared.DTOs;
using PaL.X.Shared.Models;
using PaL.X.Api.Services;
using PaL.X.Api.Hubs;
using System.Security.Claims;

namespace PaL.X.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ServiceManager _serviceManager;
        private readonly IHubContext<PaLHub> _hubContext;

        public ProfileController(AppDbContext context, ServiceManager serviceManager, IHubContext<PaLHub> hubContext)
        {
            _context = context;
            _serviceManager = serviceManager;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return Ok(new UserProfileDto { IsComplete = false });
            }

            if (user.Profile == null)
            {
                return Ok(new UserProfileDto
                {
                    Id = userId,
                    Username = user.Username,
                    DisplayedName = user.Username,
                    IsComplete = false,
                    CreatedAt = user.CreatedAt,
                    IsAdmin = user.IsAdmin
                });
            }

            var profile = user.Profile;

            // Fetch current status from the user's active session (same logic as GetUserProfile)
            var activeSession = await _context.Sessions
                .Where(s => s.UserId == userId && s.IsActive)
                .OrderByDescending(s => s.ConnectedAt)
                .FirstOrDefaultAsync();
            var currentStatus = activeSession?.DisplayedStatus ?? PaL.X.Shared.Enums.UserStatus.Offline;

            return Ok(new UserProfileDto
            {
                Id = userId,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                DisplayedName = profile.DisplayedName,
                DateOfBirth = profile.DateOfBirth,
                Gender = profile.Gender,
                Country = profile.Country,
                ProfilePicture = profile.ProfilePicture,
                IsComplete = true,
                CreatedAt = user.CreatedAt,
                IsAdmin = user.IsAdmin,
                Username = user.Username,
                // Live status fields
                CurrentStatus = currentStatus,
                IsOnline = currentStatus != PaL.X.Shared.Enums.UserStatus.Offline,
                // Visibility Settings
                VisibilityFirstName = profile.VisibilityFirstName,
                VisibilityLastName = profile.VisibilityLastName,
                VisibilityDateOfBirth = profile.VisibilityDateOfBirth,
                VisibilityGender = profile.VisibilityGender,
                VisibilityCountry = profile.VisibilityCountry,
                VisibilityProfilePicture = profile.VisibilityProfilePicture
            });
        }

        [HttpGet("{targetUserId}")]
        public async Task<IActionResult> GetUserProfile(int targetUserId)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var targetUser = await _context.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Id == targetUserId);

            if (targetUser == null || targetUser.Profile == null)
            {
                return NotFound("Profil introuvable.");
            }

            var profile = targetUser.Profile;
            bool isMe = currentUserId == targetUserId;
            bool isFriend = false;

            if (!isMe)
            {
                isFriend = await _context.Friendships
                    .AnyAsync(f => f.UserId == currentUserId && f.FriendId == targetUserId && !f.IsBlocked);
            }

            // Helper to check visibility
            bool CanSee(PaL.X.Shared.Enums.VisibilityLevel level)
            {
                if (isMe) return true;
                if (level == PaL.X.Shared.Enums.VisibilityLevel.Public) return true;
                if (level == PaL.X.Shared.Enums.VisibilityLevel.Friends && isFriend) return true;
                return false;
            }

            // Récupérer le statut actuel depuis la session active
            var activeSession = await _context.Sessions
                .Where(s => s.UserId == targetUserId && s.IsActive)
                .OrderByDescending(s => s.ConnectedAt)
                .FirstOrDefaultAsync();
            
            var currentStatus = activeSession?.DisplayedStatus ?? PaL.X.Shared.Enums.UserStatus.Offline;
            
            var dto = new UserProfileDto
            {
                Id = targetUserId,
                DisplayedName = profile.DisplayedName, // Always visible? Or add visibility? Assuming visible.
                IsComplete = true,
                CreatedAt = targetUser.CreatedAt,
                IsAdmin = targetUser.IsAdmin,
                Username = targetUser.Username,
                CurrentStatus = currentStatus,
                IsOnline = currentStatus != PaL.X.Shared.Enums.UserStatus.Offline
            };

            // Apply Visibility Rules
            if (CanSee(profile.VisibilityFirstName)) dto.FirstName = profile.FirstName;
            if (CanSee(profile.VisibilityLastName)) dto.LastName = profile.LastName;
            if (CanSee(profile.VisibilityDateOfBirth)) dto.DateOfBirth = profile.DateOfBirth;
            if (CanSee(profile.VisibilityGender)) dto.Gender = profile.Gender;
            if (CanSee(profile.VisibilityCountry)) dto.Country = profile.Country;
            
            // Profile Picture Logic
            // Public: Everyone sees it.
            // Friends: Only friends see it; others see placeholder (null here, client handles placeholder).
            // Only Me: Only user sees it; others see placeholder.
            if (CanSee(profile.VisibilityProfilePicture))
            {
                dto.ProfilePicture = profile.ProfilePicture;
            }
            else
            {
                dto.ProfilePicture = null; // Client will show placeholder
            }

            return Ok(dto);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllProfiles()
        {
            var profilesData = await _context.UserProfiles
                .Include(p => p.User)
                .Select(p => new 
                {
                    Profile = p,
                    Username = p.User.Username
                })
                .ToListAsync();

            var profiles = profilesData.Select(p => new UserProfileDto
            {
                Id = p.Profile.UserId,
                FirstName = p.Profile.FirstName,
                LastName = p.Profile.LastName,
                DisplayedName = p.Profile.DisplayedName,
                DateOfBirth = p.Profile.DateOfBirth,
                Gender = p.Profile.Gender,
                Country = p.Profile.Country,
                ProfilePicture = p.Profile.ProfilePicture,
                IsComplete = true,
                Username = p.Username,
                IsOnline = _serviceManager.IsUserOnline(p.Username)
            }).ToList();

            return Ok(profiles);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

                // Ensure DateOfBirth is UTC
                if (request.DateOfBirth.Kind != DateTimeKind.Utc)
                {
                    request.DateOfBirth = DateTime.SpecifyKind(request.DateOfBirth, DateTimeKind.Utc);
                }

                if (profile == null)
                {
                    profile = new UserProfile
                    {
                        UserId = userId,
                        FirstName = request.FirstName,
                        LastName = request.LastName,
                        DisplayedName = request.DisplayedName,
                        DateOfBirth = request.DateOfBirth,
                        Gender = request.Gender,
                        Country = request.Country,
                        ProfilePicture = request.ProfilePicture,
                        // Default visibility
                        VisibilityFirstName = request.VisibilityFirstName,
                        VisibilityLastName = request.VisibilityLastName,
                        VisibilityDateOfBirth = request.VisibilityDateOfBirth,
                        VisibilityGender = request.VisibilityGender,
                        VisibilityCountry = request.VisibilityCountry,
                        VisibilityProfilePicture = request.VisibilityProfilePicture
                    };
                    _context.UserProfiles.Add(profile);
                }
                else
                {
                    profile.FirstName = request.FirstName;
                    profile.LastName = request.LastName;
                    profile.DisplayedName = request.DisplayedName;
                    profile.DateOfBirth = request.DateOfBirth;
                    profile.Gender = request.Gender;
                    profile.Country = request.Country;
                    if (request.ProfilePicture != null)
                    {
                        profile.ProfilePicture = request.ProfilePicture;
                    }
                    
                    // Update Visibility
                    profile.VisibilityFirstName = request.VisibilityFirstName;
                    profile.VisibilityLastName = request.VisibilityLastName;
                    profile.VisibilityDateOfBirth = request.VisibilityDateOfBirth;
                    profile.VisibilityGender = request.VisibilityGender;
                    profile.VisibilityCountry = request.VisibilityCountry;
                    profile.VisibilityProfilePicture = request.VisibilityProfilePicture;
                }

                await _context.SaveChangesAsync();

                // Notify all clients that this user has updated their profile
                await _hubContext.Clients.All.SendAsync("ProfileUpdated", userId);

                return Ok(new { Success = true, Message = "Profile updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message} - {ex.InnerException?.Message}");
            }
        }
    }
}
