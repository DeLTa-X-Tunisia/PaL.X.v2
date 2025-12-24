using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaL.X.Data;
using PaL.X.Shared.DTOs;
using PaL.X.Shared.Models;
using System.Security.Claims;

namespace PaL.X.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly Services.ServiceManager _serviceManager;

        public ChatController(AppDbContext context, Services.ServiceManager serviceManager)
        {
            _context = context;
            _serviceManager = serviceManager;
        }

        [HttpGet("history/{otherUserId}")]
        public async Task<ActionResult<IEnumerable<ChatMessageDto>>> GetHistory(int otherUserId)
        {
            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserIdStr) || !int.TryParse(currentUserIdStr, out int currentUserId))
            {
                return Unauthorized();
            }

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                            (m.SenderId == otherUserId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.Timestamp)
                .Select(m => new ChatMessageDto
                {
                    MessageId = m.Id,
                    SenderId = m.SenderId,
                    SenderName = m.Sender.Username, 
                    ReceiverId = m.ReceiverId,
                    Content = m.Content,
                    ContentType = m.ContentType,
                    Timestamp = m.Timestamp,
                    IsEdited = m.IsEdited
                })
                .ToListAsync();

            return Ok(messages);
        }

        [HttpDelete("conversation/{otherUserId}")]
        public async Task<ActionResult> DeleteConversation(int otherUserId, [FromQuery] bool localOnly = false)
        {
            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserIdStr) || !int.TryParse(currentUserIdStr, out int currentUserId))
            {
                return Unauthorized();
            }

            IQueryable<Message> query;
            
            if (localOnly)
            {
                // Delete only messages sent by current user or received by current user
                query = _context.Messages
                    .Where(m => (m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                               (m.SenderId == otherUserId && m.ReceiverId == currentUserId));
            }
            else
            {
                // Delete all messages in the conversation (both sides)
                query = _context.Messages
                    .Where(m => (m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                               (m.SenderId == otherUserId && m.ReceiverId == currentUserId));
            }

            var messages = await query.ToListAsync();
            _context.Messages.RemoveRange(messages);
            await _context.SaveChangesAsync();

            return Ok(new { deleted = messages.Count });
        }

        [HttpPut("message/{messageId}")]
        public async Task<ActionResult> EditMessage(int messageId, [FromBody] string newContent)
        {
            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserIdStr) || !int.TryParse(currentUserIdStr, out int currentUserId))
            {
                return Unauthorized();
            }

            var message = await _context.Messages.FindAsync(messageId);
            if (message == null)
            {
                return NotFound("Message not found");
            }

            // Verify the message belongs to the current user
            if (message.SenderId != currentUserId)
            {
                return Forbid("You can only edit your own messages");
            }

            // Check 48-hour restriction
            var hoursSinceSent = (DateTime.UtcNow - message.Timestamp).TotalHours;
            if (hoursSinceSent > 48)
            {
                return BadRequest("Cannot edit messages older than 48 hours");
            }

            // Update in database
            message.Content = newContent;
            message.IsEdited = true;
            await _context.SaveChangesAsync();

            // Notify both users via SignalR
            await _serviceManager.NotifyMessageEdit(messageId, newContent, message.SenderId, message.ReceiverId);

            return Ok(new { success = true });
        }

        [HttpDelete("message/{messageId}")]
        public async Task<ActionResult> DeleteMessage(int messageId)
        {
            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserIdStr) || !int.TryParse(currentUserIdStr, out int currentUserId))
            {
                return Unauthorized();
            }

            var message = await _context.Messages.FindAsync(messageId);
            if (message == null)
            {
                return NotFound("Message not found");
            }

            // Verify the message belongs to the current user
            if (message.SenderId != currentUserId)
            {
                return Forbid("You can only delete your own messages");
            }

            // Check 48-hour restriction
            var hoursSinceSent = (DateTime.UtcNow - message.Timestamp).TotalHours;
            if (hoursSinceSent > 48)
            {
                return BadRequest("Cannot delete messages older than 48 hours");
            }

            // Store info before deletion
            int senderId = message.SenderId;
            int receiverId = message.ReceiverId;

            // Delete from database
            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            // Notify both users via SignalR
            await _serviceManager.NotifyMessageDeletion(messageId, senderId, receiverId);

            return Ok(new { success = true });
        }
    }
}
