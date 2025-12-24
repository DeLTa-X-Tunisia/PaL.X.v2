using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaL.X.Api.Services;
using PaL.X.Data;
using PaL.X.Shared.DTOs;
using PaL.X.Shared.Models;
using System.Security.Claims;

namespace PaL.X.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FriendController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ServiceManager _serviceManager;

        public FriendController(AppDbContext context, ServiceManager serviceManager)
        {
            _context = context;
            _serviceManager = serviceManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetFriends()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var friends = await _context.Friendships
                .Where(f => f.UserId == userId)
                .Include(f => f.Friend)
                .ThenInclude(u => u.Profile)
                .Select(f => new
                {
                    Profile = f.Friend.Profile,
                    Username = f.Friend.Username,
                    FriendUserId = f.FriendId,
                    f.IsBlocked
                })
                .ToListAsync();

            // Précharger les statuts de session actifs (évite double requête par ami)
            var activeSessions = await _context.Sessions
                .Where(s => s.IsActive)
                .GroupBy(s => s.UserId)
                .Select(g => g.OrderByDescending(s => s.ConnectedAt).First())
                .ToDictionaryAsync(s => s.UserId, s => s.DisplayedStatus);

            var friendDtos = friends.Select(f =>
            {
                // Statut issu des sessions actives si disponible
                var statusFromSession = activeSessions.TryGetValue(f.FriendUserId, out var s) ? s : PaL.X.Shared.Enums.UserStatus.Offline;
                var isOnlineFromSession = statusFromSession != PaL.X.Shared.Enums.UserStatus.Offline;
                var isOnlineFromService = _serviceManager.IsUserOnline(f.Username);

                // Privilégier le service temps réel, sinon la session
                var finalIsOnline = isOnlineFromService || isOnlineFromSession;
                var finalStatus = finalIsOnline
                    ? (isOnlineFromService && statusFromSession == PaL.X.Shared.Enums.UserStatus.Offline ? PaL.X.Shared.Enums.UserStatus.Online : statusFromSession)
                    : PaL.X.Shared.Enums.UserStatus.Offline;

                if (f.Profile == null)
                {
                    return new UserProfileDto
                    {
                        Id = f.FriendUserId,
                        DisplayedName = f.Username,
                        Username = f.Username,
                        IsComplete = false,
                        IsOnline = finalIsOnline,
                        CurrentStatus = finalStatus,
                        IsBlocked = f.IsBlocked
                    };
                }

                return new UserProfileDto
                {
                    Id = f.Profile.UserId,
                    FirstName = f.Profile.FirstName,
                    LastName = f.Profile.LastName,
                    DisplayedName = f.Profile.DisplayedName,
                    DateOfBirth = f.Profile.DateOfBirth,
                    Gender = f.Profile.Gender,
                    Country = f.Profile.Country,
                    ProfilePicture = f.Profile.ProfilePicture,
                    IsComplete = true,
                    Username = f.Username,
                    IsOnline = finalIsOnline,
                    CurrentStatus = finalStatus,
                    IsBlocked = f.IsBlocked
                };
            }).ToList();

            return Ok(friendDtos);
        }

        [HttpPost("add/{friendId}")]
        public async Task<IActionResult> AddFriend(int friendId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (userId == friendId)
            {
                return BadRequest("Vous ne pouvez pas vous ajouter vous-même.");
            }

            var exists = await _context.Friendships
                .AnyAsync(f => f.UserId == userId && f.FriendId == friendId);

            if (exists)
            {
                return BadRequest("Cet utilisateur est déjà dans votre liste d'amis.");
            }

            var now = DateTime.UtcNow;
            var blocked = await _context.BlockedUsers
                .AsNoTracking()
                .AnyAsync(b =>
                    (b.BlockedByUserId == friendId && b.UserId == userId ||
                     b.BlockedByUserId == userId && b.UserId == friendId) &&
                    (b.IsPermanent || !b.BlockedUntil.HasValue || b.BlockedUntil.Value > now));

            if (blocked)
            {
                return BadRequest("Impossible d'ajouter cet utilisateur : un blocage est actif.");
            }

            var friendship = new Friendship
            {
                UserId = userId,
                FriendId = friendId
            };

            _context.Friendships.Add(friendship);
            await _context.SaveChangesAsync();

            // Return the friend profile so the client can add it to the list
            var friendProfile = await _context.UserProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == friendId);

            if (friendProfile == null) return NotFound("Profil introuvable.");

            // Statut depuis session active si présent
            var activeSession = await _context.Sessions
                .Where(s => s.UserId == friendId && s.IsActive)
                .OrderByDescending(s => s.ConnectedAt)
                .FirstOrDefaultAsync();

            var statusFromSession = activeSession?.DisplayedStatus ?? PaL.X.Shared.Enums.UserStatus.Offline;
            var isOnlineFromSession = statusFromSession != PaL.X.Shared.Enums.UserStatus.Offline;
            var isOnlineFromService = _serviceManager.IsUserOnline(friendProfile.User.Username);

            var finalIsOnline = isOnlineFromService || isOnlineFromSession;
            var finalStatus = finalIsOnline
                ? (isOnlineFromService && statusFromSession == PaL.X.Shared.Enums.UserStatus.Offline ? PaL.X.Shared.Enums.UserStatus.Online : statusFromSession)
                : PaL.X.Shared.Enums.UserStatus.Offline;

            var dto = new UserProfileDto
            {
                Id = friendProfile.UserId,
                FirstName = friendProfile.FirstName,
                LastName = friendProfile.LastName,
                DisplayedName = friendProfile.DisplayedName,
                DateOfBirth = friendProfile.DateOfBirth,
                Gender = friendProfile.Gender,
                Country = friendProfile.Country,
                ProfilePicture = friendProfile.ProfilePicture,
                IsComplete = true,
                Username = friendProfile.User.Username,
                IsOnline = finalIsOnline,
                CurrentStatus = finalStatus,
                IsBlocked = false
            };

            return Ok(dto);
        }

        [HttpDelete("remove/{friendId}")]
        public async Task<IActionResult> RemoveFriend(int friendId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => f.UserId == userId && f.FriendId == friendId);

            if (friendship == null)
            {
                return NotFound("Ami non trouvé.");
            }

            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
