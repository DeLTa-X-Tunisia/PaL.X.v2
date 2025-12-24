using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PaL.X.Data;
using PaL.X.Shared.Enums;
using PaL.X.Shared.Models;

namespace PaL.X.Server.Hubs;

public class ChatHub : Hub
{
    private readonly PalContext _context;

    public ChatHub(PalContext context)
    {
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var username = httpContext?.Request.Query["username"].ToString();
        var userIdStr = httpContext?.Request.Query["userId"].ToString();

        if (!string.IsNullOrEmpty(username) && int.TryParse(userIdStr, out int userId))
        {
            var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
            
            // Create new session
            var session = new Session
            {
                UserId = userId,
                Username = username,
                IpAddress = ipAddress,
                ConnectedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow,
                IsActive = true,
                RealStatus = UserStatus.Online,
                DisplayedStatus = UserStatus.Online
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            // Store session ID in connection items for retrieval on disconnect
            Context.Items["SessionId"] = session.Id;
            Context.Items["UserId"] = userId;

            // Notify others
            await Clients.All.SendAsync("UserStatusChanged", userId, (int)UserStatus.Online);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.Items.TryGetValue("SessionId", out var sessionIdObj) && sessionIdObj is int sessionId)
        {
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session != null)
            {
                session.IsActive = false;
                session.DisconnectedAt = DateTime.UtcNow;
                session.RealStatus = UserStatus.Offline;
                await _context.SaveChangesAsync();

                if (Context.Items.TryGetValue("UserId", out var userIdObj) && userIdObj is int userId)
                {
                    await Clients.All.SendAsync("UserStatusChanged", userId, (int)UserStatus.Offline);
                }
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }

    public async Task UpdateStatus(int userId, int status)
    {
        // Update active session
        if (Context.Items.TryGetValue("SessionId", out var sessionIdObj) && sessionIdObj is int sessionId)
        {
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session != null)
            {
                session.DisplayedStatus = (UserStatus)status;
                session.LastActivity = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        // Update User table as well for persistence
        var userEntity = await _context.Users.FindAsync(userId);
        if (userEntity != null)
        {
            userEntity.Status = (UserStatus)status;
            await _context.SaveChangesAsync();
        }

        await Clients.All.SendAsync("UserStatusChanged", userId, status);
    }
}
