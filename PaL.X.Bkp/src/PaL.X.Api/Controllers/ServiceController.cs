using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaL.X.Api.Services;
using System.Security.Claims;

namespace PaL.X.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceController : ControllerBase
    {
        private readonly ServiceManager _serviceManager;

        public ServiceController(ServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        [HttpGet("check")]
        [AllowAnonymous]
        public IActionResult CheckServiceStatus([FromQuery] string? connectionId = null)
        {
            var canConnect = _serviceManager.CanClientConnect();
            var isClientValid = true;

            if (canConnect && !string.IsNullOrEmpty(connectionId))
            {
                isClientValid = _serviceManager.IsClientConnected(connectionId);
            }

            return Ok(new
            {
                ServiceAvailable = canConnect,
                ClientValid = isClientValid,
                Message = canConnect ? "Service disponible" : "Service indisponible",
                Timestamp = DateTime.UtcNow
            });
        }

        [HttpPost("connect")]
        [Authorize]
        public async Task<IActionResult> RegisterConnection([FromBody] ConnectRequest? request = null)
        {
            // Si le client envoie son ID SignalR, on l'utilise. Sinon on génère un GUID (comportement legacy)
            var connectionId = !string.IsNullOrEmpty(request?.SignalRConnectionId) 
                ? request.SignalRConnectionId 
                : Guid.NewGuid().ToString();

            var username = User.Identity?.Name;
            var email = User.FindFirst("email")?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value ?? "";
            var firstName = User.FindFirst("given_name")?.Value ?? User.FindFirst(ClaimTypes.GivenName)?.Value ?? "";
            var lastName = User.FindFirst("family_name")?.Value ?? User.FindFirst(ClaimTypes.Surname)?.Value ?? "";
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0";
            int.TryParse(userIdStr, out int userId);
            var role = User.IsInRole("Admin") ? "Admin" : "User";
            
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var connected = await _serviceManager.AddClient(connectionId, userId, username, email, role, firstName, lastName);
            
            return Ok(new
            {
                Success = connected,
                ConnectionId = connectionId,
                Message = connected ? "Connecté au service" : "Impossible de se connecter au service"
            });
        }

        [HttpPost("disconnect")]
        [Authorize]
        public async Task<IActionResult> UnregisterConnection([FromBody] DisconnectRequest request)
        {
            await _serviceManager.RemoveClient(request.ConnectionId);
            return Ok(new
            {
                Success = true,
                Message = "Déconnecté du service"
            });
        }
    }

    public class ConnectRequest
    {
        public string? SignalRConnectionId { get; set; }
    }

    public class DisconnectRequest
    {
        public string ConnectionId { get; set; } = string.Empty;
    }
}