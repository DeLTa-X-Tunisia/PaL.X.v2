using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaL.X.Api.Models;
using PaL.X.Api.Services;
using PaL.X.Shared.DTOs;

namespace PaL.X.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ServiceManager _serviceManager;

        public AdminController(ServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        [HttpGet("status")]
        public IActionResult GetServiceStatus()
        {
            var status = _serviceManager.GetStatus();
            return Ok(new
            {
                Success = true,
                Status = status
            });
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartService()
        {
            var result = await _serviceManager.StartService();
            return Ok(new
            {
                Success = result,
                Message = result ? "Service démarré avec succès" : "Le service est déjà en cours d'exécution",
                Status = _serviceManager.GetStatus()
            });
        }

        [HttpPost("stop")]
        public async Task<IActionResult> StopService()
        {
            var result = await _serviceManager.StopService();
            return Ok(new
            {
                Success = result,
                Message = result ? "Service arrêté avec succès" : "Le service n'est pas en cours d'exécution",
                Status = _serviceManager.GetStatus()
            });
        }

        [HttpGet("clients")]
        public IActionResult GetConnectedClients()
        {
            var status = _serviceManager.GetStatus();
            return Ok(new
            {
                Success = true,
                Clients = status.Clients,
                Count = status.ConnectedClients
            });
        }

        [HttpPost("disconnect-all")]
        public IActionResult DisconnectAllClients()
        {
            _serviceManager.ClearAllClients();
            return Ok(new
            {
                Success = true,
                Message = "Tous les clients ont été déconnectés"
            });
        }

        [HttpPost("disconnect-client/{connectionId}")]
        public async Task<IActionResult> DisconnectClient(string connectionId)
        {
            var result = await _serviceManager.RemoveClient(connectionId);
            return Ok(new
            {
                Success = result,
                Message = result ? "Client déconnecté avec succès" : "Client introuvable"
            });
        }

        [HttpPost("chat/start")]
        public IActionResult StartPublicChat()
        {
            _serviceManager.SetPublicChatStatus(true);
            return Ok(new
            {
                Success = true,
                Message = "Chat public activé",
                Status = _serviceManager.GetStatus()
            });
        }

        [HttpPost("chat/stop")]
        public IActionResult StopPublicChat()
        {
            _serviceManager.SetPublicChatStatus(false);
            return Ok(new
            {
                Success = true,
                Message = "Chat public désactivé",
                Status = _serviceManager.GetStatus()
            });
        }
    }
}