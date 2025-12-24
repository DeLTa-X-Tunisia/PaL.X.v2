using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PaL.X.Api.Hubs;
using PaL.X.Data;
using PaL.X.Shared.DTOs;
using PaL.X.Shared.Enums;
using PaL.X.Shared.Models;
using System.Security.Claims;

namespace PaL.X.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SessionController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SessionController> _logger;
        private readonly IHubContext<PaLHub> _hubContext;

        public SessionController(AppDbContext context, ILogger<SessionController> logger, IHubContext<PaLHub> hubContext)
        {
            _context = context;
            _logger = logger;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Get current user's active session
        /// </summary>
        [HttpGet("current")]
        public async Task<ActionResult<SessionDto>> GetCurrentSession()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            var session = await _context.Sessions
                .Where(s => s.UserId == userId && s.IsActive)
                .OrderByDescending(s => s.ConnectedAt)
                .FirstOrDefaultAsync();

            if (session == null)
            {
                return NotFound("No active session found");
            }

            return Ok(MapToDto(session));
        }

        /// <summary>
        /// Update current user's status
        /// </summary>
        [HttpPut("status")]
        public async Task<ActionResult> UpdateStatus([FromBody] UpdateStatusRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            var session = await _context.Sessions
                .Where(s => s.UserId == userId && s.IsActive)
                .OrderByDescending(s => s.ConnectedAt)
                .FirstOrDefaultAsync();

            if (session == null)
            {
                return NotFound("No active session found");
            }

            session.DisplayedStatus = request.NewStatus;
            session.LastActivity = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"User {userId} changed status to {request.NewStatus}");

            // Broadcast status change via SignalR to all connected clients
            await _hubContext.Clients.All.SendAsync("UserStatusChanged", userId, session.Username, request.NewStatus.ToString());

            return Ok(new { message = "Status updated successfully", newStatus = request.NewStatus });
        }

        /// <summary>
        /// Update last activity timestamp (heartbeat)
        /// </summary>
        [HttpPost("heartbeat")]
        public async Task<ActionResult> UpdateLastActivity()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            var session = await _context.Sessions
                .Where(s => s.UserId == userId && s.IsActive)
                .OrderByDescending(s => s.ConnectedAt)
                .FirstOrDefaultAsync();

            if (session == null)
            {
                return NotFound("No active session found");
            }

            session.LastActivity = DateTime.UtcNow;
            
            // Auto-switch from Away to Online if user was away
            if (session.DisplayedStatus == UserStatus.Away)
            {
                session.DisplayedStatus = UserStatus.Online;
                _logger.LogInformation($"User {userId} returned from Away status");
                // TODO: Broadcast status change
            }

            await _context.SaveChangesAsync();

            return Ok();
        }

        /// <summary>
        /// Get all sessions for current user (history)
        /// </summary>
        [HttpGet("history")]
        public async Task<ActionResult<List<SessionDto>>> GetSessionHistory()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            var sessions = await _context.Sessions
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.ConnectedAt)
                .Take(50) // Last 50 sessions
                .ToListAsync();

            return Ok(sessions.Select(MapToDto).ToList());
        }

        /// <summary>
        /// End current session (logout)
        /// </summary>
        [HttpPost("end")]
        public async Task<ActionResult> EndSession()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            var session = await _context.Sessions
                .Where(s => s.UserId == userId && s.IsActive)
                .OrderByDescending(s => s.ConnectedAt)
                .FirstOrDefaultAsync();

            if (session != null)
            {
                session.IsActive = false;
                session.DisconnectedAt = DateTime.UtcNow;
                session.DisplayedStatus = UserStatus.Offline;
                session.RealStatus = UserStatus.Offline;
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Session ended for user {userId}");
            }

            return Ok();
        }

        /// <summary>
        /// Get all online users with their current statuses
        /// </summary>
        [HttpGet("online-users")]
        public async Task<ActionResult<Dictionary<int, string>>> GetOnlineUsers()
        {
            // Récupérer uniquement la session la plus récente pour chaque utilisateur
            var onlineUsers = await _context.Sessions
                .Where(s => s.IsActive)
                .GroupBy(s => s.UserId)
                .Select(g => g.OrderByDescending(s => s.ConnectedAt).First())
                .Select(s => new { s.UserId, Status = s.DisplayedStatus.ToString() })
                .ToDictionaryAsync(s => s.UserId, s => s.Status);

            return Ok(onlineUsers);
        }

        private static SessionDto MapToDto(Session session)
        {
            return new SessionDto
            {
                Id = session.Id,
                UserId = session.UserId,
                Username = session.Username,
                IpAddress = session.IpAddress,
                Country = session.Country,
                DeviceSerial = session.DeviceSerial,
                ConnectedAt = session.ConnectedAt,
                LastActivity = session.LastActivity,
                DisplayedStatus = session.DisplayedStatus,
                RealStatus = session.RealStatus,
                IsActive = session.IsActive
            };
        }
    }
}
