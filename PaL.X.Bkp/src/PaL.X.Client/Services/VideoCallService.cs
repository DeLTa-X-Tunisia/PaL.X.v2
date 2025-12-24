using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using PaL.X.Shared.DTOs;

namespace PaL.X.Client.Services
{
    public class VideoCallService : IDisposable
    {
        private readonly HubConnection _hubConnection;
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;
        private readonly int _currentUserId;

        private string? _activeCallId;
        private int _activePeerId;

        public event Action<CallInviteDto>? IncomingCall;
        public event Action<CallInviteDto>? OutgoingCall;
        public event Action<CallAcceptDto>? CallAccepted;
        public event Action<CallRejectDto>? CallRejected;
        public event Action<CallEndDto>? CallEnded;

        public event Action<VideoRtcSignalDto>? RtcSignalReceived;

        public VideoCallService(HubConnection hubConnection, HttpClient httpClient, string apiBaseUrl, int currentUserId)
        {
            _hubConnection = hubConnection;
            _httpClient = httpClient;
            _apiBaseUrl = apiBaseUrl.TrimEnd('/');
            _currentUserId = currentUserId;

            RegisterHandlers();
        }

        private void RegisterHandlers()
        {
            _hubConnection.On<CallInviteDto>("VideoCallIncoming", dto =>
            {
                _activeCallId = dto.CallId;
                _activePeerId = dto.FromUserId;
                IncomingCall?.Invoke(dto);
            });

            _hubConnection.On<CallInviteDto>("VideoCallOutgoing", dto =>
            {
                _activeCallId = dto.CallId;
                _activePeerId = dto.ToUserId;
                OutgoingCall?.Invoke(dto);
            });

            _hubConnection.On<CallAcceptDto>("VideoCallAccepted", dto =>
            {
                _activeCallId = dto.CallId;
                _activePeerId = dto.FromUserId == _currentUserId ? dto.ToUserId : dto.FromUserId;
                CallAccepted?.Invoke(dto);
            });

            _hubConnection.On<CallRejectDto>("VideoCallRejected", dto =>
            {
                CallRejected?.Invoke(dto);
                _activeCallId = null;
            });

            _hubConnection.On<CallEndDto>("VideoCallEnded", dto =>
            {
                CallEnded?.Invoke(dto);
                _activeCallId = null;
            });

            _hubConnection.On<VideoRtcSignalDto>("VideoRtcSignal", dto =>
            {
                if (dto == null) return;
                if (_activeCallId != null && !string.Equals(_activeCallId, dto.CallId, StringComparison.OrdinalIgnoreCase))
                {
                    // ignore unrelated signals
                    return;
                }

                RtcSignalReceived?.Invoke(dto);
            });
        }

        public Task InviteAsync(int peerUserId) => _hubConnection.InvokeAsync("VideoCallInvite", peerUserId);

        public Task AcceptAsync(string callId)
        {
            _activeCallId = callId;
            return _hubConnection.InvokeAsync("VideoCallAccept", callId);
        }

        public Task RejectAsync(string callId, string reason = "refused")
        {
            _activeCallId = callId;
            return _hubConnection.InvokeAsync("VideoCallReject", callId, reason);
        }

        public Task CancelAsync(string callId, string reason = "cancelled")
        {
            _activeCallId = null;
            return _hubConnection.InvokeAsync("VideoCallCancel", callId, reason);
        }

        public Task HangupAsync(string callId, string reason = "hangup")
        {
            _activeCallId = null;
            return _hubConnection.InvokeAsync("VideoCallHangup", callId, reason);
        }

        public Task SendRtcSignalAsync(string callId, string signalType, string payload)
        {
            return _hubConnection.InvokeAsync("VideoRtcSendSignal", callId, signalType, payload);
        }

        public void Dispose()
        {
            // No unmanaged resources here. HubConnection is owned by MainForm.
        }
    }
}
