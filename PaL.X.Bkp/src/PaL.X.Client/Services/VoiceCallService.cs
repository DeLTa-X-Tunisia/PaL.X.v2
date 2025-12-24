using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using NAudio.Wave;
using PaL.X.Shared.DTOs;

namespace PaL.X.Client.Services
{
    public class VoiceCallService : IDisposable
    {
        private readonly HubConnection _hubConnection;
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;
        private readonly int _currentUserId;

        private WaveInEvent? _mic;
        private WaveOutEvent? _speaker;
        private BufferedWaveProvider? _playbackBuffer;
        private string? _activeCallId;
        private int _activePeerId;
        private bool _muted;

    private EventHandler<WaveInEventArgs>? _micHandler;

        private readonly WaveFormat _format = new WaveFormat(16000, 16, 1);

        public event Action<CallInviteDto>? IncomingCall;
        public event Action<CallInviteDto>? OutgoingCall;
        public event Action<CallAcceptDto>? CallAccepted;
        public event Action<CallRejectDto>? CallRejected;
        public event Action<CallEndDto>? CallEnded;
        public event Action<string, byte[]>? AudioFrameReceived;

        public VoiceCallService(HubConnection hubConnection, HttpClient httpClient, string apiBaseUrl, int currentUserId)
        {
            _hubConnection = hubConnection;
            _httpClient = httpClient;
            _apiBaseUrl = apiBaseUrl.TrimEnd('/');
            _currentUserId = currentUserId;

            RegisterHandlers();
        }

        private void RegisterHandlers()
        {
            _hubConnection.On<CallInviteDto>("CallIncoming", dto =>
            {
                _activeCallId = dto.CallId;
                _activePeerId = dto.FromUserId;
                IncomingCall?.Invoke(dto);
            });

            _hubConnection.On<CallInviteDto>("CallOutgoing", dto =>
            {
                _activeCallId = dto.CallId;
                _activePeerId = dto.ToUserId;
                OutgoingCall?.Invoke(dto);
            });

            _hubConnection.On<CallAcceptDto>("CallAccepted", dto =>
            {
                _activeCallId = dto.CallId;
                _activePeerId = dto.FromUserId == _currentUserId ? dto.ToUserId : dto.FromUserId;
                StartAudio();
                CallAccepted?.Invoke(dto);
            });

            _hubConnection.On<CallRejectDto>("CallRejected", dto =>
            {
                CallRejected?.Invoke(dto);
                StopAudio();
                _activeCallId = null;
            });

            _hubConnection.On<CallEndDto>("CallEnded", dto =>
            {
                CallEnded?.Invoke(dto);
                StopAudio();
                _activeCallId = null;
            });

            _hubConnection.On<string, byte[]>("ReceiveAudioFrame", (callId, pcm) =>
            {
                if (_activeCallId != callId || pcm == null || pcm.Length == 0)
                    return;

                AudioFrameReceived?.Invoke(callId, pcm);
                if (_playbackBuffer != null)
                {
                    _playbackBuffer.AddSamples(pcm, 0, pcm.Length);
                }
            });
        }

        public Task InviteAsync(int peerUserId)
        {
            return _hubConnection.InvokeAsync("CallInvite", peerUserId);
        }

        public Task AcceptAsync(string callId)
        {
            _activeCallId = callId;
            return _hubConnection.InvokeAsync("CallAccept", callId);
        }

        public Task RejectAsync(string callId, string reason = "refused")
        {
            _activeCallId = callId;
            StopAudio();
            return _hubConnection.InvokeAsync("CallReject", callId, reason);
        }

        public Task CancelAsync(string callId, string reason = "cancelled")
        {
            StopAudio();
            _activeCallId = null;
            return _hubConnection.InvokeAsync("CallCancel", callId, reason);
        }

        public Task HangupAsync(string callId, string reason = "hangup")
        {
            StopAudio();
            _activeCallId = null;
            return _hubConnection.InvokeAsync("CallHangup", callId, reason);
        }

        public Task<List<CallLogDto>?> GetHistoryAsync(int peerUserId)
        {
            return _httpClient.GetFromJsonAsync<List<CallLogDto>>($"{_apiBaseUrl}/call/history/{peerUserId}");
        }

        public void SetMuted(bool muted)
        {
            _muted = muted;
        }

        public void SetVolumePercent(int percent)
        {
            if (_speaker != null)
            {
                var v = Math.Clamp(percent / 100f, 0f, 1f);
                _speaker.Volume = v;
            }
        }

        private void StartAudio()
        {
            StopAudio();

            _playbackBuffer = new BufferedWaveProvider(_format)
            {
                BufferDuration = TimeSpan.FromSeconds(5),
                DiscardOnBufferOverflow = true
            };

            _speaker = new WaveOutEvent();
            _speaker.Init(_playbackBuffer);
            _speaker.Play();

            _mic = new WaveInEvent
            {
                WaveFormat = _format,
                BufferMilliseconds = 20
            };
            _micHandler = async (_, e) =>
            {
                if (_muted || _activeCallId == null)
                {
                    return;
                }

                var data = new byte[e.BytesRecorded];
                Buffer.BlockCopy(e.Buffer, 0, data, 0, e.BytesRecorded);
                try
                {
                    await _hubConnection.SendAsync("SendAudioFrame", _activeCallId, data);
                }
                catch
                {
                    // Ignore transient send errors
                }
            };

            _mic.DataAvailable += _micHandler;

            _mic.StartRecording();
        }

        private void StopAudio()
        {
            if (_mic != null)
            {
                if (_micHandler != null)
                {
                    _mic.DataAvailable -= _micHandler;
                }
                _mic.StopRecording();
                _mic.Dispose();
                _mic = null;
                _micHandler = null;
            }

            if (_speaker != null)
            {
                _speaker.Stop();
                _speaker.Dispose();
                _speaker = null;
            }

            _playbackBuffer = null;
        }

        public void Dispose()
        {
            StopAudio();
        }
    }
}