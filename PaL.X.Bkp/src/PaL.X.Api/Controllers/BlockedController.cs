using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaL.X.Api.Models;
using PaL.X.Api.Services;
using PaL.X.Data;
using PaL.X.Shared.DTOs;
using PaL.X.Shared.Models;
using System.Security.Claims;

namespace PaL.X.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class BlockedController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ServiceManager _serviceManager;

        public BlockedController(AppDbContext context, ServiceManager serviceManager)
        {
            _context = context;
            _serviceManager = serviceManager;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BlockedUserDto>>> GetBlockedUsers()
        {
            var blockerId = GetCurrentUserId();
            if (blockerId == 0)
            {
                return Unauthorized();
            }

            var now = DateTime.UtcNow;

            var blockedUsers = await _context.BlockedUsers
                .AsNoTracking()
                .Where(b => b.BlockedByUserId == blockerId)
                .ToListAsync();

            var activeBlocks = blockedUsers
                .Where(b => b.IsPermanent || !b.BlockedUntil.HasValue || b.BlockedUntil.Value > now)
                .OrderByDescending(b => b.BlockedOn)
                .Select(b => new BlockedUserDto
                {
                    Id = b.Id,
                    UserId = b.UserId,
                    BlockedByUserId = b.BlockedByUserId,
                    BlockedOn = b.BlockedOn,
                    BlockedUntil = b.BlockedUntil,
                    DurationDays = b.DurationDays,
                    IsPermanent = b.IsPermanent,
                    Reason = b.Reason,
                    Gender = b.Gender,
                    FirstName = b.FirstName,
                    LastName = b.LastName,
                    Status = b.Status
                })
                .ToList();

            return Ok(activeBlocks);
        }

        [HttpPost("{targetUserId}")]
        public async Task<ActionResult<BlockedUserDto>> BlockUser(int targetUserId, [FromBody] BlockUserRequest request)
        {
            var blockerId = GetCurrentUserId();
            if (blockerId == 0)
            {
                return Unauthorized();
            }

            if (blockerId == targetUserId)
            {
                return BadRequest("Vous ne pouvez pas vous bloquer vous-même.");
            }

            var now = DateTime.UtcNow;
            DateTime? blockedUntil = null;
            int? durationDays = null;

            if (!request.IsPermanent)
            {
                if (request.BlockedUntil.HasValue)
                {
                    blockedUntil = request.BlockedUntil.Value.Kind == DateTimeKind.Utc
                        ? request.BlockedUntil.Value
                        : request.BlockedUntil.Value.ToUniversalTime();

                    if (blockedUntil <= now)
                    {
                        return BadRequest("La date de fin du blocage doit être dans le futur.");
                    }

                    durationDays = request.DurationDays ?? (int)Math.Ceiling((blockedUntil.Value - now).TotalDays);
                }
                else if (request.DurationDays.HasValue)
                {
                    if (request.DurationDays.Value <= 0)
                    {
                        return BadRequest("La durée doit être positive.");
                    }
                    durationDays = request.DurationDays.Value;
                    blockedUntil = now.AddDays(durationDays.Value);
                }
                else
                {
                    return BadRequest("Veuillez fournir une durée ou une date de fin pour un blocage non permanent.");
                }
            }

            var blocker = await _context.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Id == blockerId);
            if (blocker == null)
            {
                return Unauthorized();
            }

            var targetUser = await _context.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Id == targetUserId);
            if (targetUser == null)
            {
                return NotFound("Utilisateur à bloquer introuvable.");
            }

            var targetSession = await _context.Sessions
                .AsNoTracking()
                .Where(s => s.UserId == targetUserId && s.IsActive)
                .OrderByDescending(s => s.ConnectedAt)
                .FirstOrDefaultAsync();
            var targetStatus = targetSession?.DisplayedStatus.ToString() ?? "Offline";

            var gender = targetUser.Profile?.Gender ?? "Autre";
            var firstName = targetUser.Profile?.FirstName ?? string.Empty;
            var lastName = targetUser.Profile?.LastName ?? targetUser.Username;

            var existingBlock = await _context.BlockedUsers
                .FirstOrDefaultAsync(b => b.BlockedByUserId == blockerId && b.UserId == targetUserId);

            BlockedUser entry;
            if (existingBlock != null)
            {
                existingBlock.BlockedOn = now;
                existingBlock.BlockedUntil = request.IsPermanent ? null : blockedUntil;
                existingBlock.DurationDays = request.IsPermanent ? null : durationDays;
                existingBlock.IsPermanent = request.IsPermanent;
                existingBlock.Reason = (request.Reason ?? string.Empty).Trim();
                existingBlock.Gender = gender;
                existingBlock.FirstName = firstName;
                existingBlock.LastName = lastName;
                existingBlock.Status = targetStatus;

                entry = existingBlock;
            }
            else
            {
                entry = new BlockedUser
                {
                    BlockedByUserId = blockerId,
                    UserId = targetUserId,
                    BlockedOn = now,
                    BlockedUntil = request.IsPermanent ? null : blockedUntil,
                    DurationDays = request.IsPermanent ? null : durationDays,
                    IsPermanent = request.IsPermanent,
                    Reason = (request.Reason ?? string.Empty).Trim(),
                    Gender = gender,
                    FirstName = firstName,
                    LastName = lastName,
                    Status = targetStatus
                };

                _context.BlockedUsers.Add(entry);
            }

            var friendships = await _context.Friendships
                .Where(f => (f.UserId == blockerId && f.FriendId == targetUserId) || (f.UserId == targetUserId && f.FriendId == blockerId))
                .ToListAsync();
            foreach (var friendship in friendships)
            {
                friendship.IsBlocked = true;
            }

            var blockerDisplayName = GetDisplayName(blocker);
            var blockMessage = _serviceManager.BuildBlockNotificationMessage(blockerDisplayName, entry);

            _context.UserSanctionHistory.Add(new UserSanctionHistory
            {
                UserId = targetUserId,
                BlockedByUserId = blockerId,
                ActionType = "BLOCK",
                Message = blockMessage
            });

            await _context.SaveChangesAsync();

            await _serviceManager.NotifyBlockedAsync(blockerId, targetUserId, blockMessage, entry.Reason, entry.IsPermanent, entry.BlockedUntil);

            var dto = new BlockedUserDto
            {
                Id = entry.Id,
                UserId = entry.UserId,
                BlockedByUserId = entry.BlockedByUserId,
                BlockedOn = entry.BlockedOn,
                BlockedUntil = entry.BlockedUntil,
                DurationDays = entry.DurationDays,
                IsPermanent = entry.IsPermanent,
                Reason = entry.Reason,
                Gender = entry.Gender,
                FirstName = entry.FirstName,
                LastName = entry.LastName,
                Status = entry.Status
            };

            return Ok(dto);
        }

        [HttpDelete("{targetUserId}")]
        public async Task<IActionResult> UnblockUser(int targetUserId)
        {
            var blockerId = GetCurrentUserId();
            if (blockerId == 0)
            {
                return Unauthorized();
            }

            var entry = await _context.BlockedUsers
                .FirstOrDefaultAsync(b => b.BlockedByUserId == blockerId && b.UserId == targetUserId);

            if (entry == null)
            {
                return NotFound("L'utilisateur n'est pas bloqué.");
            }

            var blocker = await _context.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Id == blockerId);

            if (blocker == null)
            {
                return Unauthorized();
            }

            var blockerDisplayName = GetDisplayName(blocker);
            var unblockMessage = _serviceManager.BuildUnblockNotificationMessage(blockerDisplayName);

            _context.BlockedUsers.Remove(entry);

            var friendships = await _context.Friendships
                .Where(f => (f.UserId == blockerId && f.FriendId == targetUserId) || (f.UserId == targetUserId && f.FriendId == blockerId))
                .ToListAsync();
            foreach (var friendship in friendships)
            {
                friendship.IsBlocked = false;
            }

            _context.UserSanctionHistory.Add(new UserSanctionHistory
            {
                UserId = targetUserId,
                BlockedByUserId = blockerId,
                ActionType = "UNBLOCK",
                Message = unblockMessage
            });

            await _context.SaveChangesAsync();

            await _serviceManager.NotifyUnblockedAsync(blockerId, targetUserId, unblockMessage);

            return NoContent();
        }

        private int GetCurrentUserId()
        {
            var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdValue, out var userId) ? userId : 0;
        }

        private static string GetDisplayName(PaL.X.Shared.Models.User user)
        {
            if (user.Profile != null)
            {
                if (!string.IsNullOrWhiteSpace(user.Profile.DisplayedName))
                {
                    return user.Profile.DisplayedName;
                }

                var composed = $"{user.Profile.LastName} {user.Profile.FirstName}".Trim();
                if (!string.IsNullOrWhiteSpace(composed))
                {
                    return composed;
                }
            }

            return user.Username;
        }
    }
}
