using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace PaL.X.Client.Video
{
    public partial class VideoCallForm : Form
    {
        private readonly TaskCompletionSource<bool> _pageReady = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private bool _micOn = true;
        private bool _camOn = true;
        private readonly bool _incoming;
        private bool _inCall;

        private Image? _imgCamOn;
        private Image? _imgCamOff;
        private Image? _imgMicOn;
        private Image? _imgMicOff;
        private Image? _imgHangup;
        private Image? _imgCalling;

        public sealed class RtcSignalToSendEventArgs : EventArgs
        {
            public required string SignalType { get; init; }
            public required string Payload { get; init; }
        }

        public event EventHandler? HangupRequested;
        public event EventHandler<bool>? MicToggled;
        public event EventHandler<bool>? CamToggled;
        public event EventHandler? AcceptRequested;
        public event EventHandler? RejectRequested;
        public event EventHandler<RtcSignalToSendEventArgs>? RtcSignalToSend;

        public bool IsIncoming => _incoming;
        public bool IsInCall => _inCall;
        public bool IsMicOn => _micOn;
        public bool IsCamOn => _camOn;

        public VideoCallForm(string peerName, bool incoming)
        {
            InitializeComponent();

            // DesignMode check to prevent crashes in Visual Studio Designer
            if (DesignMode) return;

            _incoming = incoming;
            _inCall = !incoming;

            _lblName.Text = peerName;

            LoadAssets();
            SetupButtons();
            UpdateButtonsUi();

            Shown += async (_, __) =>
            {
                try
                {
                    await InitializeWebViewAsync();
                }
                catch
                {
                    // If WebView2 init fails, keep UI usable for accept/reject/hangup.
                }
            };

            if (_incoming)
            {
                _lblStatus.Text = "Appel vidéo entrant...";
                _lblStatus.ForeColor = Color.FromArgb(54, 179, 126); // Green accent
                
                // Show incoming buttons, hide others
                _btnAccept.Visible = true;
                _btnReject.Visible = true;
                _btnMic.Visible = false;
                _btnHangup.Visible = false;
                _btnCam.Visible = false;
            }
            else
            {
                // Show call buttons
                _btnAccept.Visible = false;
                _btnReject.Visible = false;
                _btnMic.Visible = true;
                _btnHangup.Visible = true;
                _btnCam.Visible = true;
            }
        }

        private void LoadAssets()
        {
            _imgCalling = TryLoadAsset("Appel en cour.png", new Size(64, 64));
            _imgHangup = TryLoadAsset("raccrocher.png", new Size(28, 28));
            _imgCamOff = TryLoadAsset("Cam_OFF.png", new Size(28, 28));
            _imgCamOn = TryLoadAsset("Cam_ON.png", new Size(28, 28));
            _imgMicOff = TryLoadAsset("Mic_OFF.png", new Size(28, 28));
            _imgMicOn = TryLoadAsset("Mic_ON.png", new Size(28, 28));
        }

        private static Image? TryLoadAsset(string fileName, Size size)
        {
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Video CaLL", fileName);
                if (!File.Exists(path))
                {
                    return null;
                }

                using var img = Image.FromFile(path);
                return new Bitmap(img, size);
            }
            catch
            {
                return null;
            }
        }

        private void SetupButtons()
        {
            // Wire events
            _btnCam.Click += (_, __) => ToggleCam();
            _btnMic.Click += (_, __) => ToggleMic();
            _btnHangup.Click += (_, __) => HangupRequested?.Invoke(this, EventArgs.Empty);
            _btnAccept.Click += (_, __) => AcceptRequested?.Invoke(this, EventArgs.Empty);
            _btnReject.Click += (_, __) => RejectRequested?.Invoke(this, EventArgs.Empty);

            // Set Tooltips
            _tt.SetToolTip(_btnCam, "Caméra");
            _tt.SetToolTip(_btnMic, "Micro");
            _tt.SetToolTip(_btnHangup, "Raccrocher");
            _tt.SetToolTip(_btnAccept, "Accepter");
            _tt.SetToolTip(_btnReject, "Refuser");

            // Apply Circular Region
            void MakeCircular(Button btn)
            {
                btn.Paint += (s, e) =>
                {
                    using var path = new System.Drawing.Drawing2D.GraphicsPath();
                    path.AddEllipse(0, 0, btn.Width, btn.Height);
                    btn.Region = new Region(path);
                };
            }

            MakeCircular(_btnCam);
            MakeCircular(_btnMic);
            MakeCircular(_btnHangup);
            MakeCircular(_btnAccept);
            MakeCircular(_btnReject);

            // Set initial images for static buttons
            _btnHangup.Image = _imgHangup;
            _btnAccept.Image = _imgCalling;
            _btnReject.Image = _imgHangup;
        }

        private void ToggleMic()
        {
            _micOn = !_micOn;
            UpdateButtonsUi();
            MicToggled?.Invoke(this, _micOn);
            _ = SetMicAsync(_micOn);
        }

        private void ToggleCam()
        {
            _camOn = !_camOn;
            UpdateButtonsUi();
            CamToggled?.Invoke(this, _camOn);
            _ = SetCamAsync(_camOn);
        }

        private void UpdateButtonsUi()
        {
            _btnMic.Image = _micOn ? _imgMicOn : _imgMicOff;
            _btnCam.Image = _camOn ? _imgCamOn : _imgCamOff;
        }

        public void SwitchToInCallMode()
        {
            if (_inCall) return;

            _inCall = true;
            _lblStatus.Text = "En appel";
            _lblStatus.ForeColor = Color.FromArgb(39, 174, 96); // Green status

            // Update visibility
            _btnAccept.Visible = false;
            _btnReject.Visible = false;
            
            _btnMic.Visible = true;
            _btnHangup.Visible = true;
            _btnCam.Visible = true;
        }

        private async Task InitializeWebViewAsync()
        {
            if (DesignMode) return;

            await _webView.EnsureCoreWebView2Async();
            if (_webView.CoreWebView2 == null)
            {
                return;
            }

            _webView.CoreWebView2.PermissionRequested += (_, e) =>
            {
                try
                {
                    if (e.PermissionKind == CoreWebView2PermissionKind.Microphone || e.PermissionKind == CoreWebView2PermissionKind.Camera)
                    {
                        e.State = CoreWebView2PermissionState.Allow;
                    }
                }
                catch { }
            };

            _webView.CoreWebView2.WebMessageReceived += (_, e) =>
            {
                try
                {
                    var json = e.WebMessageAsJson;
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        return;
                    }

                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    if (!root.TryGetProperty("kind", out var kindProp))
                    {
                        return;
                    }

                    var kind = kindProp.GetString() ?? string.Empty;
                    if (string.Equals(kind, "webrtc-page-loaded", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(kind, "webrtc-ready", StringComparison.OrdinalIgnoreCase))
                    {
                        _pageReady.TrySetResult(true);
                        return;
                    }

                    if (string.Equals(kind, "webrtc-signal", StringComparison.OrdinalIgnoreCase))
                    {
                        var signalType = root.TryGetProperty("signalType", out var st) ? (st.GetString() ?? string.Empty) : string.Empty;
                        var payload = root.TryGetProperty("payload", out var pl) ? (pl.GetString() ?? string.Empty) : string.Empty;
                        if (!string.IsNullOrWhiteSpace(signalType))
                        {
                            RtcSignalToSend?.Invoke(this, new RtcSignalToSendEventArgs { SignalType = signalType, Payload = payload });
                        }
                    }
                }
                catch
                {
                    // ignore malformed messages
                }
            };

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "palx.local",
                baseDir,
                CoreWebView2HostResourceAccessKind.Allow);

            var target = "https://palx.local/Video/WebRtc/webrtc_call.html";
            _webView.CoreWebView2.Navigate(target);
        }

        private async Task ExecuteWhenReadyAsync(string script)
        {
            try
            {
                await _pageReady.Task;
                if (_webView.CoreWebView2 == null)
                {
                    return;
                }

                await _webView.ExecuteScriptAsync(script);
            }
            catch
            {
                // ignore
            }
        }

        public Task StartWebRtcAsync(bool isInitiator)
        {
            return ExecuteWhenReadyAsync($"window.__palxStart({(isInitiator ? "true" : "false")});");
        }

        public Task ApplyRemoteSignalAsync(string signalType, string payload)
        {
            var safeType = JsonSerializer.Serialize(signalType ?? string.Empty);
            var safePayload = JsonSerializer.Serialize(payload ?? string.Empty);
            return ExecuteWhenReadyAsync($"window.__palxReceiveSignal({safeType}, {safePayload});");
        }

        public Task SetMicAsync(bool on)
        {
            return ExecuteWhenReadyAsync($"window.__palxSetMic({(on ? "true" : "false")});");
        }

        public Task SetCamAsync(bool on)
        {
            return ExecuteWhenReadyAsync($"window.__palxSetCam({(on ? "true" : "false")});");
        }

        public void SetStatus(string status)
        {
            _lblStatus.Text = status;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            _imgCamOn?.Dispose();
            _imgCamOff?.Dispose();
            _imgMicOn?.Dispose();
            _imgMicOff?.Dispose();
            _imgHangup?.Dispose();
            _imgCalling?.Dispose();
        }
    }
}
