using PaL.X.Shared.DTOs;
using PaL.X.Shared.Enums;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Threading;

namespace PaL.X.Client.Services
{
    public class UserActivityMonitor : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly System.Threading.Timer _inactivityTimer;
        private readonly System.Threading.Timer _heartbeatTimer;
        private DateTime _lastActivity;
        private UserStatus _currentStatus;
        private bool _isAway;
        
        private const int INACTIVITY_MINUTES = 15;
        private const int HEARTBEAT_SECONDS = 30; // Send heartbeat every 30 seconds

        // Windows API for detecting user input
        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        public event EventHandler<UserStatus>? StatusChanged;

        public UserActivityMonitor(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _lastActivity = DateTime.Now;
            _currentStatus = UserStatus.Online;
            _isAway = false;

            // Timer to check for inactivity every 30 seconds
            _inactivityTimer = new System.Threading.Timer(CheckInactivity, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
            
            // Timer to send heartbeat every 30 seconds
            _heartbeatTimer = new System.Threading.Timer(SendHeartbeat, null, TimeSpan.FromSeconds(HEARTBEAT_SECONDS), TimeSpan.FromSeconds(HEARTBEAT_SECONDS));
        }

        private void CheckInactivity(object? state)
        {
            try
            {
                var idleTime = GetIdleTime();
                
                // If idle for more than 15 minutes and not already away
                if (idleTime.TotalMinutes >= INACTIVITY_MINUTES && !_isAway && _currentStatus != UserStatus.Away)
                {
                    _isAway = true;
                    _ = UpdateStatusAsync(UserStatus.Away);
                }
                // If user became active again and was away
                else if (idleTime.TotalSeconds < 5 && _isAway)
                {
                    _isAway = false;
                    _ = UpdateStatusAsync(UserStatus.Online);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking inactivity: {ex.Message}");
            }
        }

        private void SendHeartbeat(object? state)
        {
            try
            {
                _ = SendHeartbeatAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending heartbeat: {ex.Message}");
            }
        }

        private TimeSpan GetIdleTime()
        {
            var lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            
            if (GetLastInputInfo(ref lastInputInfo))
            {
                var idleTime = ((uint)Environment.TickCount - lastInputInfo.dwTime);
                return TimeSpan.FromMilliseconds(idleTime);
            }
            
            return TimeSpan.Zero;
        }

        public async Task UpdateStatusAsync(UserStatus newStatus)
        {
            try
            {
                var request = new UpdateStatusRequest { NewStatus = newStatus };
                var response = await _httpClient.PutAsJsonAsync("https://localhost:5001/api/session/status", request);
                
                if (response.IsSuccessStatusCode)
                {
                    _currentStatus = newStatus;
                    StatusChanged?.Invoke(this, newStatus);
                    System.Diagnostics.Debug.WriteLine($"Status updated to: {newStatus.GetDisplayName()}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating status: {ex.Message}");
            }
        }

        private async Task SendHeartbeatAsync()
        {
            try
            {
                await _httpClient.PostAsync("https://localhost:5001/api/session/heartbeat", null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending heartbeat: {ex.Message}");
            }
        }

        public void SetStatus(UserStatus status)
        {
            _currentStatus = status;
            _isAway = (status == UserStatus.Away);
        }

        public UserStatus GetCurrentStatus() => _currentStatus;

        public void Dispose()
        {
            _inactivityTimer?.Dispose();
            _heartbeatTimer?.Dispose();
        }
    }
}
