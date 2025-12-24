using System.Collections.Concurrent;
using System.IO;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PaL.X.Api.Hubs;
using PaL.X.Api.Models;
using PaL.X.Data;
using PaL.X.Shared.Models;
using PaL.X.Shared.DTOs;
using System.Text.Json;

namespace PaL.X.Api.Services
{
    public class ServiceManager
    {
        private readonly IHubContext<PaLHub> _hubContext;
        private readonly IServiceScopeFactory _scopeFactory;
        private bool _isServiceRunning = false;
        private bool _isPublicChatEnabled = true; // Par défaut, le chat public est activé
        private DateTime? _serviceStartedAt = null;
        
        // Stockage des clients connectés
        private readonly ConcurrentDictionary<string, ClientInfo> _connectedClients = new();

    // Gestion des appels (in-memory)
    private readonly ConcurrentDictionary<string, CallSession> _activeCalls = new();

        public ServiceManager(IHubContext<PaLHub> hubContext, IServiceScopeFactory scopeFactory)
        {
            _hubContext = hubContext;
            _scopeFactory = scopeFactory;
        }

        public bool IsPublicChatEnabled => _isPublicChatEnabled;

        public ServiceStatus GetStatus()
        {
            return new ServiceStatus
            {
                IsRunning = _isServiceRunning,
                IsPublicChatEnabled = _isPublicChatEnabled, // Ajouter cette propriété au DTO ServiceStatus
                StartedAt = _serviceStartedAt,
                ConnectedClients = _connectedClients.Count,
                Clients = _connectedClients.Values.ToList()
            };
        }

        public async Task<bool> StartService()
        {
            if (!_isServiceRunning)
            {
                _isServiceRunning = true;
                _serviceStartedAt = DateTime.UtcNow;
                await _hubContext.Clients.All.SendAsync("ServiceStatusChanged", true);
                return true;
            }
            return false;
        }

        public async Task<bool> StopService()
        {
            if (_isServiceRunning)
            {
                _isServiceRunning = false;
                await _hubContext.Clients.All.SendAsync("ServiceStatusChanged", false);
                ClearAllClients(); // This will notify disconnects
                return true;
            }
            return false;
        }

        public void SetPublicChatStatus(bool isEnabled)
        {
            _isPublicChatEnabled = isEnabled;
        }

        public async Task<bool> AddClient(string connectionId, int userId, string username, string email, string role, string firstName = "", string lastName = "")
        {
            if (!_isServiceRunning)
                return false;

            var clientInfo = new ClientInfo
            {
                UserId = userId,
                Username = username,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Role = role,
                ConnectionId = connectionId,
                ConnectedAt = DateTime.UtcNow
            };

            if (_connectedClients.TryAdd(connectionId, clientInfo))
            {
                await _hubContext.Clients.Group("Admins").SendAsync("ClientListUpdated");
                await NotifyFriendsStatusChange(userId, true);
                
                // Check for pending conversation deletion notifications
                await CheckPendingDeletionNotifications(userId, connectionId);
                await NotifyActiveBlocksOnConnect(userId, connectionId);
                
                return true;
            }
            return false;
        }

        private async Task CheckPendingDeletionNotifications(int userId, string connectionId)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                // Find pending deletion requests for this user
                var pendingRequests = await dbContext.PendingConversationDeletions
                    .Where(p => p.RecipientId == userId && !p.IsNotified)
                    .ToListAsync();

                foreach (var request in pendingRequests)
                {
                    // Get requester info
                    var requester = await dbContext.Users
                        .Include(u => u.Profile)
                        .FirstOrDefaultAsync(u => u.Id == request.RequesterId);
                    
                    if (requester != null)
                    {
                        string displayName = !string.IsNullOrWhiteSpace(requester.Profile?.FirstName)
                            ? $"{requester.Profile.FirstName} {requester.Profile.LastName}".Trim()
                            : requester.Username;

                        // Send notification
                        await _hubContext.Clients.Client(connectionId)
                            .SendAsync("ConversationDeletionRequest", request.RequesterId, displayName);

                        // Mark as notified
                        request.IsNotified = true;
                    }
                }

                await dbContext.SaveChangesAsync();
            }
        }

        private static bool IsBlockActive(BlockedUser block)
        {
            if (!block.IsPermanent && block.BlockedUntil.HasValue && block.BlockedUntil.Value <= DateTime.UtcNow)
            {
                return false;
            }

            return true;
        }

        private async Task<string> GetUserDisplayNameAsync(AppDbContext context, int userId)
        {
            var user = await context.Users
                .AsNoTracking()
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return $"Utilisateur {userId}";
            }

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

        public string BuildBlockNotificationMessage(string blockerDisplayName, BlockedUser block)
        {
            var reasonPart = string.IsNullOrWhiteSpace(block.Reason)
                ? string.Empty
                : $" Raison : {block.Reason}.";

            string periodPart;
            if (block.IsPermanent)
            {
                periodPart = " BLOCK PERMANANT.";
            }
            else if (block.BlockedUntil.HasValue)
            {
                var start = block.BlockedOn.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
                var end = block.BlockedUntil.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
                var duration = block.DurationDays.HasValue ? $" Durée : {block.DurationDays.Value} jour(s)." : string.Empty;
                periodPart = $" Période : du {start} au {end}.{duration}";
            }
            else
            {
                periodPart = string.Empty;
            }

            return $"{blockerDisplayName} vous a bloqué.{reasonPart}{periodPart}".Trim();
        }

        public string BuildUnblockNotificationMessage(string blockerDisplayName)
        {
            return $"{blockerDisplayName} vous a débloqué. Vous pouvez à nouveau écrire.";
        }

        private string BuildBlockReminderMessage(string blockerDisplayName, BlockedUser block)
        {
            var baseMessage = BuildBlockNotificationMessage(blockerDisplayName, block);
            return $"Envoi impossible : {baseMessage}";
        }

        private async Task NotifyMessageAttemptBlocked(int senderUserId, int blockerUserId, string message, bool isPermanent, DateTime? blockedUntil, string reason)
        {
            var senderConnections = _connectedClients.Values
                .Where(c => c.UserId == senderUserId)
                .Select(c => c.ConnectionId)
                .ToList();

            if (senderConnections.Any())
            {
                await _hubContext.Clients.Clients(senderConnections)
                    .SendAsync("SanctionNotification", new
                    {
                        NotificationType = "BlockReminder",
                        Severity = "warning",
                        BlockedByUserId = blockerUserId,
                        Message = message,
                        Reason = reason,
                        IsPermanent = isPermanent,
                        BlockedUntil = blockedUntil
                    });

                await _hubContext.Clients.Clients(senderConnections)
                    .SendAsync("BlockedStateChanged", blockerUserId, true);
            }
        }

        private async Task NotifyBlockStateAsync(int blockerUserId, int blockedUserId, bool isBlocked, string severity, string message, string reason, bool isPermanent, DateTime? blockedUntil)
        {
            var blockedConnections = _connectedClients.Values
                .Where(c => c.UserId == blockedUserId)
                .Select(c => c.ConnectionId)
                .ToList();

            if (blockedConnections.Any())
            {
                await _hubContext.Clients.Clients(blockedConnections)
                    .SendAsync("SanctionNotification", new
                    {
                        NotificationType = isBlocked ? "Block" : "Unblock",
                        Severity = severity,
                        BlockedByUserId = blockerUserId,
                        Message = message,
                        Reason = reason,
                        IsPermanent = isPermanent,
                        BlockedUntil = blockedUntil
                    });

                await _hubContext.Clients.Clients(blockedConnections)
                    .SendAsync("BlockedStateChanged", blockerUserId, isBlocked);
            }

            var blockerConnections = _connectedClients.Values
                .Where(c => c.UserId == blockerUserId)
                .Select(c => c.ConnectionId)
                .ToList();

            if (blockerConnections.Any())
            {
                await _hubContext.Clients.Clients(blockerConnections)
                    .SendAsync("BlockedUsersUpdated");

                await _hubContext.Clients.Clients(blockerConnections)
                    .SendAsync("BlockedUserStateChanged", blockedUserId, isBlocked);
            }
        }

        public async Task NotifyBlockedAsync(int blockerUserId, int blockedUserId, string message, string reason, bool isPermanent, DateTime? blockedUntil)
        {
            await NotifyBlockStateAsync(blockerUserId, blockedUserId, true, "critical", message, reason, isPermanent, blockedUntil);
        }

        public async Task NotifyUnblockedAsync(int blockerUserId, int blockedUserId, string message)
        {
            await NotifyBlockStateAsync(blockerUserId, blockedUserId, false, "success", message, string.Empty, false, null);
        }

        private async Task NotifyActiveBlocksOnConnect(int userId, string connectionId)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var blocks = await context.BlockedUsers
                .AsNoTracking()
                .Where(b => b.UserId == userId)
                .ToListAsync();

            foreach (var block in blocks)
            {
                if (!IsBlockActive(block))
                {
                    continue;
                }

                var blockerName = await GetUserDisplayNameAsync(context, block.BlockedByUserId);
                var message = BuildBlockNotificationMessage(blockerName, block);

                await _hubContext.Clients.Client(connectionId)
                    .SendAsync("SanctionNotification", new
                    {
                        NotificationType = "Block",
                        Severity = "critical",
                        BlockedByUserId = block.BlockedByUserId,
                        Message = message,
                        Reason = block.Reason,
                        IsPermanent = block.IsPermanent,
                        BlockedUntil = block.BlockedUntil
                    });

                await _hubContext.Clients.Client(connectionId)
                    .SendAsync("BlockedStateChanged", block.BlockedByUserId, true);
            }
        }

        private async Task<BlockedUser?> GetActiveBlockAsync(AppDbContext context, int blockerUserId, int blockedUserId)
        {
            var block = await context.BlockedUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.BlockedByUserId == blockerUserId && b.UserId == blockedUserId);

            if (block == null)
            {
                return null;
            }

            return IsBlockActive(block) ? block : null;
        }

        public async Task<bool> IsUserBlockedAsync(int blockerUserId, int blockedUserId)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var block = await GetActiveBlockAsync(context, blockerUserId, blockedUserId);
            return block != null;
        }

        public async Task<bool> RemoveClient(string connectionId)
        {
            if (_connectedClients.TryRemove(connectionId, out var clientInfo))
            {
                // Notifier le client spécifique qu'il est déconnecté
                await _hubContext.Clients.Client(connectionId).SendAsync("ForceDisconnect");
                // Notifier les admins
                await _hubContext.Clients.Group("Admins").SendAsync("ClientListUpdated");
                
                if (clientInfo != null)
                {
                    await NotifyFriendsStatusChange(clientInfo.UserId, false);
                }
                return true;
            }
            return false;
        }

        private async Task NotifyFriendsStatusChange(int userId, bool isOnline)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    
                    // Trouver tous les utilisateurs qui ont cet utilisateur comme ami
                    // C'est à dire: Friendship où FriendId == userId
                    var friendUserIds = await dbContext.Friendships
                        .Where(f => f.FriendId == userId && !f.IsBlocked)
                        .Select(f => f.UserId)
                        .ToListAsync();

                    // Pour chaque ami, vérifier s'il est connecté et lui envoyer une notification
                    foreach (var friendId in friendUserIds)
                    {
                        // Trouver les connexions actives de cet ami
                        var activeConnections = _connectedClients.Values
                            .Where(c => c.UserId == friendId)
                            .Select(c => c.ConnectionId)
                            .ToList();

                        if (activeConnections.Any())
                        {
                            await _hubContext.Clients.Clients(activeConnections)
                                .SendAsync("FriendStatusChanged", userId, isOnline);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la notification de changement de statut: {ex.Message}");
            }
        }

        public void ClearAllClients()
        {
            foreach (var client in _connectedClients)
            {
                _hubContext.Clients.Client(client.Key).SendAsync("ForceDisconnect");
            }
            _connectedClients.Clear();
            _hubContext.Clients.Group("Admins").SendAsync("ClientListUpdated");
        }

        public bool CanClientConnect()
        {
            return _isServiceRunning;
        }

        public bool IsClientConnected(string connectionId)
        {
            return _connectedClients.ContainsKey(connectionId);
        }

        public bool IsUserOnline(string username)
        {
            return _connectedClients.Values.Any(c => c.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        }

        public async Task SendFriendRequestByConnectionId(string senderConnectionId, int toUserId)
        {
            var senderClient = _connectedClients.Values.FirstOrDefault(c => c.ConnectionId == senderConnectionId);
            if (senderClient != null)
            {
                await SendFriendRequest(senderClient.UserId, toUserId);
            }
        }

        public async Task SendFriendResponseByConnectionId(string responderConnectionId, int requesterId, string responseType, string reason)
        {
            var responderClient = _connectedClients.Values.FirstOrDefault(c => c.ConnectionId == responderConnectionId);
            if (responderClient != null)
            {
                await SendFriendResponse(responderClient.UserId, requesterId, responseType, reason);
            }
        }

        public async Task SendFriendRequest(int fromUserId, int toUserId)
        {
            var targetClient = _connectedClients.Values.FirstOrDefault(c => c.UserId == toUserId);
            var senderClient = _connectedClients.Values.FirstOrDefault(c => c.UserId == fromUserId);

            if (targetClient != null && senderClient != null)
            {
                await _hubContext.Clients.Client(targetClient.ConnectionId)
                    .SendAsync("ReceiveFriendRequest", senderClient.UserId, senderClient.Username);
            }
        }

        public async Task SendFriendResponse(int responderId, int requesterId, string responseType, string reason)
        {
            var requesterClient = _connectedClients.Values.FirstOrDefault(c => c.UserId == requesterId); // A
            var responderClient = _connectedClients.Values.FirstOrDefault(c => c.UserId == responderId); // B

            // 1. Notify Requester (A) of the response
            if (requesterClient != null && responderClient != null)
            {
                await _hubContext.Clients.Client(requesterClient.ConnectionId)
                    .SendAsync("ReceiveFriendResponse", responderClient.Username, responseType, reason);
            }

            // 2. Refresh Lists (Persistence is handled by Controller)
            if (responseType == "Accept" || responseType == "AcceptAdd")
            {
                // Notify A to refresh list (since they added B)
                if (requesterClient != null)
                {
                    await _hubContext.Clients.Client(requesterClient.ConnectionId).SendAsync("RefreshFriendList");
                }
                
                // If AcceptAdd, Notify B to refresh list too
                if (responseType == "AcceptAdd" && responderClient != null)
                {
                    await _hubContext.Clients.Client(responderClient.ConnectionId).SendAsync("RefreshFriendList");
                }
            }
        }

        public async Task<bool> SendPrivateMessage(ChatMessageDto message)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Check if receiver has blocked the sender
            var block = await GetActiveBlockAsync(dbContext, message.ReceiverId, message.SenderId);
            if (block != null)
            {
                var blockerName = await GetUserDisplayNameAsync(dbContext, message.ReceiverId);
                var reminder = BuildBlockReminderMessage(blockerName, block);
                await NotifyMessageAttemptBlocked(message.SenderId, message.ReceiverId, reminder, block.IsPermanent, block.BlockedUntil, block.Reason);
                return false;
            }

            // Save the message
            var dbMessage = new Message
            {
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
                Content = message.Content,
                ContentType = message.ContentType,
                Timestamp = DateTime.UtcNow,
                IsEdited = false
            };
            dbContext.Messages.Add(dbMessage);
            await dbContext.SaveChangesAsync();

            // Log file-based messages in FileTransfers for traceability
            var normalizedContentType = (message.ContentType ?? string.Empty).Trim().ToLowerInvariant();
            if (IsFileContentType(normalizedContentType) && TryGetFileUrl(message.Content, out var fileUrl))
            {
                var transfer = new FileTransfer
                {
                    MessageId = dbMessage.Id,
                    SenderId = message.SenderId,
                    ReceiverId = message.ReceiverId,
                    ContentType = normalizedContentType,
                    FileName = ResolveFileName(message),
                    FileUrl = fileUrl,
                    FileSizeBytes = message.FileSizeBytes ?? 0,
                    DurationSeconds = message.DurationSeconds,
                    SentAt = dbMessage.Timestamp
                };

                dbContext.FileTransfers.Add(transfer);
                await dbContext.SaveChangesAsync();
            }

            // Update DTO to match DB
            message.MessageId = dbMessage.Id;
            message.Timestamp = dbMessage.Timestamp;
            message.IsEdited = dbMessage.IsEdited;

            // Send to receiver
            var receiverConnections = _connectedClients.Values
                .Where(c => c.UserId == message.ReceiverId)
                .Select(c => c.ConnectionId)
                .ToList();

            if (receiverConnections.Any())
            {
                await _hubContext.Clients.Clients(receiverConnections)
                    .SendAsync("ReceivePrivateMessage", message);
            }

            // Send back to sender with correct MessageId
            var senderConnections = _connectedClients.Values
                .Where(c => c.UserId == message.SenderId)
                .Select(c => c.ConnectionId)
                .ToList();

            if (senderConnections.Any())
            {
                await _hubContext.Clients.Clients(senderConnections)
                    .SendAsync("MessageSaved", message);
            }

            return true;
        }

        private static bool IsFileContentType(string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType)) return false;

            return contentType.Equals("file", StringComparison.OrdinalIgnoreCase)
                   || contentType.Equals("image", StringComparison.OrdinalIgnoreCase)
                   || contentType.Equals("video", StringComparison.OrdinalIgnoreCase)
                   || contentType.Equals("audio", StringComparison.OrdinalIgnoreCase)
                   || contentType.Equals("voice", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryGetFileUrl(string content, out string url)
        {
            url = string.Empty;
            if (string.IsNullOrWhiteSpace(content)) return false;

            if (Uri.TryCreate(content, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                url = uri.ToString();
                return true;
            }

            return false;
        }

        private static string ResolveFileName(ChatMessageDto message)
        {
            if (!string.IsNullOrWhiteSpace(message.FileName))
            {
                return message.FileName;
            }

            if (!string.IsNullOrWhiteSpace(message.Content))
            {
                try
                {
                    if (Uri.TryCreate(message.Content, UriKind.Absolute, out var uri))
                    {
                        var last = Path.GetFileName(uri.AbsolutePath);
                        if (!string.IsNullOrWhiteSpace(last)) return last;
                    }
                    else
                    {
                        var last = Path.GetFileName(message.Content);
                        if (!string.IsNullOrWhiteSpace(last)) return last;
                    }
                }
                catch { }
            }

            return "file";
        }

        public async Task NotifyMessageEdit(int messageId, string newContent, int senderId, int receiverId)
        {
            // Notify both sender and receiver
            var userIds = new[] { senderId, receiverId };
            
            foreach (var userId in userIds)
            {
                var connections = _connectedClients.Values
                    .Where(c => c.UserId == userId)
                    .Select(c => c.ConnectionId)
                    .ToList();

                if (connections.Any())
                {
                    await _hubContext.Clients.Clients(connections)
                        .SendAsync("MessageEdited", messageId, newContent);
                }
            }
        }

        public async Task NotifyMessageDeletion(int messageId, int senderId, int receiverId)
        {
            // Notify both sender and receiver
            var userIds = new[] { senderId, receiverId };
            
            foreach (var userId in userIds)
            {
                var connections = _connectedClients.Values
                    .Where(c => c.UserId == userId)
                    .Select(c => c.ConnectionId)
                    .ToList();

                if (connections.Any())
                {
                    await _hubContext.Clients.Clients(connections)
                        .SendAsync("MessageDeleted", messageId);
                }
            }
        }

        public async Task NotifyTyping(string senderConnectionId, int recipientUserId, bool isTyping)
        {
            // Find sender info
            if (!_connectedClients.TryGetValue(senderConnectionId, out var senderInfo))
                return;

            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var block = await GetActiveBlockAsync(context, recipientUserId, senderInfo.UserId);
                if (block != null)
                {
                    var blockerName = await GetUserDisplayNameAsync(context, recipientUserId);
                    var reminder = BuildBlockReminderMessage(blockerName, block);
                    await NotifyMessageAttemptBlocked(senderInfo.UserId, recipientUserId, reminder, block.IsPermanent, block.BlockedUntil, block.Reason);
                    return;
                }
            }

            // Build full name
            string displayName = !string.IsNullOrWhiteSpace(senderInfo.FirstName)
                ? $"{senderInfo.FirstName} {senderInfo.LastName}".Trim()
                : senderInfo.Username;

            // Find receiver connections
            var receiverConnections = _connectedClients.Values
                .Where(c => c.UserId == recipientUserId)
                .Select(c => c.ConnectionId)
                .ToList();

            if (receiverConnections.Any())
            {
                await _hubContext.Clients.Clients(receiverConnections)
                    .SendAsync("UserTyping", senderInfo.UserId, displayName, isTyping);
            }
        }

        public async Task NotifyConversationDeletion(string senderConnectionId, int recipientUserId)
        {
            // Find sender info
            if (!_connectedClients.TryGetValue(senderConnectionId, out var senderInfo))
                return;

            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var block = await GetActiveBlockAsync(context, recipientUserId, senderInfo.UserId);
                if (block != null)
                {
                    var blockerName = await GetUserDisplayNameAsync(context, recipientUserId);
                    var reminder = BuildBlockReminderMessage(blockerName, block);
                    await NotifyMessageAttemptBlocked(senderInfo.UserId, recipientUserId, reminder, block.IsPermanent, block.BlockedUntil, block.Reason);
                    return;
                }
            }

            // Build display name
            string displayName = !string.IsNullOrWhiteSpace(senderInfo.FirstName)
                ? $"{senderInfo.FirstName} {senderInfo.LastName}".Trim()
                : senderInfo.Username;

            // Store pending deletion request in database
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                // Check if already exists
                var existingRequest = await dbContext.PendingConversationDeletions
                    .FirstOrDefaultAsync(p => p.RequesterId == senderInfo.UserId && p.RecipientId == recipientUserId && !p.IsNotified);
                
                if (existingRequest == null)
                {
                    dbContext.PendingConversationDeletions.Add(new PendingConversationDeletion
                    {
                        RequesterId = senderInfo.UserId,
                        RecipientId = recipientUserId,
                        CreatedAt = DateTime.UtcNow,
                        IsNotified = false
                    });
                    await dbContext.SaveChangesAsync();
                }
            }

            // Find receiver connections - if online, notify immediately
            var receiverConnections = _connectedClients.Values
                .Where(c => c.UserId == recipientUserId)
                .Select(c => c.ConnectionId)
                .ToList();

            if (receiverConnections.Any())
            {
                await _hubContext.Clients.Clients(receiverConnections)
                    .SendAsync("ConversationDeletionRequest", senderInfo.UserId, displayName);
                    
                // Mark as notified
                using (var scope = _scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var request = await dbContext.PendingConversationDeletions
                        .FirstOrDefaultAsync(p => p.RequesterId == senderInfo.UserId && p.RecipientId == recipientUserId && !p.IsNotified);
                    if (request != null)
                    {
                        request.IsNotified = true;
                        await dbContext.SaveChangesAsync();
                    }
                }
            }
            // If recipient is offline, the notification stays pending in the database
        }

        public async Task RespondConversationDeletion(string responderConnectionId, int requesterId, bool accepted)
        {
            // Get responder info
            if (!_connectedClients.TryGetValue(responderConnectionId, out var responderInfo))
                return;

            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var block = await GetActiveBlockAsync(context, requesterId, responderInfo.UserId);
                if (block != null)
                {
                    var blockerName = await GetUserDisplayNameAsync(context, requesterId);
                    var reminder = BuildBlockReminderMessage(blockerName, block);
                    await NotifyMessageAttemptBlocked(responderInfo.UserId, requesterId, reminder, block.IsPermanent, block.BlockedUntil, block.Reason);
                    return;
                }
            }

            // Remove the pending deletion request from database
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var pendingRequest = await dbContext.PendingConversationDeletions
                    .FirstOrDefaultAsync(p => p.RequesterId == requesterId && p.RecipientId == responderInfo.UserId);
                
                if (pendingRequest != null)
                {
                    dbContext.PendingConversationDeletions.Remove(pendingRequest);
                    await dbContext.SaveChangesAsync();
                }
            }

            // Find requester connections
            var requesterConnections = _connectedClients.Values
                .Where(c => c.UserId == requesterId)
                .Select(c => c.ConnectionId)
                .ToList();

            if (requesterConnections.Any())
            {
                await _hubContext.Clients.Clients(requesterConnections)
                    .SendAsync("ConversationDeletionResponse", accepted);
            }
        }

        public async Task NotifyMessageEdit(int messageId, string newContent, int recipientUserId)
        {
            // Find recipient connections
            var recipientConnections = _connectedClients.Values
                .Where(c => c.UserId == recipientUserId)
                .Select(c => c.ConnectionId)
                .ToList();

            if (recipientConnections.Any())
            {
                await _hubContext.Clients.Clients(recipientConnections)
                    .SendAsync("MessageEdited", messageId, newContent);
            }
        }

        public async Task NotifyMessageDeletion(int messageId, int recipientUserId)
        {
            // Find recipient connections
            var recipientConnections = _connectedClients.Values
                .Where(c => c.UserId == recipientUserId)
                .Select(c => c.ConnectionId)
                .ToList();

            if (recipientConnections.Any())
            {
                await _hubContext.Clients.Clients(recipientConnections)
                    .SendAsync("MessageDeleted", messageId);
            }
        }

        #region Voice/Video call signaling

        private enum CallKind
        {
            Voice,
            Video
        }

        private bool IsUserInActiveCall(int userId)
        {
            return _activeCalls.Values.Any(c => c.Status != CallState.Ended && (c.CallerId == userId || c.CalleeId == userId));
        }

        private CallKind? GetUserActiveCallKind(int userId)
        {
            foreach (var call in _activeCalls.Values)
            {
                if (call.Status == CallState.Ended)
                {
                    continue;
                }

                if (call.CallerId == userId || call.CalleeId == userId)
                {
                    return call.Kind;
                }
            }

            return null;
        }

        public async Task<CallInviteDto?> StartCallAsync(string callerConnectionId, int targetUserId)
        {
            if (!_isServiceRunning)
            {
                return null;
            }

            if (!_connectedClients.TryGetValue(callerConnectionId, out var caller))
            {
                return null;
            }

            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var block = await GetActiveBlockAsync(context, targetUserId, caller.UserId);
                if (block != null)
                {
                    var blockerName = await GetUserDisplayNameAsync(context, targetUserId);
                    var reminder = BuildBlockReminderMessage(blockerName, block);
                    await NotifyMessageAttemptBlocked(caller.UserId, targetUserId, reminder, block.IsPermanent, block.BlockedUntil, block.Reason);

                    var payload = new CallRejectDto
                    {
                        CallId = Guid.NewGuid().ToString(),
                        FromUserId = targetUserId,
                        ToUserId = caller.UserId,
                        Reason = "Blocked",
                        RejectedAt = DateTime.UtcNow
                    };

                    await _hubContext.Clients.Client(callerConnectionId)
                        .SendAsync("CallRejected", payload);

                    await PersistBlockedAttemptAsync(caller.UserId, targetUserId, payload);
                    return null;
                }
            }

            // Si l'appelant ou le destinataire est déjà en appel, rejeter immédiatement
            if (IsUserInActiveCall(caller.UserId) || IsUserInActiveCall(targetUserId))
            {
                await SendBusyRejectionAsync(caller, targetUserId);
                return null;
            }

            var calleeConnections = _connectedClients.Values
                .Where(c => c.UserId == targetUserId)
                .Select(c => c.ConnectionId)
                .ToList();

            if (!calleeConnections.Any())
            {
                // Destinataire hors ligne : notifier l'appelant
                var offlinePayload = new CallRejectDto
                {
                    CallId = Guid.NewGuid().ToString(),
                    FromUserId = targetUserId,
                    ToUserId = caller.UserId,
                    Reason = "offline",
                    RejectedAt = DateTime.UtcNow
                };

                await _hubContext.Clients.Client(callerConnectionId)
                    .SendAsync("CallRejected", offlinePayload);
                return null;
            }

            var invite = new CallInviteDto
            {
                CallId = Guid.NewGuid().ToString(),
                FromUserId = caller.UserId,
                FromName = string.IsNullOrWhiteSpace(caller.FirstName) ? caller.Username : $"{caller.FirstName} {caller.LastName}".Trim(),
                ToUserId = targetUserId,
                SentAt = DateTime.UtcNow
            };

            var session = new CallSession
            {
                CallId = invite.CallId,
                CallerId = caller.UserId,
                CalleeId = targetUserId,
                Status = CallState.Ringing,
                Kind = CallKind.Voice,
                StartedAt = invite.SentAt
            };

            _activeCalls[session.CallId] = session;

            await _hubContext.Clients.Clients(calleeConnections)
                .SendAsync("CallIncoming", invite);

            // Inform caller that invite has been sent
            await _hubContext.Clients.Client(callerConnectionId)
                .SendAsync("CallOutgoing", invite);

            return invite;
        }

        public async Task<bool> AcceptCallAsync(string calleeConnectionId, string callId)
        {
            if (!_connectedClients.TryGetValue(calleeConnectionId, out var callee))
            {
                return false;
            }

            if (!_activeCalls.TryGetValue(callId, out var call) || call.CalleeId != callee.UserId)
            {
                return false;
            }

            if (call.Status == CallState.Ended)
            {
                return false;
            }

            call.Status = CallState.InProgress;
            call.AcceptedAt = DateTime.UtcNow;
            _activeCalls[callId] = call;

            var payload = new CallAcceptDto
            {
                CallId = callId,
                FromUserId = callee.UserId,
                ToUserId = call.CallerId,
                AcceptedAt = call.AcceptedAt.Value
            };

            var callerConnections = _connectedClients.Values
                .Where(c => c.UserId == call.CallerId)
                .Select(c => c.ConnectionId)
                .ToList();

            if (callerConnections.Any())
            {
                await _hubContext.Clients.Clients(callerConnections)
                    .SendAsync("CallAccepted", payload);
            }

            // Notify callee as well (for state sync)
            await _hubContext.Clients.Client(calleeConnectionId)
                .SendAsync("CallAccepted", payload);

            return true;
        }

        private async Task SendBusyRejectionAsync(ClientInfo caller, int targetUserId)
        {
            var busyKind = GetUserActiveCallKind(targetUserId) ?? GetUserActiveCallKind(caller.UserId);
            var reason = busyKind == CallKind.Video ? "VideoInCall" : "InCall";

            var payload = new CallRejectDto
            {
                CallId = Guid.NewGuid().ToString(),
                FromUserId = targetUserId,
                ToUserId = caller.UserId,
                Reason = reason,
                RejectedAt = DateTime.UtcNow
            };

            var callerConnections = _connectedClients.Values
                .Where(c => c.UserId == caller.UserId)
                .Select(c => c.ConnectionId)
                .ToList();

            if (callerConnections.Any())
            {
                await _hubContext.Clients.Clients(callerConnections)
                    .SendAsync("CallRejected", payload);
            }

            await PersistBusyAttemptAsync(caller.UserId, targetUserId, payload);
        }

        public async Task<bool> RejectCallAsync(string calleeConnectionId, string callId, string reason = "refused")
        {
            if (!_connectedClients.TryGetValue(calleeConnectionId, out var callee))
            {
                return false;
            }

            if (!_activeCalls.TryRemove(callId, out var call) || call.CalleeId != callee.UserId)
            {
                return false;
            }

            call.EndedAt = DateTime.UtcNow;
            call.EndReason = reason;
            call.Status = CallState.Ended;

            var payload = new CallRejectDto
            {
                CallId = callId,
                FromUserId = callee.UserId,
                ToUserId = call.CallerId,
                Reason = reason,
                RejectedAt = call.EndedAt.Value
            };

            var callerConnections = _connectedClients.Values
                .Where(c => c.UserId == call.CallerId)
                .Select(c => c.ConnectionId)
                .ToList();

            if (callerConnections.Any())
            {
                await _hubContext.Clients.Clients(callerConnections)
                    .SendAsync("CallRejected", payload);
            }

            await _hubContext.Clients.Client(calleeConnectionId)
                .SendAsync("CallRejected", payload);

            await PersistCallLogAsync(call, "rejected");
            return true;
        }

        public async Task<bool> CancelCallAsync(string callerConnectionId, string callId, string reason = "cancelled")
        {
            if (!_connectedClients.TryGetValue(callerConnectionId, out var caller))
            {
                return false;
            }

            if (!_activeCalls.TryRemove(callId, out var call) || call.CallerId != caller.UserId)
            {
                return false;
            }

            call.EndedAt = DateTime.UtcNow;
            call.EndReason = reason;
            call.Status = CallState.Ended;

            var payload = new CallEndDto
            {
                CallId = callId,
                FromUserId = caller.UserId,
                ToUserId = call.CalleeId,
                Reason = reason,
                EndedAt = call.EndedAt.Value
            };

            var calleeConnections = _connectedClients.Values
                .Where(c => c.UserId == call.CalleeId)
                .Select(c => c.ConnectionId)
                .ToList();

            if (calleeConnections.Any())
            {
                await _hubContext.Clients.Clients(calleeConnections)
                    .SendAsync("CallEnded", payload);
            }

            await _hubContext.Clients.Client(callerConnectionId)
                .SendAsync("CallEnded", payload);

            await PersistCallLogAsync(call, reason);
            return true;
        }

        public async Task<bool> EndCallAsync(string connectionId, string callId, string reason = "hangup")
        {
            if (!_connectedClients.TryGetValue(connectionId, out var sender))
            {
                return false;
            }

            if (!_activeCalls.TryGetValue(callId, out var call))
            {
                return false;
            }

            if (call.Status == CallState.Ended)
            {
                return false;
            }

            call.EndedAt = DateTime.UtcNow;
            call.EndReason = reason;
            call.Status = CallState.Ended;
            _activeCalls.TryRemove(callId, out _);

            var otherUserId = sender.UserId == call.CallerId ? call.CalleeId : call.CallerId;
            var payload = new CallEndDto
            {
                CallId = callId,
                FromUserId = sender.UserId,
                ToUserId = otherUserId,
                Reason = reason,
                EndedAt = call.EndedAt.Value
            };

            var otherConnections = _connectedClients.Values
                .Where(c => c.UserId == otherUserId)
                .Select(c => c.ConnectionId)
                .ToList();

            if (otherConnections.Any())
            {
                await _hubContext.Clients.Clients(otherConnections)
                    .SendAsync("CallEnded", payload);
            }

            await _hubContext.Clients.Client(connectionId)
                .SendAsync("CallEnded", payload);

            var outcome = reason == "hangup" ? "completed" : reason;
            await PersistCallLogAsync(call, outcome);
            return true;
        }

        public async Task<bool> SendAudioFrameAsync(string senderConnectionId, string callId, byte[] pcmData)
        {
            if (pcmData == null || pcmData.Length == 0 || pcmData.Length > 65536)
            {
                return false;
            }

            if (!_connectedClients.TryGetValue(senderConnectionId, out var sender))
            {
                return false;
            }

            if (!_activeCalls.TryGetValue(callId, out var call) || call.Status != CallState.InProgress || call.Kind != CallKind.Voice)
            {
                return false;
            }

            var targetUserId = sender.UserId == call.CallerId ? call.CalleeId : call.CallerId;
            var targetConnections = _connectedClients.Values
                .Where(c => c.UserId == targetUserId)
                .Select(c => c.ConnectionId)
                .ToList();

            if (!targetConnections.Any())
            {
                return false;
            }

            await _hubContext.Clients.Clients(targetConnections)
                .SendAsync("ReceiveAudioFrame", callId, pcmData);

            return true;
        }

        // ---- Video calls ----
        public async Task<CallInviteDto?> StartVideoCallAsync(string callerConnectionId, int targetUserId)
        {
            if (!_isServiceRunning)
            {
                return null;
            }

            if (!_connectedClients.TryGetValue(callerConnectionId, out var caller))
            {
                return null;
            }

            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var block = await GetActiveBlockAsync(context, targetUserId, caller.UserId);
                if (block != null)
                {
                    var blockerName = await GetUserDisplayNameAsync(context, targetUserId);
                    var reminder = BuildBlockReminderMessage(blockerName, block);
                    await NotifyMessageAttemptBlocked(caller.UserId, targetUserId, reminder, block.IsPermanent, block.BlockedUntil, block.Reason);

                    var payload = new CallRejectDto
                    {
                        CallId = Guid.NewGuid().ToString(),
                        FromUserId = targetUserId,
                        ToUserId = caller.UserId,
                        Reason = "Blocked",
                        RejectedAt = DateTime.UtcNow
                    };

                    await _hubContext.Clients.Client(callerConnectionId)
                        .SendAsync("VideoCallRejected", payload);
                    return null;
                }
            }

            // Bloquer tout autre appel pendant un appel vidéo actif, et bloquer l'appel vidéo si l'un est déjà en cours.
            if (IsUserInActiveCall(caller.UserId) || IsUserInActiveCall(targetUserId))
            {
                await SendVideoBusyRejectionAsync(caller, targetUserId);
                return null;
            }

            var calleeConnections = _connectedClients.Values
                .Where(c => c.UserId == targetUserId)
                .Select(c => c.ConnectionId)
                .ToList();

            if (!calleeConnections.Any())
            {
                var offlinePayload = new CallRejectDto
                {
                    CallId = Guid.NewGuid().ToString(),
                    FromUserId = targetUserId,
                    ToUserId = caller.UserId,
                    Reason = "offline",
                    RejectedAt = DateTime.UtcNow
                };

                await _hubContext.Clients.Client(callerConnectionId)
                    .SendAsync("VideoCallRejected", offlinePayload);
                return null;
            }

            var invite = new CallInviteDto
            {
                CallId = Guid.NewGuid().ToString(),
                FromUserId = caller.UserId,
                FromName = string.IsNullOrWhiteSpace(caller.FirstName) ? caller.Username : $"{caller.FirstName} {caller.LastName}".Trim(),
                ToUserId = targetUserId,
                SentAt = DateTime.UtcNow
            };

            var session = new CallSession
            {
                CallId = invite.CallId,
                CallerId = caller.UserId,
                CalleeId = targetUserId,
                Status = CallState.Ringing,
                Kind = CallKind.Video,
                StartedAt = invite.SentAt
            };

            _activeCalls[session.CallId] = session;

            await _hubContext.Clients.Clients(calleeConnections)
                .SendAsync("VideoCallIncoming", invite);

            await _hubContext.Clients.Client(callerConnectionId)
                .SendAsync("VideoCallOutgoing", invite);

            return invite;
        }

        private async Task SendVideoBusyRejectionAsync(ClientInfo caller, int targetUserId)
        {
            var busyKind = GetUserActiveCallKind(targetUserId) ?? GetUserActiveCallKind(caller.UserId);
            // Pour l'appel vidéo, distinguer l'occupation vidéo du simple "InCall"
            var reason = busyKind == CallKind.Video ? "VideoInCall" : "InCall";

            var payload = new CallRejectDto
            {
                CallId = Guid.NewGuid().ToString(),
                FromUserId = targetUserId,
                ToUserId = caller.UserId,
                Reason = reason,
                RejectedAt = DateTime.UtcNow
            };

            var callerConnections = _connectedClients.Values
                .Where(c => c.UserId == caller.UserId)
                .Select(c => c.ConnectionId)
                .ToList();

            if (callerConnections.Any())
            {
                await _hubContext.Clients.Clients(callerConnections)
                    .SendAsync("VideoCallRejected", payload);
            }
        }

        public async Task<bool> AcceptVideoCallAsync(string calleeConnectionId, string callId)
        {
            if (!_connectedClients.TryGetValue(calleeConnectionId, out var callee))
            {
                return false;
            }

            if (!_activeCalls.TryGetValue(callId, out var call) || call.CalleeId != callee.UserId || call.Kind != CallKind.Video)
            {
                return false;
            }

            if (call.Status == CallState.Ended)
            {
                return false;
            }

            call.Status = CallState.InProgress;
            call.AcceptedAt = DateTime.UtcNow;
            _activeCalls[callId] = call;

            var payload = new CallAcceptDto
            {
                CallId = callId,
                FromUserId = callee.UserId,
                ToUserId = call.CallerId,
                AcceptedAt = call.AcceptedAt.Value
            };

            var callerConnections = _connectedClients.Values
                .Where(c => c.UserId == call.CallerId)
                .Select(c => c.ConnectionId)
                .ToList();

            if (callerConnections.Any())
            {
                await _hubContext.Clients.Clients(callerConnections)
                    .SendAsync("VideoCallAccepted", payload);
            }

            await _hubContext.Clients.Client(calleeConnectionId)
                .SendAsync("VideoCallAccepted", payload);

            return true;
        }

        public async Task<bool> RejectVideoCallAsync(string calleeConnectionId, string callId, string reason = "refused")
        {
            if (!_connectedClients.TryGetValue(calleeConnectionId, out var callee))
            {
                return false;
            }

            if (!_activeCalls.TryRemove(callId, out var call) || call.CalleeId != callee.UserId || call.Kind != CallKind.Video)
            {
                return false;
            }

            call.EndedAt = DateTime.UtcNow;
            call.EndReason = reason;
            call.Status = CallState.Ended;

            var payload = new CallRejectDto
            {
                CallId = callId,
                FromUserId = callee.UserId,
                ToUserId = call.CallerId,
                Reason = reason,
                RejectedAt = call.EndedAt.Value
            };

            var callerConnections = _connectedClients.Values
                .Where(c => c.UserId == call.CallerId)
                .Select(c => c.ConnectionId)
                .ToList();

            if (callerConnections.Any())
            {
                await _hubContext.Clients.Clients(callerConnections)
                    .SendAsync("VideoCallRejected", payload);
            }

            await _hubContext.Clients.Client(calleeConnectionId)
                .SendAsync("VideoCallRejected", payload);
            return true;
        }

        public async Task<bool> CancelVideoCallAsync(string callerConnectionId, string callId, string reason = "cancelled")
        {
            if (!_connectedClients.TryGetValue(callerConnectionId, out var caller))
            {
                return false;
            }

            if (!_activeCalls.TryRemove(callId, out var call) || call.CallerId != caller.UserId || call.Kind != CallKind.Video)
            {
                return false;
            }

            call.EndedAt = DateTime.UtcNow;
            call.EndReason = reason;
            call.Status = CallState.Ended;

            var payload = new CallEndDto
            {
                CallId = callId,
                FromUserId = caller.UserId,
                ToUserId = call.CalleeId,
                Reason = reason,
                EndedAt = call.EndedAt.Value
            };

            var calleeConnections = _connectedClients.Values
                .Where(c => c.UserId == call.CalleeId)
                .Select(c => c.ConnectionId)
                .ToList();

            if (calleeConnections.Any())
            {
                await _hubContext.Clients.Clients(calleeConnections)
                    .SendAsync("VideoCallEnded", payload);
            }

            await _hubContext.Clients.Client(callerConnectionId)
                .SendAsync("VideoCallEnded", payload);
            return true;
        }

        public async Task<bool> EndVideoCallAsync(string connectionId, string callId, string reason = "hangup")
        {
            if (!_connectedClients.TryGetValue(connectionId, out var sender))
            {
                return false;
            }

            if (!_activeCalls.TryGetValue(callId, out var call) || call.Kind != CallKind.Video)
            {
                return false;
            }

            if (call.Status == CallState.Ended)
            {
                return false;
            }

            call.EndedAt = DateTime.UtcNow;
            call.EndReason = reason;
            call.Status = CallState.Ended;
            _activeCalls.TryRemove(callId, out _);

            var otherUserId = sender.UserId == call.CallerId ? call.CalleeId : call.CallerId;
            var payload = new CallEndDto
            {
                CallId = callId,
                FromUserId = sender.UserId,
                ToUserId = otherUserId,
                Reason = reason,
                EndedAt = call.EndedAt.Value
            };

            var otherConnections = _connectedClients.Values
                .Where(c => c.UserId == otherUserId)
                .Select(c => c.ConnectionId)
                .ToList();

            if (otherConnections.Any())
            {
                await _hubContext.Clients.Clients(otherConnections)
                    .SendAsync("VideoCallEnded", payload);
            }

            await _hubContext.Clients.Client(connectionId)
                .SendAsync("VideoCallEnded", payload);
            return true;
        }

        public async Task<bool> SendVideoRtcSignalAsync(string senderConnectionId, string callId, string signalType, string payload)
        {
            if (string.IsNullOrWhiteSpace(callId) || string.IsNullOrWhiteSpace(signalType))
            {
                return false;
            }

            if (!_connectedClients.TryGetValue(senderConnectionId, out var sender))
            {
                return false;
            }

            if (!_activeCalls.TryGetValue(callId, out var call) || call.Kind != CallKind.Video || call.Status == CallState.Ended)
            {
                return false;
            }

            // Sender must be part of the call
            if (sender.UserId != call.CallerId && sender.UserId != call.CalleeId)
            {
                return false;
            }

            var otherUserId = sender.UserId == call.CallerId ? call.CalleeId : call.CallerId;
            var otherConnections = _connectedClients.Values
                .Where(c => c.UserId == otherUserId)
                .Select(c => c.ConnectionId)
                .ToList();

            if (!otherConnections.Any())
            {
                return false;
            }

            var dto = new VideoRtcSignalDto
            {
                CallId = callId,
                FromUserId = sender.UserId,
                SignalType = signalType,
                Payload = payload ?? string.Empty,
                SentAt = DateTime.UtcNow
            };

            await _hubContext.Clients.Clients(otherConnections)
                .SendAsync("VideoRtcSignal", dto);

            return true;
        }

        public async Task<bool> SendVideoAudioFrameAsync(string senderConnectionId, string callId, byte[] pcmData)
        {
            if (pcmData == null || pcmData.Length == 0 || pcmData.Length > 65536)
            {
                return false;
            }

            if (!_connectedClients.TryGetValue(senderConnectionId, out var sender))
            {
                return false;
            }

            if (!_activeCalls.TryGetValue(callId, out var call) || call.Status != CallState.InProgress || call.Kind != CallKind.Video)
            {
                return false;
            }

            var targetUserId = sender.UserId == call.CallerId ? call.CalleeId : call.CallerId;
            var targetConnections = _connectedClients.Values
                .Where(c => c.UserId == targetUserId)
                .Select(c => c.ConnectionId)
                .ToList();

            if (!targetConnections.Any())
            {
                return false;
            }

            await _hubContext.Clients.Clients(targetConnections)
                .SendAsync("ReceiveVideoAudioFrame", callId, pcmData);

            return true;
        }

        public async Task<bool> SendVideoFrameAsync(string senderConnectionId, string callId, byte[] frameBytes)
        {
            if (frameBytes == null || frameBytes.Length == 0 || frameBytes.Length > 400_000)
            {
                return false;
            }

            if (!_connectedClients.TryGetValue(senderConnectionId, out var sender))
            {
                return false;
            }

            if (!_activeCalls.TryGetValue(callId, out var call) || call.Status != CallState.InProgress || call.Kind != CallKind.Video)
            {
                return false;
            }

            var targetUserId = sender.UserId == call.CallerId ? call.CalleeId : call.CallerId;
            var targetConnections = _connectedClients.Values
                .Where(c => c.UserId == targetUserId)
                .Select(c => c.ConnectionId)
                .ToList();

            if (!targetConnections.Any())
            {
                return false;
            }

            await _hubContext.Clients.Clients(targetConnections)
                .SendAsync("ReceiveVideoFrame", callId, frameBytes);

            return true;
        }

        public async Task<List<CallLogDto>> GetCallHistoryAsync(int userId, int peerId)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var messages = await dbContext.Messages
                .Where(m => m.ContentType == "call" &&
                            ((m.SenderId == userId && m.ReceiverId == peerId) ||
                             (m.SenderId == peerId && m.ReceiverId == userId)))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            var result = new List<CallLogDto>();
            foreach (var msg in messages)
            {
                try
                {
                    var dto = JsonSerializer.Deserialize<CallLogDto>(msg.Content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (dto != null)
                    {
                        result.Add(dto);
                    }
                }
                catch
                {
                    // ignore malformed
                }
            }

            return result;
        }

        private async Task PersistCallLogAsync(CallSession call, string outcome)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var log = new CallLogDto
            {
                CallId = call.CallId,
                CallerId = call.CallerId,
                CalleeId = call.CalleeId,
                StartedAt = call.StartedAt,
                AcceptedAt = call.AcceptedAt,
                EndedAt = call.EndedAt ?? DateTime.UtcNow,
                Result = outcome,
                EndReason = call.EndReason
            };

            var message = new Message
            {
                SenderId = call.CallerId,
                ReceiverId = call.CalleeId,
                Content = JsonSerializer.Serialize(log),
                ContentType = "call",
                Timestamp = log.EndedAt
            };

            dbContext.Messages.Add(message);
            await dbContext.SaveChangesAsync();
        }

        private async Task PersistBusyAttemptAsync(int callerId, int calleeId, CallRejectDto payload)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var log = new CallLogDto
            {
                CallId = payload.CallId,
                CallerId = callerId,
                CalleeId = calleeId,
                StartedAt = payload.RejectedAt,
                EndedAt = payload.RejectedAt,
                Result = "busy",
                EndReason = payload.Reason
            };

            var message = new Message
            {
                SenderId = callerId,
                ReceiverId = calleeId,
                Content = JsonSerializer.Serialize(log),
                ContentType = "call",
                Timestamp = payload.RejectedAt
            };

            dbContext.Messages.Add(message);
            await dbContext.SaveChangesAsync();
        }

        private async Task PersistBlockedAttemptAsync(int callerId, int calleeId, CallRejectDto payload)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var log = new CallLogDto
            {
                CallId = payload.CallId,
                CallerId = callerId,
                CalleeId = calleeId,
                StartedAt = payload.RejectedAt,
                EndedAt = payload.RejectedAt,
                Result = "blocked",
                EndReason = payload.Reason
            };

            // Trace interne : on journalise sous forme de message de type "call" (non diffusé côté callee)
            var message = new Message
            {
                SenderId = callerId,
                ReceiverId = calleeId,
                Content = JsonSerializer.Serialize(log),
                ContentType = "call",
                Timestamp = payload.RejectedAt
            };

            dbContext.Messages.Add(message);
            await dbContext.SaveChangesAsync();
        }

        private enum CallState
        {
            Ringing,
            InProgress,
            Ended
        }

        private class CallSession
        {
            public string CallId { get; set; } = string.Empty;
            public int CallerId { get; set; }
            public int CalleeId { get; set; }
            public CallState Status { get; set; }
            public CallKind Kind { get; set; }
            public DateTime StartedAt { get; set; }
            public DateTime? AcceptedAt { get; set; }
            public DateTime? EndedAt { get; set; }
            public string? EndReason { get; set; }
        }

        #endregion
    }
}