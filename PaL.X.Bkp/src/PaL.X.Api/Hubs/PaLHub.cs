using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PaL.X.Api.Services;
using PaL.X.Data;
using PaL.X.Shared.DTOs;
using System.Security.Claims;

namespace PaL.X.Api.Hubs
{
    [Authorize]
    public class PaLHub : Hub
    {
        private readonly ServiceManager _serviceManager;
        private readonly AppDbContext _context;

        public PaLHub(ServiceManager serviceManager, AppDbContext context)
        {
            _serviceManager = serviceManager;
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            var user = Context.User;
            if (user != null)
            {
                // Si c'est un admin, on l'ajoute au groupe Admin
                if (user.IsInRole("Admin"))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
                }
                else
                {
                    // C'est un client standard
                    await Groups.AddToGroupAsync(Context.ConnectionId, "Clients");
                    
                    // Broadcaster le statut initial à tous les amis
                    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
                    {
                        var session = await _context.Sessions
                            .Where(s => s.UserId == userId && s.IsActive)
                            .OrderByDescending(s => s.ConnectedAt)
                            .FirstOrDefaultAsync();
                            
                        if (session != null)
                        {
                            // 1. Envoyer les statuts de tous les amis en ligne au nouveau connecté
                            var friendIds = await _context.Friendships
                                .Where(f => (f.UserId == userId || f.FriendId == userId) && !f.IsBlocked)
                                .Select(f => f.UserId == userId ? f.FriendId : f.UserId)
                                .ToListAsync();
                            
                            var onlineFriends = await _context.Sessions
                                .Where(s => friendIds.Contains(s.UserId) && s.IsActive)
                                .ToListAsync();
                            
                            // Envoyer les statuts de tous les amis en ligne (y compris ceux "Hors ligne" mais connectés)
                            foreach (var friendSession in onlineFriends)
                            {
                                await Clients.Caller.SendAsync("UserStatusChanged",
                                    friendSession.UserId,
                                    friendSession.Username,
                                    friendSession.DisplayedStatus.ToString());
                            }
                            
                            // 2. Broadcaster le statut du nouveau connecté à tous les autres clients
                            await Clients.AllExcept(Context.ConnectionId).SendAsync("UserStatusChanged", 
                                session.UserId, 
                                session.Username, 
                                session.DisplayedStatus.ToString());
                        }
                    }
                }
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var user = Context.User;
            if (user != null && !user.IsInRole("Admin"))
            {
                // Récupérer l'ID de l'utilisateur qui se déconnecte
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
                {
                    // Récupérer le nom d'utilisateur depuis la session
                    var session = await _context.Sessions
                        .Where(s => s.UserId == userId && s.IsActive)
                        .FirstOrDefaultAsync();
                    
                    if (session != null)
                    {
                        // Désactiver la session dans la base de données
                        session.IsActive = false;
                        session.DisconnectedAt = DateTime.UtcNow;
                        session.DisplayedStatus = PaL.X.Shared.Enums.UserStatus.Offline;
                        session.RealStatus = PaL.X.Shared.Enums.UserStatus.Offline;
                        await _context.SaveChangesAsync();
                        
                        // Broadcaster le statut Offline à tous les autres clients
                        await Clients.AllExcept(Context.ConnectionId).SendAsync("UserStatusChanged",
                            userId,
                            session.Username,
                            "Offline");
                    }
                }
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendFriendRequest(int targetUserId)
        {
            var user = Context.User;
            if (user != null)
            {
                // We need the sender's ID. 
                // Since we don't have easy access to int UserId from ClaimsPrincipal without parsing,
                // we rely on the ServiceManager to look it up via ConnectionId or we pass it.
                // But passing it is insecure (client can spoof).
                // Better: ServiceManager knows the mapping ConnectionId -> UserId.
                
                // Let's modify ServiceManager.SendFriendRequest to take ConnectionId instead of fromUserId?
                // Or just look it up in ServiceManager.
                
                // Wait, ServiceManager stores ClientInfo which has ConnectionId AND UserId.
                // So we can find the sender by Context.ConnectionId.
                
                await _serviceManager.SendFriendRequestByConnectionId(Context.ConnectionId, targetUserId);
            }
        }

        public async Task RespondToFriendRequest(int requesterId, string responseType, string reason)
        {
            var user = Context.User;
            if (user != null)
            {
                await _serviceManager.SendFriendResponseByConnectionId(Context.ConnectionId, requesterId, responseType, reason);
            }
        }

        public async Task SendPrivateMessage(ChatMessageDto message)
        {
            var sent = await _serviceManager.SendPrivateMessage(message);
            if (!sent)
            {
                return;
            }
        }

        public async Task NotifyTyping(int recipientUserId, bool isTyping)
        {
            await _serviceManager.NotifyTyping(Context.ConnectionId, recipientUserId, isTyping);
        }

        public async Task NotifyConversationDeletion(int recipientUserId)
        {
            await _serviceManager.NotifyConversationDeletion(Context.ConnectionId, recipientUserId);
        }

        public async Task RespondConversationDeletion(int requesterId, bool accepted)
        {
            await _serviceManager.RespondConversationDeletion(Context.ConnectionId, requesterId, accepted);
        }

        public async Task NotifyMessageEdit(int messageId, string newContent, int recipientUserId)
        {
            await _serviceManager.NotifyMessageEdit(messageId, newContent, recipientUserId);
        }

        public async Task NotifyMessageDeletion(int messageId, int recipientUserId)
        {
            await _serviceManager.NotifyMessageDeletion(messageId, recipientUserId);
        }

        // ---- Voice calls ----
        public async Task CallInvite(int targetUserId)
        {
            await _serviceManager.StartCallAsync(Context.ConnectionId, targetUserId);
        }

        public async Task CallAccept(string callId)
        {
            await _serviceManager.AcceptCallAsync(Context.ConnectionId, callId);
        }

        public async Task CallReject(string callId, string reason)
        {
            await _serviceManager.RejectCallAsync(Context.ConnectionId, callId, reason);
        }

        public async Task CallCancel(string callId, string reason)
        {
            await _serviceManager.CancelCallAsync(Context.ConnectionId, callId, reason);
        }

        public async Task CallHangup(string callId, string reason)
        {
            await _serviceManager.EndCallAsync(Context.ConnectionId, callId, reason);
        }

        public async Task SendAudioFrame(string callId, byte[] pcmData)
        {
            await _serviceManager.SendAudioFrameAsync(Context.ConnectionId, callId, pcmData);
        }

        // ---- Video calls ----
        public async Task VideoCallInvite(int targetUserId)
        {
            await _serviceManager.StartVideoCallAsync(Context.ConnectionId, targetUserId);
        }

        public async Task VideoCallAccept(string callId)
        {
            await _serviceManager.AcceptVideoCallAsync(Context.ConnectionId, callId);
        }

        public async Task VideoCallReject(string callId, string reason)
        {
            await _serviceManager.RejectVideoCallAsync(Context.ConnectionId, callId, reason);
        }

        public async Task VideoCallCancel(string callId, string reason)
        {
            await _serviceManager.CancelVideoCallAsync(Context.ConnectionId, callId, reason);
        }

        public async Task VideoCallHangup(string callId, string reason)
        {
            await _serviceManager.EndVideoCallAsync(Context.ConnectionId, callId, reason);
        }

        public async Task SendVideoAudioFrame(string callId, byte[] pcmData)
        {
            await _serviceManager.SendVideoAudioFrameAsync(Context.ConnectionId, callId, pcmData);
        }

        public async Task SendVideoFrame(string callId, byte[] frameBytes)
        {
            await _serviceManager.SendVideoFrameAsync(Context.ConnectionId, callId, frameBytes);
        }

        public async Task VideoRtcSendSignal(string callId, string signalType, string payload)
        {
            await _serviceManager.SendVideoRtcSignalAsync(Context.ConnectionId, callId, signalType, payload);
        }
    }
}
