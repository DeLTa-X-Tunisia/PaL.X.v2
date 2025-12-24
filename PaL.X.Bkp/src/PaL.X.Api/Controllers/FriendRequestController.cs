using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PaL.X.Api.Hubs;
using PaL.X.Api.Services;
using PaL.X.Data;
using PaL.X.Shared.Models;
using System.Security.Claims;

namespace PaL.X.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FriendRequestController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<PaLHub> _hubContext;
        private readonly ServiceManager _serviceManager;

        public FriendRequestController(AppDbContext context, IHubContext<PaLHub> hubContext, ServiceManager serviceManager)
        {
            _context = context;
            _hubContext = hubContext;
            _serviceManager = serviceManager;
        }

        [HttpPost("send/{toUserId}")]
        public async Task<IActionResult> SendRequest(int toUserId)
        {
            var senderId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (senderId == 0) return Unauthorized();

            if (senderId == toUserId) return BadRequest("Vous ne pouvez pas vous ajouter vous-même.");

            var now = DateTime.UtcNow;
            var isBlocked = await _context.BlockedUsers
                .AsNoTracking()
                .AnyAsync(b =>
                    (b.BlockedByUserId == toUserId && b.UserId == senderId ||
                     b.BlockedByUserId == senderId && b.UserId == toUserId) &&
                    (b.IsPermanent || !b.BlockedUntil.HasValue || b.BlockedUntil.Value > now));

            if (isBlocked)
            {
                return BadRequest("Impossible d'envoyer une demande : un blocage est actif entre ces utilisateurs.");
            }

            // Check if request already exists
            var existingRequest = await _context.FriendRequests
                .FirstOrDefaultAsync(r => r.SenderId == senderId && r.ReceiverId == toUserId && r.Status == "Pending");

            if (existingRequest != null) return BadRequest("Une demande est déjà en attente.");

            // Check if already friends
            var alreadyFriends = await _context.Friendships
                .AnyAsync(f => f.UserId == senderId && f.FriendId == toUserId);
            
            if (alreadyFriends) return BadRequest("Vous êtes déjà amis.");

            var request = new FriendRequest
            {
                SenderId = senderId,
                ReceiverId = toUserId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.FriendRequests.Add(request);
            await _context.SaveChangesAsync();

            // Notify Receiver if online
            await _serviceManager.SendFriendRequest(senderId, toUserId);

            return Ok(new { Message = "Demande envoyée avec succès." });
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var requests = await _context.FriendRequests
                .Include(r => r.Sender)
                .ThenInclude(u => u.Profile)
                .Where(r => r.ReceiverId == userId && r.Status == "Pending")
                .Select(r => new 
                {
                    r.Id,
                    r.SenderId,
                    SenderName = r.Sender.Username, // Or Profile Name
                    r.CreatedAt
                })
                .ToListAsync();

            return Ok(requests);
        }

        [HttpPost("respond/{requestId}")]
        public async Task<IActionResult> RespondToRequest(int requestId, [FromBody] RequestResponseDto response)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var request = await _context.FriendRequests
                .Include(r => r.Sender)
                .FirstOrDefaultAsync(r => r.Id == requestId && r.ReceiverId == userId);

            if (request == null) return NotFound("Demande introuvable.");
            if (request.Status != "Pending") return BadRequest("Cette demande a déjà été traitée.");

            request.Status = response.ResponseType; // Accept, AcceptAdd, Refuse
            request.Reason = response.Reason;
            request.UpdatedAt = DateTime.UtcNow;

            if (response.ResponseType == "Accept" || response.ResponseType == "AcceptAdd")
            {
                // Create Friendship (Receiver -> Sender) - Wait, logic is usually:
                // If A sends to B, and B accepts:
                // A adds B (Friendship: UserId=A, FriendId=B)
                // If B accepts & adds:
                // B adds A (Friendship: UserId=B, FriendId=A)
                
                // Let's stick to the previous logic:
                // A (Sender) adds B (Receiver)
                if (!await _context.Friendships.AnyAsync(f => f.UserId == request.SenderId && f.FriendId == request.ReceiverId))
                {
                    _context.Friendships.Add(new Friendship { UserId = request.SenderId, FriendId = request.ReceiverId, CreatedAt = DateTime.UtcNow });
                }

                if (response.ResponseType == "AcceptAdd")
                {
                    // B (Receiver) adds A (Sender)
                    if (!await _context.Friendships.AnyAsync(f => f.UserId == request.ReceiverId && f.FriendId == request.SenderId))
                    {
                        _context.Friendships.Add(new Friendship { UserId = request.ReceiverId, FriendId = request.SenderId, CreatedAt = DateTime.UtcNow });
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Notify Sender (A)
            await _serviceManager.SendFriendResponse(userId, request.SenderId, response.ResponseType, response.Reason ?? "");

            return Ok(new { Message = "Réponse enregistrée." });
        }
    }

    public class RequestResponseDto
    {
        public string ResponseType { get; set; } = "Refuse";
        public string? Reason { get; set; }
    }
}
