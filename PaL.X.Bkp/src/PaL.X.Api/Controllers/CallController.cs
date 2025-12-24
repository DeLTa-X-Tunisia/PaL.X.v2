using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaL.X.Api.Services;
using PaL.X.Shared.DTOs;
using System.Security.Claims;

namespace PaL.X.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CallController : ControllerBase
    {
        private readonly ServiceManager _serviceManager;

        public CallController(ServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        [HttpGet("history/{peerId}")]
        public async Task<ActionResult<IEnumerable<CallLogDto>>> GetHistory(int peerId)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
            {
                return Unauthorized();
            }

            var history = await _serviceManager.GetCallHistoryAsync(userId, peerId);
            return Ok(history);
        }
    }
}