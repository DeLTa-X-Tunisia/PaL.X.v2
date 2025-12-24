using PaL.X.Client.Services;
using PaL.X.Shared.DTOs;
using PaL.X.Shared.Enums;
using PaL.X.Client.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Net;
using System.Text.Json;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using Color = System.Drawing.Color;
using Image = System.Drawing.Image;
using Size = System.Drawing.Size;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.MediaFoundation;
using PaL.X.Client.Voice;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

namespace PaL.X.Client
{
    public enum VoiceCallUiState
    {
        Idle,
        InCall,
        Muted
    }

    public partial class FormChat : Form
    {
        private readonly UserProfileDto _recipient;
        private readonly UserData _currentUser;
        private readonly MainForm _mainForm;

        private WebView2 webViewChat = null!;
        private RichTextBox rtbInput = null!;
        private Button btnSend = null!;
        private Label lblStatus = null!;
        private PictureBox picProfile = null!;
        private Label lblName = null!;
        private static bool _pdfLicenseConfigured;
        private static readonly Regex _smileyTokenRegex = new Regex(@"@_[^\s]+?\.(?:gif|png)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly object _smileyExportCacheLock = new object();
    private static readonly Dictionary<string, byte[]> _smileyExportCache = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
    private const int PdfSmileySizePixels = 16;
    private const int InitialHistoryLimit = 50;
        private LinkLabel? _lnkLoadOlder;
        private bool _isLoadingOlderHistory;
        private bool _hasMoreHistory;
        private DateTime? _oldestLoadedTimestamp;
        private readonly HashSet<int> _loadedMessageIds = new HashSet<int>();
    // Status icon next to recipient name
    private PictureBox picStatusIcon = null!;
    private ToolStripDropDownButton _ddMyStatus = null!;
    private ImageList _statusImageList = null!;

        // Recipient DND state tracking
    private UserStatus _lastRecipientStatus;
    private bool _dndNoticeDisplayed = false;
    private UserStatus? _lastStatusNotice;
        private bool _inputLockedByDnd = false;
        private bool _inputDefaultsCaptured = false;
        private Color _inputDefaultBackColor;
        private Color _inputDefaultForeColor;
        private Color _sendDefaultBackColor;
        private Color _sendDefaultForeColor;
        private string? _composerRtfBeforeLock;
        private int _composerSelectionStartBeforeLock;
        private int _composerSelectionLengthBeforeLock;
    private List<string>? _smileyFilesBeforeLock;
        private bool _suppressInputEvents;
    private bool _isBlockedByMe;
    private bool _isBlockedByRemote;
    private string? _remoteBlockMessage;
    private bool _remoteBlockIsPermanent;
    private DateTime? _remoteBlockedUntil;
    private string? _remoteBlockReason;
    private bool _inputLockedByBlock;
    private Panel? _blockNoticePanel;
    private Label? _blockNoticeLabel;
    private PictureBox? _blockToggleIcon;
    private ToolTip? _blockToggleToolTip;
    private Image? _blockToggleBlockedImage;
    private Image? _blockToggleUnblockedImage;
    private bool _blockToggleBusy;

        private record PendingAttachment(ChatMessageControl Control, byte[] Payload, string FileName, string Mime, bool IsAudio, string TempId);

        // Pending image sends (keyed by data URI payload)
        private readonly Dictionary<string, (ChatMessageControl control, byte[] payload, string tempId)> _pendingImageMessages = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, (string contentKey, ChatMessageControl control)> _pendingImageMessagesByTempId = new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, (ChatMessageControl control, byte[] payload, string tempId)> _pendingVideoMessages = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, (string contentKey, ChatMessageControl control)> _pendingVideoMessagesByTempId = new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, (ChatMessageControl control, string tempId)> _pendingVideoUploads = new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, PendingAttachment> _pendingAttachments = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, PendingAttachment> _pendingAttachmentsByTempId = new(StringComparer.OrdinalIgnoreCase);

        // Global correlation to deduplicate sender bubbles
        private readonly Dictionary<string, ChatMessageControl> _pendingByTempId = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _completedTempIds = new(StringComparer.OrdinalIgnoreCase);
        
        // Deduplication for WebView messages
        private readonly HashSet<string> _pendingWebMessages = new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<ChatMessageControl, (DateTime lastUpdate, int lastPercent)> _progressCache = new();

    // Voice messages
    private ToolStripButton? _tsbVoice;
    private VoiceState _voiceState = VoiceState.Disabled;
    private readonly System.Windows.Forms.Timer _voiceTimeoutTimer = new System.Windows.Forms.Timer();

    // Voice call button (header)
    private PictureBox? _btnVoiceCall;
    private ToolTip? _ttVoiceCall;
    private PictureBox? _btnDeleteHistory;
    private Image? _deleteHistoryEmptyIcon;
    private Image? _deleteHistoryFilledIcon;
    private VoiceCallUiState _voiceCallUiState = VoiceCallUiState.Idle;
    private VoiceCallDockControl? _callDockControl;
    private DockPosition _callDockPosition = DockPosition.Top;
    private FlowLayoutPanel flpHistory = null!;
    private WaveInEvent? _waveIn;
    private WaveFileWriter? _waveWriter;
    private string? _currentRecordingPath;
    private string? _currentMp3Path;
    private long _voiceBytesWritten;
    private double? _lastVoiceDurationSeconds;
    private bool _voiceStopping;
    private bool _sendVoiceAfterStop;
    private readonly object _voiceLock = new();
    private const int MaxVoiceSeconds = 180; // 3 minutes

    private const string BlockToggleBlockIconResource = "icon/message/devil.ico";
    private const string BlockToggleUnblockIconResource = "icon/message/angel.ico";


        // Toolbar Controls
        private ToolStrip tsFormat = null!;
        private ToolStripButton tsbBold = null!;
        private ToolStripButton tsbItalic = null!;
        private ToolStripButton tsbUnderline = null!;
        private ToolStripButton tsbColor = null!;
        private ToolStripComboBox tscbFontSize = null!;
        private ToolStripButton tsbSmiley = null!;

        // Smiley tracking: maps smiley marker GUID to filename
    private readonly List<string> _smileyFilenames = new List<string>();

    // Context menu icons
    private static readonly string CopyIconPath = @"C:\Users\azizi\OneDrive\Desktop\PaL.X\context\Copy.png";
    private static readonly string PasteIconPath = @"C:\Users\azizi\OneDrive\Desktop\PaL.X\context\Paste.png";
    private static readonly Image? CopyIcon = TryLoadIcon(CopyIconPath);
    private static readonly Image? PasteIcon = TryLoadIcon(PasteIconPath);
        
        // Formatting persistence
        private Font? _lastUsedFont = null;
        private Color _lastUsedColor = Color.Black;

        // Typing indicator
        private System.Threading.Timer? _typingTimer;
        private bool _isCurrentlyTyping = false;
    private Label? _lblTypingIndicator;

        // Constructor for Designer
        public FormChat()
        {
            _recipient = new UserProfileDto();
            _currentUser = new UserData();
            _mainForm = null!;
            InitializeComponent();
            InitializeWebView();
            InitializeVoiceComponents();
            UpdateHeaderUI();
            _lastRecipientStatus = _recipient.CurrentStatus;
            ApplyInitialRecipientState();
        }

        private async void InitializeWebView()
        {
            // Hide legacy controls
            if (flpHistory != null) flpHistory.Visible = false;
            if (rtbInput != null && rtbInput.Parent != null) rtbInput.Parent.Visible = false;
            
            // Initialize WebView2
            webViewChat = new WebView2();
            webViewChat.Dock = DockStyle.Fill;
            this.Controls.Add(webViewChat);
            
            // Ensure WebView2 is behind the header but in front of everything else
            webViewChat.BringToFront();
            
            await webViewChat.EnsureCoreWebView2Async();

            // Map assets
            string assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
            if (!Directory.Exists(assetsPath)) Directory.CreateDirectory(assetsPath);
            
            webViewChat.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "assets.pal.x", 
                assetsPath, 
                CoreWebView2HostResourceAccessKind.Allow
            );

            // Map smileys (Direct path for testing)
            string smileysPath = @"C:\Users\azizi\OneDrive\Desktop\PaL.X\PaL.X.Assets\Smiley";
            if (!Directory.Exists(smileysPath)) Directory.CreateDirectory(smileysPath);

            webViewChat.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "smileys.pal.x", 
                smileysPath, 
                CoreWebView2HostResourceAccessKind.Allow
            );

            // Setup communication
            webViewChat.CoreWebView2.WebMessageReceived += WebView_WebMessageReceived;

            // Load Chat UI
            string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "chat", "index.html");
            if (File.Exists(htmlPath))
            {
                webViewChat.Source = new Uri(htmlPath);
            }
            else
            {
                MessageBox.Show($"Fichier introuvable: {htmlPath}");
            }
        }

        private void WebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                // Use WebMessageAsJson to handle both JSON objects and strings correctly
                string json = e.WebMessageAsJson;
                
                // Debug: Uncomment to see raw traffic
                // System.Diagnostics.Debug.WriteLine($"WebView Message: {json}");

                var message = JsonSerializer.Deserialize<JsonElement>(json);
                
                if (message.TryGetProperty("type", out var typeProp))
                {
                    string type = typeProp.GetString() ?? "";
                    if (type == "sendMessage")
                    {
                        if (message.TryGetProperty("content", out var contentProp))
                        {
                            string content = contentProp.GetString() ?? "";
                            _ = SendMessageFromWebAsync(content);
                        }
                    }
                    else if (type == "requestSmileys")
                    {
                        SendSmileyListToWeb();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WebView Error: {ex.Message}");
                // MessageBox.Show($"Erreur réception message: {ex.Message}");
            }
        }

        private void SendSmileyListToWeb()
        {
            try
            {
                // Use the specific assets folder requested
                string smileysPath = @"C:\Users\azizi\OneDrive\Desktop\PaL.X\PaL.X.Assets\Smiley";
                
                if (!Directory.Exists(smileysPath)) 
                {
                    // MessageBox.Show($"Dossier smileys introuvable: {smileysPath}");
                    return;
                }

                var smileyData = new Dictionary<string, List<string>>();
                
                // Load files directly from this folder into a "Défaut" category
                var files = Directory.GetFiles(smileysPath, "*.*")
                    .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || 
                                f.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ||
                                f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                    .Select(f => Path.GetFileName(f))
                    .Where(f => !string.IsNullOrEmpty(f))
                    .Cast<string>()
                    .ToList();

                if (files.Count > 0)
                {
                    smileyData["Défaut"] = files;
                }
                else 
                {
                     // MessageBox.Show($"Aucun fichier image trouvé dans : {smileysPath}");
                }

                var payload = new { type = "loadSmileys", payload = smileyData };
                string json = JsonSerializer.Serialize(payload);
                
                webViewChat.CoreWebView2.PostWebMessageAsJson(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading smileys: {ex.Message}");
                MessageBox.Show($"Erreur smileys: {ex.Message}");
            }
        }

        private async Task SendMessageFromWebAsync(string htmlContent)
        {
             if (_inputLockedByDnd || _inputLockedByBlock || _isBlockedByMe || _isBlockedByRemote)
                return;

            if (string.IsNullOrWhiteSpace(htmlContent)) return;

            var msg = new ChatMessageDto
            {
                SenderId = _currentUser.Id,
                SenderName = !string.IsNullOrWhiteSpace(_currentUser.FirstName) 
                    ? $"{_currentUser.FirstName} {_currentUser.LastName}" 
                    : _currentUser.Username,
                ReceiverId = _recipient.Id,
                Content = htmlContent, 
                ContentType = "HTML", 
                Timestamp = DateTime.UtcNow,
                SmileyFilenames = new List<string>(),
                ClientTempId = Guid.NewGuid().ToString()
            };

            // Register as pending to avoid duplicate when SignalR echoes it back
            if (!string.IsNullOrEmpty(msg.ClientTempId))
            {
                _pendingWebMessages.Add(msg.ClientTempId);
            }

            try
            {
                await _mainForm.SendPrivateMessage(msg);
                AddMessageToWebView(msg, isMe: true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur d'envoi: {ex.Message}");
            }
        }

        private void AddMessageToWebView(ChatMessageDto msg, bool isMe)
        {
            if (webViewChat == null || webViewChat.CoreWebView2 == null) return;

            var payload = new { 
                type = "newMessage", 
                payload = new { 
                    id = msg.MessageId, 
                    content = msg.Content, 
                    isMe = isMe, 
                    timestamp = msg.Timestamp.ToLocalTime().ToString("HH:mm") 
                } 
            };
            
            string json = JsonSerializer.Serialize(payload);
            webViewChat.CoreWebView2.PostWebMessageAsJson(json);
        }

        private void InitializeVoiceComponents()
        {
            _voiceTimeoutTimer.Interval = 5 * 60 * 1000; // 5 minutes
            _voiceTimeoutTimer.Tick += async (_, __) => await ResetVoiceToDisabledAsync();
        }

        private void InitializeCallDockControl()
        {
            _callDockControl = new VoiceCallDockControl
            {
                Dock = DockStyle.Top,
                Visible = false
            };

            if (_mainForm != null)
            {
                _callDockControl.MuteToggled += async (_, muted) => await _mainForm.SetCallMutedAsync(muted);
                _callDockControl.VolumeChanged += (_, vol) => _mainForm.SetCallVolume(vol);
                _callDockControl.HangupClicked += async (_, __) => await _mainForm.HangupActiveCallAsync();
                _callDockControl.AcceptClicked += async (_, __) =>
                {
                    await _mainForm.AcceptActiveCallAsync();
                    _callDockControl.SetStatus("En appel");
                    _callDockControl.SetAcceptVisible(false);
                };
            }
            _callDockControl.DockToggleRequested += (_, pos) => ToggleCallDock(pos);

            Controls.Add(_callDockControl);
            Controls.SetChildIndex(_callDockControl, 0);
        }

        private void ToggleCallDock(DockPosition position)
        {
            if (_callDockControl == null) return;
            _callDockPosition = position;
            _callDockControl.DockPosition = position;
            _callDockControl.Dock = position == DockPosition.Top ? DockStyle.Top : DockStyle.Bottom;
            if (position == DockPosition.Top)
            {
                Controls.SetChildIndex(_callDockControl, 0);
            }
            else
            {
                Controls.SetChildIndex(_callDockControl, Controls.Count - 1);
            }
        }

        private void RestartVoiceTimeout()
        {
            _voiceTimeoutTimer.Stop();
            if (_voiceState != VoiceState.Disabled)
            {
                _voiceTimeoutTimer.Start();
            }
        }

        public void UpdateVoiceCallButton(VoiceCallUiState state, string tooltip)
        {
            _voiceCallUiState = state;
            if (_btnVoiceCall == null) return;

            var offPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Chat_Form", "Appel_Vocal.png");
            var onPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Chat_Form", "Mic_ON.png");

            Image? icon = state switch
            {
                VoiceCallUiState.InCall => LoadAbsoluteImage(onPath, new Size(_btnVoiceCall.Width, _btnVoiceCall.Height))
                                            ?? ResourceImageStore.LoadImage("Voice/mic_on1.png", new Size(_btnVoiceCall.Width, _btnVoiceCall.Height)),

                VoiceCallUiState.Muted => LoadAbsoluteImage(offPath, new Size(_btnVoiceCall.Width, _btnVoiceCall.Height))
                                             ?? ResourceImageStore.LoadImage("Voice/mic_off1.png", new Size(_btnVoiceCall.Width, _btnVoiceCall.Height)),

                _ => LoadAbsoluteImage(offPath, new Size(_btnVoiceCall.Width, _btnVoiceCall.Height))
                     ?? ResourceImageStore.LoadImage("Voice/mic_off1.png", new Size(_btnVoiceCall.Width, _btnVoiceCall.Height)),
            };

            if (icon != null)
            {
                _btnVoiceCall.Image = icon;
            }

            if (_ttVoiceCall != null)
            {
                _ttVoiceCall.SetToolTip(_btnVoiceCall, tooltip);
            }

            UpdateCallDockUi(state, tooltip);
        }

        private void UpdateCallDockUi(VoiceCallUiState state, string statusText)
        {
            if (_callDockControl == null) return;

            if (state == VoiceCallUiState.Idle)
            {
                _callDockControl.Visible = false;
                return;
            }

            _callDockControl.SetStatus(statusText);
            _callDockControl.SetMuted(state == VoiceCallUiState.Muted);
            _callDockControl.SetDurationStart(DateTime.UtcNow);
            var isIncoming = statusText.IndexOf("entrant", StringComparison.OrdinalIgnoreCase) >= 0;
            _callDockControl.SetAcceptVisible(isIncoming);
            _callDockControl.Visible = true;
        }

        private void ApplyVoiceState(VoiceState state)
        {
            _voiceState = state;
            if (_tsbVoice != null)
            {
                var icon = state switch
                {
                    VoiceState.Disabled => ResourceImageStore.LoadImage("Voice/mic_desabled1.png", new Size(32, 32)),
                    VoiceState.Ready => LoadAbsoluteImage(@"C:\Users\azizi\OneDrive\Desktop\PaL.X\Voice\mic_off.png", new Size(32, 32))
                                        ?? ResourceImageStore.LoadImage("Voice/mic_off1.png", new Size(32, 32)),
                    VoiceState.Recording => ResourceImageStore.LoadImage("Voice/mic_on1.png", new Size(32, 32)),
                    _ => null
                };
                if (icon != null)
                {
                    _tsbVoice.Image = icon;
                }
            }

            RestartVoiceTimeout();
        }

        private async void VoiceButton_Click(object? sender, EventArgs e)
        {
            try
            {
                switch (_voiceState)
                {
                    case VoiceState.Disabled:
                        ApplyVoiceState(VoiceState.Ready);
                        break;
                    case VoiceState.Ready:
                        await StartVoiceRecordingAsync();
                        break;
                    case VoiceState.Recording:
                        await StopVoiceRecordingAsync(send: true);
                        break;
                }
            }
            catch (Exception ex)
            {
                PalMessageBox.Show($"Micro: {ex.Message}", "Micro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ApplyVoiceState(VoiceState.Disabled);
            }
        }

        private async Task ResetVoiceToDisabledAsync()
        {
            await StopVoiceRecordingAsync(send: false);
            ApplyVoiceState(VoiceState.Disabled);
        }

        private static Image? LoadAbsoluteImage(string path, Size targetSize)
        {
            try
            {
                if (!System.IO.File.Exists(path)) return null;
                using var src = Image.FromFile(path);
                if (src.Width == targetSize.Width && src.Height == targetSize.Height)
                {
                    return new Bitmap(src);
                }

                var resized = new Bitmap(targetSize.Width, targetSize.Height);
                using var g = Graphics.FromImage(resized);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(src, new Rectangle(Point.Empty, targetSize));
                return resized;
            }
            catch
            {
                return null;
            }
        }

        private Task StartVoiceRecordingAsync()
        {
            lock (_voiceLock)
            {
                if (_voiceState != VoiceState.Ready)
                    return Task.CompletedTask;
            }

            try
            {
                if (WaveInEvent.DeviceCount <= 0)
                {
                    throw new InvalidOperationException("Aucun micro détecté.");
                }

                _currentRecordingPath = Path.Combine(Path.GetTempPath(), $"palx_voice_{Guid.NewGuid():N}.wav");
                _currentMp3Path = Path.Combine(Path.GetTempPath(), $"palx_voice_{Guid.NewGuid():N}.mp3");
                _voiceBytesWritten = 0;
                _lastVoiceDurationSeconds = null;

                _waveIn = new WaveInEvent
                {
                    WaveFormat = new WaveFormat(16000, 1)
                };

                _waveWriter = new WaveFileWriter(_currentRecordingPath, _waveIn.WaveFormat);
                _waveIn.DataAvailable += WaveIn_DataAvailable;
                _waveIn.RecordingStopped += WaveIn_RecordingStopped;
                _waveIn.StartRecording();

                _sendVoiceAfterStop = false;
                ApplyVoiceState(VoiceState.Recording);
            }
            catch
            {
                CleanupVoiceRecording(deleteFile: true);
                ApplyVoiceState(VoiceState.Disabled);
                throw;
            }

            return Task.CompletedTask;
        }

        private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
        {
            try
            {
                _waveWriter?.Write(e.Buffer, 0, e.BytesRecorded);
                _waveWriter?.Flush();
                _voiceBytesWritten += e.BytesRecorded;

                var bytesPerSecond = _waveIn?.WaveFormat.AverageBytesPerSecond ?? 32000;
                if (bytesPerSecond > 0)
                {
                    _lastVoiceDurationSeconds = Math.Round((double)_voiceBytesWritten / bytesPerSecond, 2);
                }

                var maxBytes = bytesPerSecond * MaxVoiceSeconds;
                if (_voiceBytesWritten >= maxBytes && !_voiceStopping)
                {
                    // Auto-stop and send after hitting max duration
                    _ = StopVoiceRecordingAsync(send: true);
                }
            }
            catch
            {
                // ignore buffer errors
            }
        }

        private async void WaveIn_RecordingStopped(object? sender, StoppedEventArgs e)
        {
            var wavPath = _currentRecordingPath;
            var mp3Path = _currentMp3Path;
            var shouldSend = _sendVoiceAfterStop;
            try
            {
                var bytesPerSecond = _waveIn?.WaveFormat.AverageBytesPerSecond ?? 32000;
                _lastVoiceDurationSeconds = Math.Round((_voiceBytesWritten / Math.Max(1.0, bytesPerSecond)), 2);
            }
            catch
            {
                _lastVoiceDurationSeconds = null;
            }
            CleanupVoiceRecording(deleteFile: !shouldSend);
            _voiceStopping = false;
            _sendVoiceAfterStop = false;
            ApplyVoiceState(VoiceState.Ready);

            if (e.Exception != null)
            {
                PalMessageBox.Show($"Enregistrement interrompu : {e.Exception.Message}", "Micro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (!string.IsNullOrWhiteSpace(wavPath) && File.Exists(wavPath))
                {
                    TryDeleteFile(wavPath);
                }
                if (!string.IsNullOrWhiteSpace(mp3Path) && File.Exists(mp3Path))
                {
                    TryDeleteFile(mp3Path);
                }
                return;
            }

            if (!shouldSend)
            {
                if (!string.IsNullOrWhiteSpace(wavPath))
                {
                    TryDeleteFile(wavPath);
                }
                if (!string.IsNullOrWhiteSpace(mp3Path))
                {
                    TryDeleteFile(mp3Path);
                }
                return;
            }

            if (!string.IsNullOrWhiteSpace(wavPath) && File.Exists(wavPath))
            {
                var info = new FileInfo(wavPath);
                if (info.Length < 800) // ignore too-short clips
                {
                    TryDeleteFile(wavPath);
                    if (!string.IsNullOrWhiteSpace(mp3Path)) TryDeleteFile(mp3Path);
                    return;
                }

                // Encode to MP3 before sending
                try
                {
                    if (string.IsNullOrWhiteSpace(mp3Path))
                    {
                        mp3Path = Path.Combine(Path.GetTempPath(), $"palx_voice_{Guid.NewGuid():N}.mp3");
                    }

                    await ConvertWavToMp3Async(wavPath, mp3Path);
                    var mp3Info = new FileInfo(mp3Path);
                    await SendVoiceMessageAsync(mp3Info);
                }
                finally
                {
                    TryDeleteFile(wavPath);
                    if (!string.IsNullOrWhiteSpace(mp3Path)) TryDeleteFile(mp3Path);
                }
            }
        }

        private void TryDeleteFile(string path)
        {
            try { File.Delete(path); } catch { }
        }

        private Task StopVoiceRecordingAsync(bool send)
        {
            lock (_voiceLock)
            {
                if (_voiceStopping)
                {
                    return Task.CompletedTask;
                }
                _voiceStopping = true;
            }

            _sendVoiceAfterStop = send;

            if (_waveIn != null)
            {
                try
                {
                    _waveIn.StopRecording();
                }
                catch
                {
                    CleanupVoiceRecording(deleteFile: !send);
                    _voiceStopping = false;
                    _sendVoiceAfterStop = false;
                }
            }
            else
            {
                CleanupVoiceRecording(deleteFile: !send);
                _voiceStopping = false;
                _sendVoiceAfterStop = false;
                ApplyVoiceState(send ? VoiceState.Ready : VoiceState.Disabled);
            }
            return Task.CompletedTask;
        }

        private void CleanupVoiceRecording(bool deleteFile)
        {
            try
            {
                if (_waveIn != null)
                {
                    _waveIn.DataAvailable -= WaveIn_DataAvailable;
                    _waveIn.RecordingStopped -= WaveIn_RecordingStopped;
                    _waveIn.Dispose();
                }
            }
            catch { }
            finally { _waveIn = null; }

            try
            {
                _waveWriter?.Dispose();
            }
            catch { }
            finally { _waveWriter = null; }

            if (deleteFile && !string.IsNullOrWhiteSpace(_currentRecordingPath))
            {
                TryDeleteFile(_currentRecordingPath);
            }
            if (deleteFile && !string.IsNullOrWhiteSpace(_currentMp3Path))
            {
                TryDeleteFile(_currentMp3Path);
            }

            _currentRecordingPath = null;
            _currentMp3Path = null;
        }

        private async Task SendVoiceMessageAsync(FileInfo fileInfo)
        {
            if (!fileInfo.Exists)
            {
                return;
            }

            if (fileInfo.Length > 25 * 1024 * 1024)
            {
                PalMessageBox.Show("Message vocal trop volumineux (max 25 Mo).", "Voix", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var mime = "audio/mpeg";
            byte[] bytes;
            try
            {
                bytes = await File.ReadAllBytesAsync(fileInfo.FullName);
            }
            catch
            {
                PalMessageBox.Show("Impossible de lire l'enregistrement.", "Voix", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var msgTimestamp = DateTime.UtcNow;
            var senderName = !string.IsNullOrWhiteSpace(_currentUser.FirstName)
                ? $"{_currentUser.FirstName} {_currentUser.LastName}"
                : _currentUser.Username;

            var tempId = Guid.NewGuid().ToString("N");
            var placeholderContent = $"file://{fileInfo.FullName}";
            var control = new ChatMessageControl(0, _currentUser.Id, senderName, placeholderContent, msgTimestamp, isIncoming: false, isEdited: false, "Voice", null, ChatMessageControl.SendState.Sending, true);
            control.SetSendProgress(5);

            // Seed the bubble with the local payload so the sender can play/save even before the server ACK
            control.UpdateAttachmentSource(placeholderContent, bytes, fileInfo.Name, mime, isAudio: true, isVoice: true);

            control.RetryRequested += async (_, __) => await RetrySendAttachmentAsync(placeholderContent, bytes, fileInfo.Name, mime, true, control, tempId);

            flpHistory.Controls.Add(control);
            flpHistory.ScrollControlIntoView(control);

            var pending = new PendingAttachment(control, bytes, fileInfo.Name, mime, true, tempId);
            _pendingAttachments[fileInfo.FullName] = pending;
            _pendingAttachmentsByTempId[tempId] = pending;
            _pendingByTempId[tempId] = control;

            try
            {
                try
                {
                    await SendAttachmentUploadFlowAsync(fileInfo, mime, isAudio: true, control, senderName, tempId, contentTypeOverride: "Voice");
                }
                finally
                {
                    TryDeleteFile(fileInfo.FullName);
                }
            }
            finally
            {
                TryDeleteFile(fileInfo.FullName);
            }
        }

        public FormChat(UserProfileDto recipient, UserData currentUser, MainForm mainForm)
        {
            _recipient = recipient;
            _currentUser = currentUser;
            _mainForm = mainForm;
            InitializeComponent();
            InitializeWebView();
            InitializeVoiceComponents();
            UpdateHeader();
            _lastRecipientStatus = _recipient.CurrentStatus;
            ApplyInitialRecipientState();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Text = "Chat";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(400, 400);

            // 1. Header Panel
            var pnlHeader = new Panel();
            pnlHeader.SuspendLayout();
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 80;
            pnlHeader.BackColor = Color.WhiteSmoke;
            pnlHeader.Padding = new Padding(10);
            this.Controls.Add(pnlHeader);

            // Profile Picture
            picProfile = new PictureBox();
            picProfile.Size = new Size(60, 60);
            picProfile.Location = new Point(10, 10);
            picProfile.SizeMode = PictureBoxSizeMode.StretchImage;
            
            pnlHeader.Controls.Add(picProfile);

            // Name
            lblName = new Label();
            lblName.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblName.Location = new Point(80, 15);
            lblName.AutoSize = true;
            pnlHeader.Controls.Add(lblName);

            // Status Selector (harmonized avec MainForm)
            // Menu déroulant avec icônes et libellés, placé en haut à droite (à gauche de l'icône voicechat)
            _statusImageList = new ImageList
            {
                ImageSize = new Size(16, 16),
                ColorDepth = ColorDepth.Depth32Bit
            };
            try
            {
                TryAddStatusImage("icon/status/en_ligne.ico", "Online");
                TryAddStatusImage("icon/status/hors_ligne.ico", "Offline");
                TryAddStatusImage("icon/status/absent.ico", "Away");
                TryAddStatusImage("icon/status/brb.ico", "BRB");
                TryAddStatusImage("icon/status/dnd.ico", "DoNotDisturb");
                TryAddStatusImage("icon/status/occupé.ico", "Busy");
                TryAddStatusImage(@"Voice/en_appel.png", "InCall");
            }
            catch { /* icon loading is best-effort */ }

            var tsStatus = new ToolStrip
            {
                Dock = DockStyle.None,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            _ddMyStatus = new ToolStripDropDownButton
            {
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Text = _mainForm.CurrentUserStatus.GetDisplayName()
            };
            if (_statusImageList.Images.ContainsKey(_mainForm.CurrentUserStatus.ToString()))
            {
                _ddMyStatus.Image = _statusImageList.Images[_mainForm.CurrentUserStatus.ToString()];
            }

            var statuses = new[]
            {
                UserStatus.Online,
                UserStatus.Away,
                UserStatus.BRB,
                UserStatus.DoNotDisturb,
                UserStatus.Busy,
                UserStatus.Offline
            };
            foreach (var st in statuses)
            {
                var mi = new ToolStripMenuItem(st.GetDisplayName()) { Tag = st };
                if (_statusImageList.Images.ContainsKey(st.ToString()))
                {
                    mi.Image = _statusImageList.Images[st.ToString()];
                }
                mi.Click += async (s, e) =>
                {
                    var selected = (UserStatus)((ToolStripMenuItem)s!).Tag!;
                    try
                    {
                        await _mainForm.ChangeMyStatusAsync(selected);
                        _ddMyStatus.Text = selected.GetDisplayName();
                        if (_statusImageList.Images.ContainsKey(selected.ToString()))
                            _ddMyStatus.Image = _statusImageList.Images[selected.ToString()];
                        // Also reflect in header label color immediately
                        ApplyStatusStyle(selected);
                    }
                    catch { }
                };
                _ddMyStatus.DropDownItems.Add(mi);
            }
            tsStatus.Items.Add(_ddMyStatus);
            pnlHeader.Controls.Add(tsStatus);

            // Icône de statut du destinataire (à côté du nom)
            picStatusIcon = new PictureBox();
            picStatusIcon.Size = new Size(16, 16);
            picStatusIcon.Location = new Point(80 + lblName.Width + 5, 18);
            picStatusIcon.SizeMode = PictureBoxSizeMode.StretchImage;
            pnlHeader.Controls.Add(picStatusIcon);

            // Status & Role
            lblStatus = new Label();
            lblStatus.Text = "Statut";
            lblStatus.ForeColor = Color.Gray;
            lblStatus.Location = new Point(110, 40);
            lblStatus.AutoSize = true;
            pnlHeader.Controls.Add(lblStatus);

            InitializeBlockToggleControl(pnlHeader);

            // Removed legacy ComboBox quick status; replaced by harmonized dropdown next to the name

            // Action Icons (Top Right)
            int iconSize = 32;
            int iconSpacing = 40;
            int startX = this.ClientSize.Width - (iconSize + 10); // 10px margin from right

            // Delete Chat Icon (Active)
            _btnDeleteHistory = new PictureBox();
            _btnDeleteHistory.Size = new Size(iconSize, iconSize);
            _btnDeleteHistory.Location = new Point(startX - (iconSpacing * 0), 24);
            _btnDeleteHistory.SizeMode = PictureBoxSizeMode.Zoom;
            _btnDeleteHistory.Cursor = Cursors.Hand;
            var deleteIcon = ResourceImageStore.LoadImage("various/deletechat.png");
            if (deleteIcon != null)
            {
                _btnDeleteHistory.Image = deleteIcon;
            }
            _btnDeleteHistory.Click += BtnDeleteChat_Click;
            var ttDelete = new ToolTip();
            ttDelete.SetToolTip(_btnDeleteHistory, "Effacer le chat");
            pnlHeader.Controls.Add(_btnDeleteHistory);

            // Save Chat to PDF Icon (Active)
            var btnSaveChatPdf = new PictureBox();
            btnSaveChatPdf.Size = new Size(iconSize, iconSize);
            btnSaveChatPdf.Location = new Point(startX - (iconSpacing * 1), 24);
            btnSaveChatPdf.SizeMode = PictureBoxSizeMode.Zoom;
            btnSaveChatPdf.Cursor = Cursors.Hand;
            var saveHistoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Chat_Form", "Save_History.png");
            btnSaveChatPdf.Image =
                LoadAbsoluteImage(saveHistoryPath, new Size(btnSaveChatPdf.Width, btnSaveChatPdf.Height))
                ?? ResourceImageStore.LoadImage("various/savepdf.png");
            btnSaveChatPdf.Click += BtnSaveChatPdf_Click;
            var ttPdf = new ToolTip();
            ttPdf.SetToolTip(btnSaveChatPdf, "Enregistrer le chat");
            pnlHeader.Controls.Add(btnSaveChatPdf);

            // Video Chat Icon (Active)
            var btnVideoChat = new PictureBox();
            btnVideoChat.Size = new Size(iconSize, iconSize);
            btnVideoChat.Location = new Point(startX - (iconSpacing * 2), 24);
            btnVideoChat.SizeMode = PictureBoxSizeMode.Zoom;
            btnVideoChat.Cursor = Cursors.Hand;

            var videoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Chat_Form", "Video_Call.png");
            btnVideoChat.Image =
                LoadAbsoluteImage(videoPath, new Size(btnVideoChat.Width, btnVideoChat.Height))
                ?? ResourceImageStore.LoadImage("various/videochat.png");

            btnVideoChat.Click += async (_, __) => await (_mainForm?.StartVideoCall(_recipient.Id) ?? Task.CompletedTask);

            var ttVideo = new ToolTip();
            ttVideo.SetToolTip(btnVideoChat, "Chat vidéo");
            pnlHeader.Controls.Add(btnVideoChat);

            // Voice Call Icon (active)
            _btnVoiceCall = new PictureBox();
            _btnVoiceCall.Size = new Size(iconSize, iconSize);
            _btnVoiceCall.Location = new Point(startX - (iconSpacing * 3), 24);
            _btnVoiceCall.SizeMode = PictureBoxSizeMode.Zoom;
            _btnVoiceCall.Cursor = Cursors.Hand;
            _btnVoiceCall.Click += (_, __) => _mainForm?.StartVoiceCall(_recipient.Id);

            _ttVoiceCall = new ToolTip();
            pnlHeader.Controls.Add(_btnVoiceCall);
            UpdateVoiceCallButton(VoiceCallUiState.Idle, "Appel vocal");

            // Positionner le sélecteur de statut à gauche de l'icône voicechat
            // Place le ToolStrip juste avant le bouton voice chat
            tsStatus.Location = new Point(_btnVoiceCall.Left - 130, 28);

            // 2. Input Panel (Bottom)
            var pnlInput = new Panel();
            pnlInput.SuspendLayout();
            pnlInput.Dock = DockStyle.Bottom;
            pnlInput.Height = 100; // Reduced height as requested
            pnlInput.Padding = new Padding(10);
            pnlInput.BackColor = Color.WhiteSmoke;

            // ToolStrip (Formatting)
            tsFormat = new ToolStrip();
            tsFormat.SuspendLayout();
            tsFormat.Dock = DockStyle.Top;
            tsFormat.GripStyle = ToolStripGripStyle.Hidden;
            tsFormat.BackColor = Color.WhiteSmoke;
            tsFormat.RenderMode = ToolStripRenderMode.System;
            // Adjust padding to move icons slightly up (Bottom padding pushes content up)
            tsFormat.Padding = new Padding(0, 0, 0, 5); 
            
            tsbBold = new ToolStripButton("B");
            tsbBold.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbBold.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            tsbBold.Click += (s, e) => ToggleStyle(FontStyle.Bold);
            
            tsbItalic = new ToolStripButton("I");
            tsbItalic.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbItalic.Font = new Font("Segoe UI", 9, FontStyle.Italic);
            tsbItalic.Click += (s, e) => ToggleStyle(FontStyle.Italic);
            
            tsbUnderline = new ToolStripButton("U");
            tsbUnderline.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbUnderline.Font = new Font("Segoe UI", 9, FontStyle.Underline);
            tsbUnderline.Click += (s, e) => ToggleStyle(FontStyle.Underline);
            
            tsbColor = new ToolStripButton("Couleur");
            tsbColor.Click += TsbColor_Click;

            tscbFontSize = new ToolStripComboBox();
            tscbFontSize.Items.AddRange(new object[] { "8", "10", "12", "14", "16", "18", "20" });
            tscbFontSize.SelectedIndexChanged += TscbFontSize_SelectedIndexChanged;
            tscbFontSize.Text = "10";
            tscbFontSize.Size = new Size(50, 23);

            tsbSmiley = new ToolStripButton();
            tsbSmiley.DisplayStyle = ToolStripItemDisplayStyle.Image;
            tsbSmiley.AutoSize = false;
            tsbSmiley.Size = new Size(36, 36);
            tsbSmiley.ImageAlign = ContentAlignment.MiddleCenter;
            tsbSmiley.ImageScaling = ToolStripItemImageScaling.None;
            try 
            {
                var smileyIcon = ResourceImageStore.LoadImage("smiley/Basic_Smiley/s_4.png", new Size(32, 32));
                if (smileyIcon != null)
                {
                    tsbSmiley.Image = smileyIcon;
                }
                else
                {
                    tsbSmiley.Text = "😊";
                    tsbSmiley.DisplayStyle = ToolStripItemDisplayStyle.Text;
                }
            }
            catch { tsbSmiley.Text = "😊"; }
            tsbSmiley.Click += TsbSmiley_Click;

            // Future features: quick action icons (no behavior yet)
            var tsbSendPicVid = new ToolStripButton
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                AutoSize = false,
                Size = new Size(36, 36),
                ImageScaling = ToolStripItemImageScaling.None,
                Margin = new Padding(4, 0, 0, 0)
            };
            var picVidIcon = ResourceImageStore.LoadImage("send file/send_pic_vid.png", new Size(32, 32));
            if (picVidIcon != null)
            {
                tsbSendPicVid.Image = picVidIcon;
            }
            tsbSendPicVid.Click += async (s, e) => await SendMediaAsync();

            var tsbSendFile = new ToolStripButton
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                AutoSize = false,
                Size = new Size(36, 36),
                ImageScaling = ToolStripItemImageScaling.None,
                Margin = new Padding(4, 0, 0, 0)
            };
            var sendFileIcon = ResourceImageStore.LoadImage("send file/send_file.png", new Size(32, 32));
            if (sendFileIcon != null)
            {
                tsbSendFile.Image = sendFileIcon;
            }
            tsbSendFile.Click += async (s, e) => await SendAttachmentAsync();

            var tsbScreenshot = new ToolStripButton
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                AutoSize = false,
                Size = new Size(36, 36),
                ImageScaling = ToolStripItemImageScaling.None,
                Margin = new Padding(4, 0, 0, 0)
            };
            var screenshotIcon = ResourceImageStore.LoadImage("../various/EditScreen.png", new Size(32, 32))
                                 ?? ResourceImageStore.LoadImage("various/EditScreen.png", new Size(32, 32))
                                 ?? ResourceImageStore.LoadImage("../various/screenshot.png", new Size(32, 32))
                                 ?? ResourceImageStore.LoadImage("various/screenshot.png", new Size(32, 32));
            if (screenshotIcon != null)
            {
                tsbScreenshot.Image = screenshotIcon;
            }
            tsbScreenshot.Click += async (s, e) => await CaptureAndSendScreenshotAsync();

            _tsbVoice = new ToolStripButton
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                AutoSize = false,
                Size = new Size(36, 36),
                ImageScaling = ToolStripItemImageScaling.None,
                Margin = new Padding(4, 0, 0, 0)
            };
            _tsbVoice.Click += VoiceButton_Click;
            ApplyVoiceState(VoiceState.Disabled);

            tsFormat.Items.AddRange(new ToolStripItem[] { 
                tsbBold, tsbItalic, tsbUnderline, 
                new ToolStripSeparator(), 
                tsbColor, 
                new ToolStripSeparator(), 
                new ToolStripLabel("Taille:"), tscbFontSize,
                new ToolStripSeparator(),
                tsbSmiley,
                tsbSendPicVid,
                tsbSendFile,
                tsbScreenshot,
                _tsbVoice
            });
            pnlInput.Controls.Add(tsFormat);

            // Button Container
            var pnlBtnContainer = new Panel();
            pnlBtnContainer.SuspendLayout();
            pnlBtnContainer.Dock = DockStyle.Right;
            pnlBtnContainer.Width = 100;
            pnlBtnContainer.Padding = new Padding(10, 0, 0, 0);
            pnlBtnContainer.BackColor = Color.Transparent;
            pnlInput.Controls.Add(pnlBtnContainer);

            btnSend = new Button();
            btnSend.Text = "Envoyer";
            btnSend.Dock = DockStyle.Fill; // Fill the container to align vertically
            btnSend.Cursor = Cursors.Hand;
            btnSend.BackColor = Color.FromArgb(0, 122, 204);
            btnSend.ForeColor = Color.White;
            btnSend.FlatStyle = FlatStyle.Flat;
            btnSend.FlatAppearance.BorderSize = 0;
            btnSend.Click += BtnSend_Click;
            pnlBtnContainer.Controls.Add(btnSend);

            // Text Input
            rtbInput = new RichTextBox();
            rtbInput.Dock = DockStyle.Fill;
            rtbInput.BorderStyle = BorderStyle.None;
            rtbInput.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            rtbInput.KeyDown += RtbInput_KeyDown;
            rtbInput.TextChanged += RtbInput_TextChanged;
            SetupInputContextMenu();
            pnlInput.Controls.Add(rtbInput);

            EnsureInputDefaultsCaptured();
            
            // Ensure rtbInput is at the top of Z-order to fill remaining space properly
            rtbInput.BringToFront();
            
            _blockNoticePanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                BackColor = Color.FromArgb(255, 248, 220),
                Visible = false,
                Padding = new Padding(12)
            };

            _blockNoticeLabel = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(110, 60, 0),
                Text = string.Empty
            };

            _blockNoticePanel.Controls.Add(_blockNoticeLabel);
            this.Controls.Add(_blockNoticePanel);
            InitializeCallDockControl();
            this.Controls.Add(pnlInput);
            
            // Ensure pnlBtnContainer is processed AFTER tsFormat (so it doesn't take full height)
            pnlBtnContainer.BringToFront();
            
            // Send tsFormat to back so it docks FIRST (taking full width at the top)
            tsFormat.SendToBack();

            // 3. History (Fill)
            flpHistory = new FlowLayoutPanel();
            flpHistory.Dock = DockStyle.Fill;
            flpHistory.BackColor = Color.White;
            flpHistory.AutoScroll = true;
            flpHistory.FlowDirection = FlowDirection.TopDown;
            flpHistory.WrapContents = false; // Important for vertical list
            flpHistory.SuspendLayout();
            
            _lnkLoadOlder = new LinkLabel
            {
                Text = "Afficher l'historique précédent",
                AutoSize = true,
                LinkColor = Color.FromArgb(0, 122, 204),
                ActiveLinkColor = Color.FromArgb(0, 88, 148),
                Margin = new Padding(0, 6, 0, 2),
                Visible = false
            };
            _lnkLoadOlder.Click += async (s, e) => await LoadOlderMessagesAsync();
            flpHistory.Controls.Add(_lnkLoadOlder);

            // Initial state: chat is empty until history loads
            UpdateDeleteHistoryButtonIcon();

            flpHistory.ResumeLayout(false);
            this.Controls.Add(flpHistory);
            
            // Correct Z-Order for Docking
            // Panels dock first (Bottom of Z-Order)
            pnlHeader.SendToBack();
            _blockNoticePanel?.SendToBack();
            pnlInput.SendToBack();
            // Fill control docks last (Top of Z-Order)
            flpHistory.BringToFront();
            
            // Typing indicator label (add to form, not flpHistory)
            _lblTypingIndicator = new Label();
            _lblTypingIndicator.Text = "";
            _lblTypingIndicator.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            _lblTypingIndicator.ForeColor = Color.Gray;
            _lblTypingIndicator.BackColor = Color.White; // Same as chat background
            _lblTypingIndicator.AutoSize = true;
            _lblTypingIndicator.Visible = false;
            _lblTypingIndicator.Padding = new Padding(10, 5, 10, 5);
            _lblTypingIndicator.Dock = DockStyle.Bottom;
            this.Controls.Add(_lblTypingIndicator);
            _lblTypingIndicator.BringToFront();
            
            this.Load += async (s, e) => await LoadHistory();

            pnlBtnContainer.ResumeLayout(false);
            pnlInput.ResumeLayout(false);
            pnlInput.PerformLayout();
            tsFormat.ResumeLayout(false);
            tsFormat.PerformLayout();
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void InitializeBlockToggleControl(Control header)
        {
            const int iconSize = 26;

            _blockToggleIcon = new PictureBox
            {
                Size = new Size(iconSize, iconSize),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Visible = false
            };

            _blockToggleIcon.Click += BlockToggleIcon_Click;
            header.Controls.Add(_blockToggleIcon);

            _blockToggleToolTip = new ToolTip();

            LoadBlockToggleIcons();
            UpdateBlockToggleLayout();
            UpdateBlockToggleVisualState();
        }

        private void LoadBlockToggleIcons()
        {
            var size = new Size(26, 26);
            _blockToggleBlockedImage = LoadIconAsset(BlockToggleBlockIconResource, size);
            _blockToggleUnblockedImage = LoadIconAsset(BlockToggleUnblockIconResource, size);
        }

        private static Image? LoadIconAsset(string resourceKey, Size size)
        {
            var image = ResourceImageStore.LoadImage(resourceKey);
            if (image == null)
            {
                return null;
            }

            if (image.Width == size.Width && image.Height == size.Height)
            {
                return image;
            }

            try
            {
                var resized = new Bitmap(size.Width, size.Height);
                using var graphics = Graphics.FromImage(resized);
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(image, new Rectangle(Point.Empty, size));
                return resized;
            }
            finally
            {
                image.Dispose();
            }
        }

        private void UpdateBlockToggleLayout()
        {
            if (_blockToggleIcon == null || lblStatus == null)
            {
                return;
            }

            int x = Math.Max(10, lblStatus.Left - _blockToggleIcon.Width - 6);
            int y = lblStatus.Top + (lblStatus.Height - _blockToggleIcon.Height) / 2;
            _blockToggleIcon.Location = new Point(x, Math.Max(lblStatus.Top - 2, y));
        }

        private void UpdateBlockToggleVisualState()
        {
            if (_blockToggleIcon == null)
            {
                return;
            }

            if (_blockToggleBusy)
            {
                _blockToggleIcon.Enabled = false;
                _blockToggleIcon.Cursor = Cursors.WaitCursor;
            }
            else
            {
                _blockToggleIcon.Enabled = true;
                _blockToggleIcon.Cursor = Cursors.Hand;
            }

            var icon = _isBlockedByMe ? _blockToggleUnblockedImage : _blockToggleBlockedImage;
            if (icon != null)
            {
                _blockToggleIcon.Image = icon;
            }

            if (_blockToggleToolTip != null)
            {
                string tooltip;
                if (_blockToggleBusy)
                {
                    tooltip = "Action en cours…";
                }
                else if (_isBlockedByMe)
                {
                    tooltip = "Débloquer ce contact";
                }
                else
                {
                    tooltip = _isBlockedByRemote
                        ? "Bloquer ce contact (vous êtes bloqué)"
                        : "Bloquer ce contact";
                }

                _blockToggleToolTip.SetToolTip(_blockToggleIcon, tooltip);
            }

            _blockToggleIcon.Visible = true;
        }

        private async void BlockToggleIcon_Click(object? sender, EventArgs e)
        {
            await HandleBlockToggleAsync();
        }

        private async Task HandleBlockToggleAsync()
        {
            if (_mainForm == null || _mainForm.IsDisposed)
            {
                return;
            }

            if (_blockToggleBusy)
            {
                return;
            }

            _blockToggleBusy = true;
            UpdateBlockToggleVisualState();

            try
            {
                if (_isBlockedByMe)
                {
                    var (success, error) = await _mainForm.TryUnblockFriendAsync(_recipient.Id);
                    if (!success)
                    {
                        var message = string.IsNullOrWhiteSpace(error) ? "Déblocage impossible." : error;
                        PalMessageBox.Show(message, "Déblocage", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    var (success, error) = await _mainForm.TryApplyBlockAsync(_recipient.Id, null, true, null, null);
                    if (!success)
                    {
                        var message = string.IsNullOrWhiteSpace(error) ? "Blocage impossible." : error;
                        PalMessageBox.Show(message, "Blocage", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                PalMessageBox.Show($"Action impossible : {ex.Message}", "Blocage", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _blockToggleBusy = false;
                UpdateBlockToggleVisualState();
            }
        }

        private async Task LoadHistory()
        {
            if (_mainForm == null || flpHistory == null)
            {
                return;
            }

            _loadedMessageIds.Clear();
            _oldestLoadedTimestamp = null;

            List<ChatMessageDto>? messages = null;
            try
            {
                messages = (await _mainForm.GetChatHistory(_recipient.Id, InitialHistoryLimit))?.ToList();
            }
            catch
            {
                return;
            }

            if (messages == null || messages.Count == 0)
            {
                _hasMoreHistory = false;
                UpdateLoadOlderVisibility();
                UpdateDeleteHistoryButtonIcon();
                return;
            }

            _hasMoreHistory = messages.Count >= InitialHistoryLimit;

            if (messages.Count > InitialHistoryLimit)
            {
                messages = messages.Skip(messages.Count - InitialHistoryLimit).ToList();
            }

            // Send history to WebView
            var historyPayload = messages.Select(msg => new {
                id = msg.MessageId,
                content = msg.Content,
                isMe = msg.SenderId == _currentUser.Id,
                timestamp = msg.Timestamp.ToLocalTime().ToString("HH:mm")
            }).ToList();

            var payload = new { type = "loadHistory", payload = historyPayload };
            string json = JsonSerializer.Serialize(payload);
            if (webViewChat != null && webViewChat.CoreWebView2 != null)
            {
                webViewChat.CoreWebView2.PostWebMessageAsJson(json);
            }

            foreach (var msg in messages)
            {
                RegisterLoadedMessage(msg);
            }

            UpdateLoadOlderVisibility();
            UpdateDeleteHistoryButtonIcon();
        }

        public void UpdateProfile(UserProfileDto updatedProfile)
        {
            _recipient.FirstName = updatedProfile.FirstName;
            _recipient.LastName = updatedProfile.LastName;
            _recipient.ProfilePicture = updatedProfile.ProfilePicture;
            _recipient.Gender = updatedProfile.Gender;
            UpdateHeaderUI();
        }

        private void UpdateHeader() => UpdateHeaderUI();

        private void UpdateHeaderUI()
        {
            if (lblName != null)
            {
                lblName.Text = $"{_recipient.FirstName ?? string.Empty} {_recipient.LastName ?? string.Empty}";
                
                // Update status icon position next to name
                if (picStatusIcon != null)
                {
                    picStatusIcon.Location = new Point(lblName.Left + lblName.Width + 5, lblName.Top + 3);
                    // Load status icon
                            string iconResource = _recipient.CurrentStatus.GetIconPath();
                            var statusImage = ResourceImageStore.LoadImage(iconResource);
                            if (statusImage != null)
                            {
                                picStatusIcon.Image?.Dispose();
                                picStatusIcon.Image = statusImage;
                            }
                }
            }

            if (picProfile != null)
            {
                bool hasProfilePic = false;
                if (_recipient.ProfilePicture != null && _recipient.ProfilePicture.Length > 0)
                {
                    try
                    {
                        using (var ms = new System.IO.MemoryStream(_recipient.ProfilePicture))
                        {
                            picProfile.Image = Image.FromStream(ms);
                            hasProfilePic = true;
                        }
                    }
                    catch { }
                }

                if (!hasProfilePic)
                {
                    string genderIcon = "icon/gender/autre.ico";
                    if (string.Equals(_recipient.Gender, "Homme", StringComparison.OrdinalIgnoreCase)) genderIcon = "icon/gender/homme.ico";
                    else if (string.Equals(_recipient.Gender, "Femme", StringComparison.OrdinalIgnoreCase)) genderIcon = "icon/gender/femme.ico";

                    var fallbackImage = ResourceImageStore.LoadImage(genderIcon);
                    if (fallbackImage != null)
                    {
                        picProfile.Image?.Dispose();
                        picProfile.Image = fallbackImage;
                    }
                }
            }
            
            if (lblStatus != null)
            {
                lblStatus.Text = $"{_recipient.CurrentStatus.GetDisplayName()} - Ami";
                lblStatus.ForeColor = _recipient.CurrentStatus switch
                {
                    UserStatus.Online => Color.Green,
                    UserStatus.Offline => Color.Gray,
                    UserStatus.Busy => Color.Red,
                    UserStatus.BRB => Color.Orange,
                    UserStatus.DoNotDisturb => Color.Violet,
                    UserStatus.Away => Color.Blue,
                    UserStatus.InCall => Color.MediumPurple,
                    _ => Color.Gray
                };
            }

            UpdateBlockToggleLayout();
            UpdateBlockToggleVisualState();
            
            this.Text = $"Chat avec {_recipient.FirstName} {_recipient.LastName}";
        }

        public void UpdateUserStatus(PaL.X.Shared.Enums.UserStatus newStatus)
        {
            var previousStatus = _lastRecipientStatus;
            _recipient.CurrentStatus = newStatus;
            _recipient.IsOnline = newStatus != PaL.X.Shared.Enums.UserStatus.Offline;
            UpdateHeaderUI();
            // Harmonized dropdown handles its own visual state; removed legacy ComboBox sync
            HandleRecipientStatusChange(previousStatus, newStatus);
            _lastRecipientStatus = newStatus;
        }

        // Called by MainForm when my own status changes, to sync the chat header dropdown
        public void UpdateMyStatus(UserStatus myStatus)
        {
            if (_ddMyStatus != null)
            {
                _ddMyStatus.Text = myStatus.GetDisplayName();
                if (_statusImageList != null && _statusImageList.Images.ContainsKey(myStatus.ToString()))
                {
                    _ddMyStatus.Image = _statusImageList.Images[myStatus.ToString()];
                }
            }
        }

        // Helper to immediately reflect status styling in the header
        private void ApplyStatusStyle(UserStatus status)
        {
            if (lblStatus != null)
            {
                lblStatus.Text = $"{status.GetDisplayName()} - Ami";
                lblStatus.ForeColor = status switch
                {
                    UserStatus.Online => Color.Green,
                    UserStatus.Offline => Color.Gray,
                    UserStatus.Busy => Color.Red,
                    UserStatus.BRB => Color.Orange,
                    UserStatus.DoNotDisturb => Color.Violet,
                    UserStatus.Away => Color.Blue,
                    UserStatus.InCall => Color.MediumPurple,
                    _ => Color.Gray
                };
            }
        }

        private void TryAddStatusImage(string resourceKey, string alias)
        {
            var image = ResourceImageStore.LoadImage(resourceKey);
            if (image != null && !_statusImageList.Images.ContainsKey(alias))
            {
                _statusImageList.Images.Add(alias, image);
            }
        }

        private void ToggleStyle(FontStyle style)
        {
            if (rtbInput.SelectionFont != null)
            {
                Font currentFont = rtbInput.SelectionFont;
                FontStyle newStyle = currentFont.Style ^ style;
                rtbInput.SelectionFont = new Font(currentFont.FontFamily, currentFont.Size, newStyle);
            }
        }

        private void HandleRecipientStatusChange(UserStatus previousStatus, UserStatus newStatus)
        {
            if (previousStatus == newStatus)
                return;

            // Si l'utilisateur distant m'a bloqué, je ne dois pas voir ses changements de statut
            if (_isBlockedByRemote)
                return;

            bool isNowDnd = newStatus == UserStatus.DoNotDisturb;
            bool wasDnd = previousStatus == UserStatus.DoNotDisturb;

            if (isNowDnd)
            {
                AppendDndSystemNotice();
                ApplyDndLock(true);
                _lastStatusNotice = UserStatus.DoNotDisturb;
                return;
            }

            if (wasDnd)
            {
                ApplyDndLock(false);
                _dndNoticeDisplayed = false;
                _lastStatusNotice = null;
            }

            AppendStatusSystemNotice(previousStatus, newStatus);
        }

        private void EnsureInputDefaultsCaptured()
        {
            if (_inputDefaultsCaptured || rtbInput == null || btnSend == null)
                return;

            _inputDefaultBackColor = rtbInput.BackColor;
            _inputDefaultForeColor = rtbInput.ForeColor;
            _sendDefaultBackColor = btnSend.BackColor;
            _sendDefaultForeColor = btnSend.ForeColor;
            _inputDefaultsCaptured = true;
        }

        private void ApplyDndLock(bool lockInput)
        {
            if (rtbInput == null || btnSend == null)
                return;

            if (this.InvokeRequired)
            {
                this.Invoke(new Action<bool>(ApplyDndLock), lockInput);
                return;
            }

            EnsureInputDefaultsCaptured();

            if (lockInput)
            {
                if (_inputLockedByDnd)
                    return;

                _inputLockedByDnd = true;
                _composerRtfBeforeLock = rtbInput.Rtf;
                _composerSelectionStartBeforeLock = rtbInput.SelectionStart;
                _composerSelectionLengthBeforeLock = rtbInput.SelectionLength;
                _smileyFilesBeforeLock = new List<string>(_smileyFilenames);

                rtbInput.ReadOnly = true;
                rtbInput.BackColor = Color.FromArgb(255, 243, 243);
                rtbInput.ForeColor = Color.FromArgb(120, 120, 120);
                rtbInput.Cursor = Cursors.No;
                _suppressInputEvents = true;
                rtbInput.Text = "Ne pas déranger";
                rtbInput.SelectionStart = rtbInput.TextLength;
                rtbInput.SelectionLength = 0;
                _suppressInputEvents = false;
                if (tsFormat != null)
                {
                    tsFormat.Enabled = false;
                }

                btnSend.Enabled = false;
                btnSend.Cursor = Cursors.No;
                btnSend.BackColor = Color.FromArgb(204, 51, 51);
                btnSend.ForeColor = Color.White;
                return;
            }

            if (!_inputLockedByDnd)
                return;

            _inputLockedByDnd = false;
            rtbInput.ReadOnly = false;
            rtbInput.BackColor = _inputDefaultBackColor;
            rtbInput.ForeColor = _inputDefaultForeColor;
            rtbInput.Cursor = Cursors.IBeam;
            _suppressInputEvents = true;
            if (!string.IsNullOrEmpty(_composerRtfBeforeLock))
            {
                rtbInput.Rtf = _composerRtfBeforeLock;
                int start = Math.Max(0, Math.Min(_composerSelectionStartBeforeLock, rtbInput.TextLength));
                int length = Math.Max(0, Math.Min(_composerSelectionLengthBeforeLock, rtbInput.TextLength - start));
                rtbInput.Select(start, length);
            }
            else
            {
                rtbInput.Clear();
            }
            _suppressInputEvents = false;
            _smileyFilenames.Clear();
            if (_smileyFilesBeforeLock != null)
            {
                _smileyFilenames.AddRange(_smileyFilesBeforeLock);
            }
            _composerRtfBeforeLock = null;
            _smileyFilesBeforeLock = null;
            if (tsFormat != null)
            {
                tsFormat.Enabled = true;
            }

            btnSend.Enabled = true;
            btnSend.Cursor = Cursors.Hand;
            btnSend.BackColor = _sendDefaultBackColor;
            btnSend.ForeColor = _sendDefaultForeColor;
            if (Visible)
            {
                rtbInput.Focus();
            }
        }

        private void AppendDndSystemNotice()
        {
            if (InvokeRequired)
            {
                try
                {
                    Invoke(new Action(AppendDndSystemNotice));
                }
                catch { }
                return;
            }

            if (flpHistory == null || _dndNoticeDisplayed)
                return;

            _dndNoticeDisplayed = true;

            string recipientName = $"{_recipient.FirstName ?? string.Empty} {_recipient.LastName ?? string.Empty}".Trim();
            if (string.IsNullOrWhiteSpace(recipientName))
            {
                recipientName = string.IsNullOrWhiteSpace(_recipient.DisplayedName)
                    ? "Ce contact"
                    : _recipient.DisplayedName;
            }

            var highlightColor = Color.FromArgb(184, 28, 28);

            var outer = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Padding = new Padding(12, 8, 12, 8),
                BackColor = Color.FromArgb(255, 235, 235)
            };

            var textRow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0),
                Padding = new Padding(0),
                BackColor = Color.Transparent
            };

            textRow.Controls.Add(new Label
            {
                Text = recipientName,
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.Black,
                Margin = new Padding(0)
            });

            textRow.Controls.Add(new Label
            {
                Text = " est passé en mode ",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = highlightColor,
                Margin = new Padding(0)
            });

            textRow.Controls.Add(new Label
            {
                Text = "Ne pas déranger",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = highlightColor,
                Margin = new Padding(0)
            });

            textRow.Controls.Add(new Label
            {
                Text = ".",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = highlightColor,
                Margin = new Padding(0)
            });

            outer.Controls.Add(textRow);

            outer.Controls.Add(new Label
            {
                Text = DateTime.Now.ToString("HH:mm"),
                AutoSize = true,
                Font = new Font("Segoe UI", 7F, FontStyle.Regular),
                ForeColor = Color.Gray,
                Margin = new Padding(0, 6, 0, 0)
            });

            var preferred = outer.GetPreferredSize(new Size(flpHistory.ClientSize.Width, 0));
            int availableWidth = flpHistory.ClientSize.Width;
            if (availableWidth <= 0)
            {
                availableWidth = flpHistory.Width;
            }
            int sideMargin = Math.Max(0, (availableWidth - preferred.Width) / 2);
            outer.Margin = new Padding(sideMargin, 12, sideMargin, 0);

            flpHistory.Controls.Add(outer);
            flpHistory.ScrollControlIntoView(outer);
        }

        private void AppendStatusSystemNotice(UserStatus previousStatus, UserStatus newStatus)
        {
            if (InvokeRequired)
            {
                try
                {
                    Invoke(new Action<UserStatus, UserStatus>(AppendStatusSystemNotice), previousStatus, newStatus);
                }
                catch { }
                return;
            }

            if (flpHistory == null)
                return;

            // Si je suis bloqué par le contact, ne pas afficher ses changements de statut
            if (_isBlockedByRemote)
                return;

            if (_lastStatusNotice.HasValue && _lastStatusNotice.Value == newStatus)
                return;

            string recipientName = $"{_recipient.FirstName ?? string.Empty} {_recipient.LastName ?? string.Empty}".Trim();
            if (string.IsNullOrWhiteSpace(recipientName))
            {
                recipientName = string.IsNullOrWhiteSpace(_recipient.DisplayedName)
                    ? "Ce contact"
                    : _recipient.DisplayedName;
            }

            var (statusColor, statusDisplay) = GetStatusNoticeStyle(newStatus);

            var outer = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Padding = new Padding(12, 8, 12, 8),
                BackColor = Color.Transparent
            };

            var textRow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0),
                Padding = new Padding(0),
                BackColor = Color.Transparent
            };

            textRow.Controls.Add(new Label
            {
                Text = recipientName,
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.Black,
                Margin = new Padding(0)
            });

            textRow.Controls.Add(new Label
            {
                Text = " est passé en mode ",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = statusColor,
                Margin = new Padding(0)
            });

            textRow.Controls.Add(new Label
            {
                Text = statusDisplay,
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = statusColor,
                Margin = new Padding(0)
            });

            textRow.Controls.Add(new Label
            {
                Text = ".",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = statusColor,
                Margin = new Padding(0)
            });

            outer.Controls.Add(textRow);

            outer.Controls.Add(new Label
            {
                Text = DateTime.Now.ToString("HH:mm"),
                AutoSize = true,
                Font = new Font("Segoe UI", 7F, FontStyle.Regular),
                ForeColor = Color.Gray,
                Margin = new Padding(0, 6, 0, 0)
            });

            var preferred = outer.GetPreferredSize(new Size(flpHistory.ClientSize.Width, 0));
            int availableWidth = flpHistory.ClientSize.Width;
            if (availableWidth <= 0)
            {
                availableWidth = flpHistory.Width;
            }
            int sideMargin = Math.Max(0, (availableWidth - preferred.Width) / 2);
            outer.Margin = new Padding(sideMargin, 12, sideMargin, 0);

            flpHistory.Controls.Add(outer);
            flpHistory.ScrollControlIntoView(outer);
            _lastStatusNotice = newStatus;
        }

        private (Color color, string display) GetStatusNoticeStyle(UserStatus status)
        {
            return status switch
            {
                UserStatus.Online => (Color.FromArgb(0, 153, 68), status.GetDisplayName()),
                UserStatus.Busy => (Color.FromArgb(255, 111, 0), status.GetDisplayName()),
                UserStatus.BRB => (Color.FromArgb(0, 120, 215), status.GetDisplayName()),
                UserStatus.Away => (Color.FromArgb(255, 165, 0), status.GetDisplayName()),
                UserStatus.Offline => (Color.FromArgb(120, 120, 120), status.GetDisplayName()),
                UserStatus.DoNotDisturb => (Color.FromArgb(184, 28, 28), status.GetDisplayName()),
                UserStatus.InCall => (Color.FromArgb(200, 0, 0), status.GetDisplayName()),
                _ => (Color.FromArgb(120, 120, 120), status.GetDisplayName())
            };
        }

        private void ApplyInitialRecipientState()
        {
            if (_recipient.CurrentStatus == UserStatus.DoNotDisturb)
            {
                AppendDndSystemNotice();
                ApplyDndLock(true);
            }

            UpdateBlockedStateUI();
        }

        public void SetBlockedByMe(bool isBlocked)
        {
            _isBlockedByMe = isBlocked;
            UpdateBlockedStateUI();
        }

        public void SetBlockedByRemote(bool isBlocked, string? message, bool isPermanent, DateTime? blockedUntil, string? reason)
        {
            _isBlockedByRemote = isBlocked;
            _remoteBlockMessage = message;
            _remoteBlockIsPermanent = isPermanent;
            _remoteBlockedUntil = blockedUntil;
            _remoteBlockReason = reason;
            UpdateBlockedStateUI();
        }

        private void UpdateBlockedStateUI()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(UpdateBlockedStateUI));
                return;
            }

            UpdateBlockToggleVisualState();

            if (_blockNoticePanel == null || _blockNoticeLabel == null)
            {
                return;
            }

            if (!_isBlockedByMe && !_isBlockedByRemote)
            {
                _blockNoticePanel.Visible = false;
                _blockNoticeLabel.Text = string.Empty;
                ApplyBlockComposerLock(false, string.Empty);
                SetVoiceCallAvailability(true, "Appel vocal");
                return;
            }

            var noticeBuilder = new StringBuilder();

            if (_isBlockedByRemote)
            {
                noticeBuilder.Append("Cet utilisateur vous a bloqué");
                if (_remoteBlockIsPermanent)
                {
                    noticeBuilder.Append(" de manière permanente.");
                }
                else if (_remoteBlockedUntil.HasValue)
                {
                    noticeBuilder.Append($" jusqu'au {_remoteBlockedUntil.Value.ToLocalTime():dd/MM/yyyy HH:mm}.");
                }
                else
                {
                    noticeBuilder.Append('.');
                }

                if (!string.IsNullOrWhiteSpace(_remoteBlockReason))
                {
                    noticeBuilder.Append($" Raison : {_remoteBlockReason}.");
                }

                if (!string.IsNullOrWhiteSpace(_remoteBlockMessage))
                {
                    noticeBuilder.AppendLine();
                    noticeBuilder.Append(_remoteBlockMessage);
                }
            }

            if (_isBlockedByMe)
            {
                if (noticeBuilder.Length > 0)
                {
                    noticeBuilder.AppendLine();
                    noticeBuilder.AppendLine();
                }

                noticeBuilder.Append("Vous avez bloqué cet utilisateur. Débloquez-le depuis la liste d'amis pour reprendre la discussion.");
            }

            var composerMessage = _isBlockedByRemote
                ? "Envoi désactivé : vous êtes bloqué."
                : "Envoi désactivé : utilisateur bloqué.";

            _blockNoticeLabel.Text = noticeBuilder.ToString();
            _blockNoticePanel.Visible = true;
            if (_lblTypingIndicator != null)
            {
                _lblTypingIndicator.Visible = false;
            }
            ApplyBlockComposerLock(true, composerMessage);

            if (_isBlockedByRemote)
            {
                SetVoiceCallAvailability(false, "Appel impossible — contact bloqué.");
            }
            else if (_isBlockedByMe)
            {
                SetVoiceCallAvailability(false, "Appel impossible — contact bloqué.");
            }
        }

        private void SetVoiceCallAvailability(bool enabled, string tooltip)
        {
            if (_btnVoiceCall != null)
            {
                _btnVoiceCall.Enabled = enabled;
                _btnVoiceCall.Cursor = enabled ? Cursors.Hand : Cursors.No;
                if (_ttVoiceCall != null)
                {
                    _ttVoiceCall.SetToolTip(_btnVoiceCall, tooltip);
                }
            }
        }

        private void ApplyBlockComposerLock(bool lockInput, string message)
        {
            if (rtbInput == null || btnSend == null)
            {
                return;
            }

            if (InvokeRequired)
            {
                Invoke(new Action<bool, string>(ApplyBlockComposerLock), lockInput, message);
                return;
            }

            EnsureInputDefaultsCaptured();

            if (lockInput)
            {
                if (_inputLockedByBlock)
                {
                    _suppressInputEvents = true;
                    rtbInput.Text = message;
                    rtbInput.SelectionStart = rtbInput.TextLength;
                    rtbInput.SelectionLength = 0;
                    _suppressInputEvents = false;
                    return;
                }

                _inputLockedByBlock = true;
                rtbInput.ReadOnly = true;
                rtbInput.BackColor = Color.FromArgb(254, 238, 238);
                rtbInput.ForeColor = Color.FromArgb(120, 120, 120);
                rtbInput.Cursor = Cursors.No;
                _suppressInputEvents = true;
                rtbInput.Text = message;
                rtbInput.SelectionStart = rtbInput.TextLength;
                rtbInput.SelectionLength = 0;
                _suppressInputEvents = false;
                if (tsFormat != null)
                {
                    tsFormat.Enabled = false;
                }

                btnSend.Enabled = false;
                btnSend.Cursor = Cursors.No;
                btnSend.BackColor = Color.FromArgb(189, 73, 73);
                btnSend.ForeColor = Color.White;
                return;
            }

            if (!_inputLockedByBlock)
            {
                return;
            }

            _inputLockedByBlock = false;

            if (_inputLockedByDnd)
            {
                return;
            }

            rtbInput.ReadOnly = false;
            rtbInput.BackColor = _inputDefaultBackColor;
            rtbInput.ForeColor = _inputDefaultForeColor;
            rtbInput.Cursor = Cursors.IBeam;
            _suppressInputEvents = true;
            rtbInput.Clear();
            _suppressInputEvents = false;
            if (tsFormat != null)
            {
                tsFormat.Enabled = true;
            }

            btnSend.Enabled = true;
            btnSend.Cursor = Cursors.Hand;
            btnSend.BackColor = _sendDefaultBackColor;
            btnSend.ForeColor = _sendDefaultForeColor;

            if (_isBlockedByMe || _isBlockedByRemote)
            {
                UpdateBlockedStateUI();
                return;
            }

            if (Visible)
            {
                rtbInput.Focus();
            }
        }

        private void TsbColor_Click(object? sender, EventArgs e)
        {
            using (var cd = new ColorDialog())
            {
                if (cd.ShowDialog() == DialogResult.OK)
                {
                    rtbInput.SelectionColor = cd.Color;
                }
            }
        }

        private void TscbFontSize_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (rtbInput == null) return;
            if (float.TryParse(tscbFontSize.Text, out float size) && rtbInput.SelectionFont != null)
            {
                Font currentFont = rtbInput.SelectionFont;
                rtbInput.SelectionFont = new Font(currentFont.FontFamily, size, currentFont.Style);
            }
        }

        private void TsbSmiley_Click(object? sender, EventArgs e)
        {
            var frm = new FormSmileys(_currentUser.IsAdmin);
            // Position ABOVE the button
            Point pt = tsFormat.PointToScreen(new Point(tsbSmiley.Bounds.Left, tsbSmiley.Bounds.Top));
            // Adjust for form height
            pt.Y -= 200; // Height of FormSmileys
            frm.Location = pt;
            
            frm.OnSmileySelected += selection =>
            {
                if (selection == null)
                {
                    return;
                }

                try
                {
                    System.Diagnostics.Debug.WriteLine($"[FormChat] Inserting smiley: {selection.FileName} from {selection.ResourceKey}");
                    using var rasterized = ResourceImageStore.LoadStaticImage(selection.ResourceKey, new Size(28, 28));
                    if (rasterized == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[FormChat] Failed to load image for {selection.ResourceKey}");
                        return;
                    }

                    var preservedFont = rtbInput.SelectionFont ?? _lastUsedFont ?? rtbInput.Font;
                    var preservedColor = rtbInput.SelectionColor;
                    var preservedStart = rtbInput.SelectionStart;

                    Clipboard.SetImage(rasterized);
                    rtbInput.Paste();

                    // Vérifier que le placeholder est bien présent
                    int placeholderCount = rtbInput.Text.Count(c => c == '\uFFFC');
                    System.Diagnostics.Debug.WriteLine($"[FormChat] After Paste - Placeholders in text: {placeholderCount}");

                    // Let RichTextBox handle cursor placement after paste
                    // rtbInput.SelectionStart = preservedStart + 1;
                    rtbInput.SelectionLength = 0;
                    rtbInput.SelectionColor = preservedColor;
                    rtbInput.SelectionFont = preservedFont;

                    _smileyFilenames.Add(selection.FileName);
                    System.Diagnostics.Debug.WriteLine($"[FormChat] Smiley inserted: {selection.FileName}, total tracked: {_smileyFilenames.Count}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[FormChat] Error inserting smiley: {ex.Message}");
                }
            };
            frm.Show(this);
            frm.BeginInvoke(new Action(() =>
            {
                if (!frm.IsDisposed)
                {
                    frm.Activate();
                }
            }));
        }

        private void RtbInput_KeyDown(object? sender, KeyEventArgs e)
        {
            if (_inputLockedByDnd || _inputLockedByBlock)
            {
                e.SuppressKeyPress = true;
                return;
            }

            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                BtnSend_Click(sender, e);
            }
        }

        private async void RtbInput_TextChanged(object? sender, EventArgs e)
        {
            if (_suppressInputEvents || _inputLockedByDnd || _inputLockedByBlock)
                return;

            bool hasText = !string.IsNullOrWhiteSpace(rtbInput.Text.Replace("\u200B", ""));
            TrimDetachedSmileys();

            if (hasText && !_isCurrentlyTyping)
            {
                // User started typing
                _isCurrentlyTyping = true;
                await _mainForm.NotifyTyping(_recipient.Id, true);
            }

            // Reset the timer - stop typing after 2 seconds of inactivity
            _typingTimer?.Dispose();
            _typingTimer = new System.Threading.Timer(async _ =>
            {
                if (_isCurrentlyTyping)
                {
                    _isCurrentlyTyping = false;
                    await _mainForm.NotifyTyping(_recipient.Id, false);
                }
            }, null, 2000, Timeout.Infinite);
        }

        private void TrimDetachedSmileys()
        {
            if (_smileyFilenames.Count == 0)
            {
                return;
            }

            var text = rtbInput.Text;
            int embeddedCount = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\uFFFC')
                {
                    embeddedCount++;
                }
            }

            System.Diagnostics.Debug.WriteLine($"[TRIM] Smileys tracked: {_smileyFilenames.Count}, Placeholders in text: {embeddedCount}");

            if (embeddedCount == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[TRIM] No placeholders found - WOULD clear {_smileyFilenames.Count} smileys but disabled for debugging");
                // _smileyFilenames.Clear();
                // return;
            }

            if (_smileyFilenames.Count > embeddedCount)
            {
                System.Diagnostics.Debug.WriteLine($"[TRIM] Too many smileys ({_smileyFilenames.Count}) for placeholders ({embeddedCount}) - WOULD remove extras but disabled for debugging");
                // _smileyFilenames.RemoveRange(embeddedCount, _smileyFilenames.Count - embeddedCount);
            }
        }

        public void ShowTypingIndicator(string username, bool isTyping)
        {
            if (_lblTypingIndicator == null) return;

            if (this.InvokeRequired)
            {
                this.Invoke(() => ShowTypingIndicator(username, isTyping));
                return;
            }

            if (_isBlockedByMe || _isBlockedByRemote)
            {
                _lblTypingIndicator.Visible = false;
                return;
            }

            if (isTyping)
            {
                _lblTypingIndicator.Text = $"{username} est en train d'écrire...";
                _lblTypingIndicator.Visible = true;
            }
            else
            {
                _lblTypingIndicator.Visible = false;
            }
        }

        private async void BtnSend_Click(object? sender, EventArgs e)
        {
            if (rtbInput is not RichTextBox inputBox)
            {
                return;
            }

            if (_inputLockedByDnd || _inputLockedByBlock || _isBlockedByMe || _isBlockedByRemote)
                return;

            // Check if there's content (text or images)
            string sanitizedText = inputBox.Text?.Replace("\u200B", "") ?? string.Empty;
            bool hasText = !string.IsNullOrWhiteSpace(sanitizedText);
            string rawRtf = inputBox.Rtf ?? string.Empty;
            bool hasImage = rawRtf.Contains("\\pict");
            if (!hasText && !hasImage) return;

            // Save current formatting for next message
            // Capture from the END of the text if possible, or current selection
            var len = inputBox.TextLength;
            if (len > 0)
            {
                inputBox.Select(len - 1, 1);
                var endFont = inputBox.SelectionFont;
                var endColor = inputBox.SelectionColor;
                
                if (endFont != null) _lastUsedFont = endFont;
                if (!endColor.IsEmpty) _lastUsedColor = endColor;
            }
            else
            {
                var selectionFont = inputBox.SelectionFont;
                if (selectionFont != null) _lastUsedFont = selectionFont;
                _lastUsedColor = inputBox.SelectionColor;
            }

            // Use RTF directly without metadata embedding
            string rtfContent = rawRtf;
            
            // Embed smiley filenames in the content to persist them in DB (since DB doesn't have a SmileyFilenames column)
            if (_smileyFilenames.Count > 0)
            {
                rtfContent += "|||SMILEYS:" + string.Join("|", _smileyFilenames);
            }

            var msg = new ChatMessageDto
            {
                SenderId = _currentUser.Id,
                SenderName = !string.IsNullOrWhiteSpace(_currentUser.FirstName) 
                    ? $"{_currentUser.FirstName} {_currentUser.LastName}" 
                    : _currentUser.Username,
                ReceiverId = _recipient.Id,
                Content = rtfContent, // Send RTF with embedded smiley metadata
                ContentType = "RTF",
                Timestamp = DateTime.UtcNow,
                SmileyFilenames = new List<string>(_smileyFilenames) // Keep this for live updates if needed
            };

            System.Diagnostics.Debug.WriteLine($"[SEND] Sending message with {_smileyFilenames.Count} smileys: {string.Join(", ", _smileyFilenames)}");

            // DO NOT display locally - wait for server confirmation via MessageSaved
            
            // Send via SignalR
            try
            {
                await _mainForm.SendPrivateMessage(msg);
                
                // Stop typing indicator
                _typingTimer?.Dispose();
                if (_isCurrentlyTyping)
                {
                    _isCurrentlyTyping = false;
                    await _mainForm.NotifyTyping(_recipient.Id, false);
                }
                
                // Clear input and smiley map
                inputBox.Clear();
                _smileyFilenames.Clear();

                // Restore formatting for next message
                if (_lastUsedFont != null)
                {
                    inputBox.SelectionFont = _lastUsedFont;
                }
                inputBox.SelectionColor = _lastUsedColor;
                
                // Restore last used formatting
                if (_lastUsedFont != null)
                {
                    inputBox.SelectionFont = _lastUsedFont;
                }
                else
                {
                    inputBox.SelectionFont = new Font("Segoe UI", 10F, FontStyle.Regular);
                }
                inputBox.SelectionColor = _lastUsedColor;
            }
            catch (Exception ex)
            {
                PalMessageBox.Show("Erreur lors de l'envoi: " + ex.Message);
            }
        }

        private async Task SendAttachmentAsync()
        {
            if (_inputLockedByDnd || _inputLockedByBlock || _isBlockedByMe || _isBlockedByRemote)
                return;

            using var ofd = new OpenFileDialog
            {
                Filter = "Tous les fichiers|*.*|Audio|*.mp3;*.wav;*.ogg;*.flac;*.m4a;*.aac",
                Multiselect = false,
                Title = "Envoyer un fichier"
            };

            if (ofd.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            var filePath = ofd.FileName;
            if (!File.Exists(filePath))
            {
                PalMessageBox.Show("Fichier introuvable.", "Fichier", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var fileInfo = new FileInfo(filePath);
            var ext = fileInfo.Extension.ToLowerInvariant();
            var isAudio = IsAudioExtension(ext);
            var maxBytes = isAudio ? 10 * 1024 * 1024 : 12 * 1024 * 1024; // 10 Mo audio, 12 Mo autres

            if (fileInfo.Length > maxBytes)
            {
                PalMessageBox.Show(isAudio
                        ? "Fichier audio trop volumineux (max 10 Mo)."
                        : "Fichier trop volumineux (max 12 Mo).",
                    "Envoi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(filePath);
            }
            catch (Exception ex)
            {
                PalMessageBox.Show($"Lecture impossible : {ex.Message}", "Envoi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var mime = GetAttachmentMimeFromExtension(ext, isAudio);

            var msgTimestamp = DateTime.UtcNow;
            var senderName = !string.IsNullOrWhiteSpace(_currentUser.FirstName)
                ? $"{_currentUser.FirstName} {_currentUser.LastName}"
                : _currentUser.Username;

            // placeholder content uses local file url to render badge while uploading
            var tempId = Guid.NewGuid().ToString("N");
            var placeholderContent = $"file://{fileInfo.FullName}";
            var contentType = isAudio ? "Audio" : "File";

            var control = new ChatMessageControl(0, _currentUser.Id, senderName, placeholderContent, msgTimestamp, isIncoming: false, isEdited: false, contentType, null, ChatMessageControl.SendState.Sending, true);
            control.SetSendProgress(5);
            control.RetryRequested += async (_, __) => await SendAttachmentUploadFlowAsync(fileInfo, mime, isAudio, control, senderName, tempId);

            flpHistory.Controls.Add(control);
            flpHistory.ScrollControlIntoView(control);

            var pending = new PendingAttachment(control, bytes, fileInfo.Name, mime, isAudio, tempId);
            _pendingAttachments[fileInfo.FullName] = pending;
            _pendingAttachmentsByTempId[tempId] = pending;
            _pendingByTempId[tempId] = control;

            await SendAttachmentUploadFlowAsync(fileInfo, mime, isAudio, control, senderName, tempId);
        }

        private async Task SendMediaAsync()
        {
            if (_inputLockedByDnd || _inputLockedByBlock || _isBlockedByMe || _isBlockedByRemote)
                return;

            using var ofd = new OpenFileDialog
            {
                Filter = "Images/Vidéos|*.png;*.jpg;*.jpeg;*.gif;*.mp4;*.mov;*.avi;*.webm|Images|*.png;*.jpg;*.jpeg;*.gif|Vidéos|*.mp4;*.mov;*.avi;*.webm",
                Multiselect = false,
                Title = "Envoyer une image ou une vidéo"
            };

            if (ofd.ShowDialog() != DialogResult.OK)
            {
                return; // No selection
            }

            var filePath = ofd.FileName;
            if (!File.Exists(filePath))
            {
                PalMessageBox.Show("Fichier introuvable.", "Image", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var fileInfo = new FileInfo(filePath);
            var ext = fileInfo.Extension.ToLowerInvariant();

            bool isImage = ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".gif";
            bool isVideo = ext == ".mp4" || ext == ".mov" || ext == ".avi" || ext == ".webm";

            if (!isImage && !isVideo)
            {
                PalMessageBox.Show("Format non supporté.", "Envoi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (isImage)
            {
                const long maxBytes = 5 * 1024 * 1024; // 5 MB
                if (fileInfo.Length > maxBytes)
                {
                    PalMessageBox.Show("Fichier trop volumineux (max 5 Mo).", "Image", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                try
                {
                    var originalBytes = await File.ReadAllBytesAsync(filePath);
                    using var original = Image.FromFile(filePath);

                    // Small preview for inline bubble; upload uses the full original bytes
                    var previewBytes = PreparePngPayload(original, maxPixelSize: 800, maxBytes: 220_000);
                    var dataUri = BuildImageDataUri(previewBytes);

                    var msgTimestamp = DateTime.UtcNow;
                    var senderName = !string.IsNullOrWhiteSpace(_currentUser.FirstName)
                        ? $"{_currentUser.FirstName} {_currentUser.LastName}"
                        : _currentUser.Username;

                    var tempId = Guid.NewGuid().ToString("N");
                    var mime = GetAttachmentMimeFromExtension(ext, isAudio: false);

                    var control = new ChatMessageControl(0, _currentUser.Id, senderName, dataUri, msgTimestamp, isIncoming: false, isEdited: false, "Image", null, ChatMessageControl.SendState.Sending, true);
                    control.SetSendProgress(10);
                    control.RetryRequested += async (_, __) => await RetrySendAttachmentAsync(dataUri, originalBytes, Path.GetFileName(filePath), mime, false, control, tempId);

                    flpHistory.Controls.Add(control);
                    flpHistory.ScrollControlIntoView(control);

                    var pending = new PendingAttachment(control, originalBytes, Path.GetFileName(filePath), mime, false, tempId);
                    _pendingAttachments[fileInfo.FullName] = pending;
                    _pendingAttachmentsByTempId[tempId] = pending;
                    _pendingByTempId[tempId] = control;

                    await SendAttachmentUploadFlowAsync(fileInfo, mime, isAudio: false, control, senderName, tempId, contentTypeOverride: "Image");
                }
                catch (Exception ex)
                {
                    PalMessageBox.Show($"Impossible d'envoyer cette image : {ex.Message}", "Image", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            // Video branch
            // Hard guard: inline SignalR payloads are limited; keep a conservative inline cap to avoid disconnects
            const long maxVideoBytes = 20 * 1024 * 1024; // desired cap
            const long maxInlineVideoBytes = 1 * 1024 * 1024; // ~1 MB raw (~1.4 MB base64)
            if (fileInfo.Length > maxVideoBytes)
            {
                PalMessageBox.Show("Vidéo trop volumineuse (max 20 Mo).", "Vidéo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var mimeSubtype = ext switch
            {
                ".mp4" => "mp4",
                ".mov" => "quicktime",
                ".avi" => "x-msvideo",
                ".webm" => "webm",
                _ => "mp4"
            };

            if (fileInfo.Length > maxInlineVideoBytes)
            {
                await SendVideoViaUploadAsync(fileInfo, mimeSubtype);
                return;
            }

            try
            {
                var videoBytes = File.ReadAllBytes(filePath);
                var base64 = Convert.ToBase64String(videoBytes);
                // Extra guard to avoid server disconnect on oversized frames
                if (base64.Length > 1_400_000) // ~1.4 MB base64 for ~1 MB raw
                {
                    PalMessageBox.Show("Cette vidéo dépasse la taille autorisée pour l'envoi direct (~1 Mo).", "Vidéo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var dataUri = $"data:video/{mimeSubtype};base64,{base64}";
                var tempId = Guid.NewGuid().ToString("N");

                var msg = new ChatMessageDto
                {
                    SenderId = _currentUser.Id,
                    SenderName = !string.IsNullOrWhiteSpace(_currentUser.FirstName)
                        ? $"{_currentUser.FirstName} {_currentUser.LastName}"
                        : _currentUser.Username,
                    ReceiverId = _recipient.Id,
                    Content = dataUri,
                    ContentType = "Video",
                    Timestamp = DateTime.UtcNow,
                    ClientTempId = tempId
                };

                var control = new ChatMessageControl(0, _currentUser.Id, msg.SenderName, msg.Content, msg.Timestamp, isIncoming: false, isEdited: false, msg.ContentType, null, ChatMessageControl.SendState.Sending, true);
                control.SetSendProgress(10);
                control.RetryRequested += async (_, __) => await RetrySendVideoAsync(dataUri, videoBytes, mimeSubtype, control, tempId);

                flpHistory.Controls.Add(control);
                flpHistory.ScrollControlIntoView(control);

                _pendingVideoMessages[dataUri] = (control, videoBytes, tempId);
                _pendingVideoMessagesByTempId[tempId] = (dataUri, control);
                _pendingByTempId[tempId] = control;

                await SendVideoPayloadAsync(msg, control);
            }
            catch (Exception ex)
            {
                PalMessageBox.Show($"Impossible d'envoyer cette vidéo : {ex.Message}", "Vidéo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task SendImagePayloadAsync(ChatMessageDto msg, ChatMessageControl control)
        {
            try
            {
                control.SetSendProgress(35);
                await _mainForm.SendPrivateMessage(msg);
                control.SetSendProgress(100);
                control.MarkSendSuccess();
            }
            catch (Exception ex)
            {
                control.MarkSendError(ex.Message);
                PalMessageBox.Show($"Impossible d'envoyer cette image : {ex.Message}", "Image", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task RetrySendImageAsync(string dataUri, byte[] payload, ChatMessageControl control, string? tempIdOverride = null)
        {
            var tempId = tempIdOverride ?? Guid.NewGuid().ToString("N");
            if (_pendingImageMessages.ContainsKey(dataUri))
            {
                control.SetSendProgress(10);
            }
            else
            {
                _pendingImageMessages[dataUri] = (control, payload, tempId);
                _pendingImageMessagesByTempId[tempId] = (dataUri, control);
                _pendingByTempId[tempId] = control;
                control.SetSendProgress(10);
            }

            var msg = new ChatMessageDto
            {
                SenderId = _currentUser.Id,
                SenderName = !string.IsNullOrWhiteSpace(_currentUser.FirstName)
                    ? $"{_currentUser.FirstName} {_currentUser.LastName}"
                    : _currentUser.Username,
                ReceiverId = _recipient.Id,
                Content = dataUri,
                ContentType = "Image",
                Timestamp = DateTime.UtcNow,
                ClientTempId = tempId
            };

            await SendImagePayloadAsync(msg, control);
        }

        private async Task SendVideoPayloadAsync(ChatMessageDto msg, ChatMessageControl control)
        {
            try
            {
                control.SetSendProgress(35);
                await _mainForm.SendPrivateMessage(msg);
                control.SetSendProgress(100);
                control.MarkSendSuccess();
            }
            catch (Exception ex)
            {
                control.MarkSendError(ex.Message);
                PalMessageBox.Show($"Impossible d'envoyer cette vidéo : {ex.Message}", "Vidéo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task RetrySendVideoAsync(string dataUri, byte[] payload, string mime, ChatMessageControl control, string tempId)
        {
            if (!_pendingVideoMessages.ContainsKey(dataUri))
            {
                _pendingVideoMessages[dataUri] = (control, payload, tempId);
                _pendingVideoMessagesByTempId[tempId] = (dataUri, control);
                _pendingByTempId[tempId] = control;
            }
            control.SetSendProgress(10);

            var msg = new ChatMessageDto
            {
                SenderId = _currentUser.Id,
                SenderName = !string.IsNullOrWhiteSpace(_currentUser.FirstName)
                    ? $"{_currentUser.FirstName} {_currentUser.LastName}"
                    : _currentUser.Username,
                ReceiverId = _recipient.Id,
                Content = dataUri,
                ContentType = "Video",
                Timestamp = DateTime.UtcNow,
                ClientTempId = tempId
            };

            await SendVideoPayloadAsync(msg, control);
        }

        private async Task CaptureAndSendScreenshotAsync()
        {
            if (_inputLockedByDnd || _inputLockedByBlock || _isBlockedByMe || _isBlockedByRemote)
                return;

            var sendIcon = ResourceImageStore.LoadImage("various/SendScreen.png", new Size(24, 24));
            var editIcon = ResourceImageStore.LoadImage("various/EditScreen.png", new Size(24, 24));
            var cancelIcon = ResourceImageStore.LoadImage("various/Cancel.png", new Size(24, 24));

            while (true)
            {
                Rectangle region;
                using (var selection = new ScreenshotSelectionForm())
                {
                    var selectionResult = selection.ShowDialog(this);
                    if (selectionResult != DialogResult.OK)
                    {
                        return;
                    }

                    region = selection.SelectedRegion;
                }

                Image? captured = null;
                try
                {
                    captured = CaptureScreenRegion(region);
                }
                catch (Exception ex)
                {
                    PalMessageBox.Show($"Impossible de capturer l'écran : {ex.Message}", "Capture", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                ScreenshotPreviewAction action = ScreenshotPreviewAction.None;
                Image? imageToSend = null;

                using (captured)
                using (var preview = new ScreenshotPreviewForm(captured, sendIcon, editIcon, cancelIcon))
                {
                    preview.ShowDialog(this);
                    action = preview.ActionResult;

                    if (action == ScreenshotPreviewAction.Send)
                    {
                        imageToSend = (Image)preview.ResultImage.Clone();
                    }
                }

                if (action == ScreenshotPreviewAction.Edit)
                {
                    imageToSend?.Dispose();
                    continue; // relaunch selection
                }

                if (action != ScreenshotPreviewAction.Send || imageToSend == null)
                {
                    imageToSend?.Dispose();
                    return; // Cancel or closed
                }

                using (imageToSend)
                {
                    await SendScreenshotImageAsync(imageToSend);
                }

                return;
            }
        }

        private static Bitmap CaptureScreenRegion(Rectangle region)
        {
            if (region.Width <= 0 || region.Height <= 0)
            {
                throw new ArgumentException("Zone de capture invalide.");
            }

            var bmp = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            g.CopyFromScreen(region.Location, Point.Empty, region.Size, CopyPixelOperation.SourceCopy);
            return bmp;
        }

        private async Task SendScreenshotImageAsync(Image screenshot)
        {
            try
            {
                var fullPngBytes = SaveAsPngBytesInternal(screenshot);
                const long maxBytes = 8 * 1024 * 1024; // 8 MB guard for captures
                if (fullPngBytes.Length > maxBytes)
                {
                    PalMessageBox.Show("Capture trop volumineuse (> 8 Mo). Réduisez la zone ou la résolution.", "Capture", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Lightweight preview for inline bubble
                var previewBytes = PreparePngPayload(screenshot, maxPixelSize: 900, maxBytes: 260_000);
                var dataUri = BuildImageDataUri(previewBytes);

                var msgTimestamp = DateTime.UtcNow;
                var senderName = !string.IsNullOrWhiteSpace(_currentUser.FirstName)
                    ? $"{_currentUser.FirstName} {_currentUser.LastName}"
                    : _currentUser.Username;

                var tempId = Guid.NewGuid().ToString("N");
                var tempPath = Path.Combine(Path.GetTempPath(), $"palx_capture_{tempId}.png");
                await File.WriteAllBytesAsync(tempPath, fullPngBytes);
                var tempInfo = new FileInfo(tempPath);

                var control = new ChatMessageControl(0, _currentUser.Id, senderName, dataUri, msgTimestamp, isIncoming: false, isEdited: false, "Image", null, ChatMessageControl.SendState.Sending, true);
                control.SetSendProgress(10);
                control.RetryRequested += async (_, __) => await RetrySendAttachmentAsync(dataUri, fullPngBytes, "capture.png", "image/png", false, control, tempId);

                flpHistory.Controls.Add(control);
                flpHistory.ScrollControlIntoView(control);

                var pending = new PendingAttachment(control, fullPngBytes, "capture.png", "image/png", false, tempId);
                _pendingAttachments[tempInfo.FullName] = pending;
                _pendingAttachmentsByTempId[tempId] = pending;
                _pendingByTempId[tempId] = control;

                try
                {
                    await SendAttachmentUploadFlowAsync(tempInfo, "image/png", isAudio: false, control, senderName, tempId, contentTypeOverride: "Image");
                }
                finally
                {
                    TryDeleteFile(tempInfo.FullName);
                }
            }
            catch (Exception ex)
            {
                PalMessageBox.Show($"Impossible d'envoyer la capture : {ex.Message}", "Capture", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task SendAttachmentPayloadAsync(ChatMessageDto msg, ChatMessageControl control)
        {
            try
            {
                control.SetSendProgress(35);
                await _mainForm.SendPrivateMessage(msg);
                control.SetSendProgress(100);
                control.MarkSendSuccess();
            }
            catch (Exception ex)
            {
                control.MarkSendError(ex.Message);
                PalMessageBox.Show($"Impossible d'envoyer ce fichier : {ex.Message}", "Envoi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    private async Task RetrySendAttachmentAsync(string dataUri, byte[] payload, string fileName, string mime, bool isAudio, ChatMessageControl control, string tempId)
        {
            // on retry, rebuild fileinfo from temp copy
            var ext = Path.GetExtension(fileName);
            var tempPath = Path.Combine(Path.GetTempPath(), $"palx_retry_{Guid.NewGuid():N}{(string.IsNullOrWhiteSpace(ext) ? (isAudio ? ".mp3" : ".bin") : ext)}");
            await File.WriteAllBytesAsync(tempPath, payload);
            var fileInfo = new FileInfo(tempPath);

        var pending = new PendingAttachment(control, payload, fileName, mime, isAudio, tempId);
        _pendingAttachments[fileInfo.FullName] = pending;
        _pendingAttachmentsByTempId[tempId] = pending;
        _pendingByTempId[tempId] = control;
            control.SetSendProgress(10);

        await SendAttachmentUploadFlowAsync(fileInfo, mime, isAudio, control, msgSenderName: !string.IsNullOrWhiteSpace(_currentUser.FirstName)
                    ? $"{_currentUser.FirstName} {_currentUser.LastName}"
            : _currentUser.Username, tempId);
        }

        private async Task SendVideoViaUploadAsync(FileInfo fileInfo, string mimeSubtype)
        {
            var msgTimestamp = DateTime.UtcNow;
            var senderName = !string.IsNullOrWhiteSpace(_currentUser.FirstName)
                ? $"{_currentUser.FirstName} {_currentUser.LastName}"
                : _currentUser.Username;

            var tempId = Guid.NewGuid().ToString("N");
            var placeholderContent = $"file://{fileInfo.FullName}";
            var control = new ChatMessageControl(0, _currentUser.Id, senderName, placeholderContent, msgTimestamp, isIncoming: false, isEdited: false, "Video", null, ChatMessageControl.SendState.Sending, true);
            control.SetSendProgress(5);
            control.RetryRequested += async (_, __) => await SendVideoUploadFlowAsync(fileInfo, mimeSubtype, control, senderName, tempId);

            flpHistory.Controls.Add(control);
            flpHistory.ScrollControlIntoView(control);

            _pendingVideoUploads[fileInfo.FullName] = (control, tempId);
            var pending = new PendingAttachment(control, Array.Empty<byte>(), fileInfo.Name, $"video/{mimeSubtype}", false, tempId);
            _pendingAttachments[fileInfo.FullName] = pending;
            _pendingAttachmentsByTempId[tempId] = pending;
            _pendingByTempId[tempId] = control;

            await SendVideoUploadFlowAsync(fileInfo, mimeSubtype, control, senderName, tempId);
        }

        private async Task SendVideoUploadFlowAsync(FileInfo fileInfo, string mimeSubtype, ChatMessageControl control, string senderName, string tempId)
        {
            try
            {
                var httpClient = _mainForm.GetHttpClient();
                var apiBase = _mainForm.GetApiBaseUrl();

                using var fs = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                var total = Math.Max(1, fs.Length);
                var progressContent = new ProgressableStreamContent(fs, 64 * 1024, sent =>
                {
                    var percent = (int)Math.Min(95, Math.Round(((double)sent / total) * 90.0) + 5);
                    UpdateSendProgressSafe(control, percent);
                });
                progressContent.Headers.ContentType = new MediaTypeHeaderValue($"video/{mimeSubtype}");

                using var multipart = new MultipartFormDataContent();
                multipart.Add(progressContent, "file", fileInfo.Name);

                UpdateSendProgressSafe(control, 10);
                var response = await httpClient.PostAsync($"{apiBase}/media/upload", multipart);
                var uploadResult = await response.Content.ReadFromJsonAsync<UploadResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (!response.IsSuccessStatusCode || uploadResult == null || string.IsNullOrWhiteSpace(uploadResult.Url))
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    throw new Exception(string.IsNullOrWhiteSpace(errorBody) ? "Réponse upload invalide." : errorBody);
                }

                UpdateSendProgressSafe(control, 96);

                var msg = new ChatMessageDto
                {
                    SenderId = _currentUser.Id,
                    SenderName = senderName,
                    ReceiverId = _recipient.Id,
                    Content = uploadResult.Url,
                    ContentType = "Video",
                    Timestamp = DateTime.UtcNow,
                    FileName = uploadResult.OriginalName ?? uploadResult.FileName,
                    FileSizeBytes = uploadResult.Size,
                    DurationSeconds = null,
                    ClientTempId = tempId
                };

                await _mainForm.SendPrivateMessage(msg);
                control.UpdateVideoSource(uploadResult.Url);
                UpdateSendProgressSafe(control, 100);
                control.MarkSendSuccess();

                _pendingVideoUploads.Remove(fileInfo.FullName);
            }
            catch (Exception ex)
            {
                control.MarkSendError(ex.Message);
                PalMessageBox.Show($"Impossible d'envoyer cette vidéo (upload) : {ex.Message}", "Vidéo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task SendAttachmentUploadFlowAsync(FileInfo fileInfo, string mime, bool isAudio, ChatMessageControl control, string msgSenderName, string tempId, string? contentTypeOverride = null)
        {
            try
            {
                var httpClient = _mainForm.GetHttpClient();
                var apiBase = _mainForm.GetApiBaseUrl();

                using var fs = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                var total = Math.Max(1, fs.Length);
                var progressContent = new ProgressableStreamContent(fs, 64 * 1024, sent =>
                {
                    var percent = (int)Math.Min(95, Math.Round(((double)sent / total) * 90.0) + 5);
                    UpdateSendProgressSafe(control, percent);
                });
                progressContent.Headers.ContentType = new MediaTypeHeaderValue(mime);

                using var multipart = new MultipartFormDataContent();
                multipart.Add(progressContent, "file", fileInfo.Name);

                UpdateSendProgressSafe(control, 10);
                var response = await httpClient.PostAsync($"{apiBase}/media/upload", multipart);
                var uploadResult = await response.Content.ReadFromJsonAsync<UploadResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (!response.IsSuccessStatusCode || uploadResult == null || string.IsNullOrWhiteSpace(uploadResult.Url))
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    throw new Exception(string.IsNullOrWhiteSpace(errorBody) ? "Réponse upload invalide." : errorBody);
                }

                var msg = new ChatMessageDto
                {
                    SenderId = _currentUser.Id,
                    SenderName = msgSenderName,
                    ReceiverId = _recipient.Id,
                    Content = uploadResult.Url,
                    ContentType = contentTypeOverride ?? (isAudio ? "Audio" : "File"),
                    Timestamp = DateTime.UtcNow,
                    FileName = uploadResult.OriginalName ?? uploadResult.FileName ?? fileInfo.Name,
                    FileSizeBytes = uploadResult.Size,
                    DurationSeconds = contentTypeOverride?.Equals("Voice", StringComparison.OrdinalIgnoreCase) == true ? _lastVoiceDurationSeconds : null,
                    ClientTempId = tempId
                };

                UpdateSendProgressSafe(control, 98);
                await _mainForm.SendPrivateMessage(msg);
                UpdateSendProgressSafe(control, 100);
                control.MarkSendSuccess();

                if (_pendingAttachments.TryGetValue(fileInfo.FullName, out var pending))
                {
                    _pendingAttachments.Remove(fileInfo.FullName);
                    _pendingAttachments[uploadResult.Url] = pending;
                }
            }
            catch (Exception ex)
            {
                control.MarkSendError(ex.Message);
                PalMessageBox.Show($"Impossible d'envoyer ce fichier : {ex.Message}", "Envoi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateSendProgressSafe(ChatMessageControl control, int percent)
        {
            if (control.IsDisposed) return;

            var now = DateTime.UtcNow;
            if (_progressCache.TryGetValue(control, out var entry))
            {
                if (percent <= entry.lastPercent) return;
                if ((now - entry.lastUpdate).TotalMilliseconds < 80 && percent - entry.lastPercent < 2) return;
            }

            if (control.InvokeRequired)
            {
                control.Invoke(new Action(() => control.SetSendProgress(percent)));
            }
            else
            {
                control.SetSendProgress(percent);
            }

            _progressCache[control] = (now, percent);
        }

    private record UploadResponse(string Url, string FileName, long Size, string ContentType, string? OriginalName);

        private class ProgressableStreamContent : HttpContent
        {
            private readonly Stream _source;
            private readonly int _bufferSize;
            private readonly Action<long> _progress;

            public ProgressableStreamContent(Stream source, int bufferSize, Action<long> progress)
            {
                _source = source;
                _bufferSize = bufferSize;
                _progress = progress;

                if (source.CanSeek)
                {
                    Headers.ContentLength = source.Length;
                }
            }

            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            {
                var buffer = new byte[_bufferSize];
                int read;
                long total = 0;
                while ((read = await _source.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await stream.WriteAsync(buffer, 0, read);
                    total += read;
                    _progress(total);
                }
            }

            protected override bool TryComputeLength(out long length)
            {
                if (_source.CanSeek)
                {
                    length = _source.Length;
                    return true;
                }

                length = -1;
                return false;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _source.Dispose();
                }

                base.Dispose(disposing);
            }
        }

        private static async Task ConvertWavToMp3Async(string wavPath, string mp3Path)
        {
            await Task.Run(() =>
            {
                using var reader = new WaveFileReader(wavPath);
                MediaFoundationApi.Startup();
                try
                {
                    MediaFoundationEncoder.EncodeToMp3(reader, mp3Path, 128000);
                }
                finally
                {
                    MediaFoundationApi.Shutdown();
                }
            });
        }

        private static Bitmap ResizeImage(Image source, int maxWidth, int maxHeight)
        {
            var ratio = Math.Min((double)maxWidth / source.Width, (double)maxHeight / source.Height);
            ratio = Math.Min(1.0, ratio);

            var width = Math.Max(1, (int)Math.Round(source.Width * ratio));
            var height = Math.Max(1, (int)Math.Round(source.Height * ratio));

            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);
            g.DrawImage(source, new Rectangle(0, 0, width, height));
            return bmp;
        }

        private static byte[] PreparePngPayload(Image original, int maxPixelSize, int maxBytes)
        {
            // Start with a bounded resize
            var working = ResizeImage(original, maxPixelSize, maxPixelSize);
            byte[] bestBytes = SaveAsPngBytesInternal(working);

            int stepSize = Math.Max(64, maxPixelSize / 2);
            try
            {
                while (bestBytes.Length > maxBytes && stepSize >= 64)
                {
                    working.Dispose();
                    working = ResizeImage(original, stepSize, stepSize);
                    var candidate = SaveAsPngBytesInternal(working);
                    if (candidate.Length < bestBytes.Length)
                    {
                        bestBytes = candidate;
                    }
                    stepSize = (int)(stepSize * 0.75);
                }

                return bestBytes;
            }
            finally
            {
                working.Dispose();
            }
        }

        private static byte[] SaveAsPngBytesInternal(Image image)
        {
            using var ms = new MemoryStream();
            image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();
        }

        private static string BuildImageDataUri(byte[] pngBytes)
        {
            return $"data:image/png;base64,{Convert.ToBase64String(pngBytes)}";
        }

        private static string BuildAttachmentDataUri(byte[] data, string mime, string fileName)
        {
            var safeName = Uri.EscapeDataString(fileName);
            return $"data:{mime};name={safeName};base64,{Convert.ToBase64String(data)}";
        }

        private static bool IsAudioExtension(string ext)
        {
            if (string.IsNullOrWhiteSpace(ext)) return false;
            ext = ext.ToLowerInvariant();
            return ext is ".mp3" or ".wav" or ".ogg" or ".flac" or ".m4a" or ".aac";
        }

        private static string GetAttachmentMimeFromExtension(string ext, bool isAudio)
        {
            ext = (ext ?? string.Empty).ToLowerInvariant();
            return ext switch
            {
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".ogg" => "audio/ogg",
                ".flac" => "audio/flac",
                ".m4a" => "audio/mp4",
                ".aac" => "audio/aac",
                ".pdf" => "application/pdf",
                ".zip" => "application/zip",
                ".txt" => "text/plain",
                ".json" => "application/json",
                ".csv" => "text/csv",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => isAudio ? "audio/mpeg" : "application/octet-stream"
            };
        }

        public void AppendMessage(ChatMessageDto msg, bool isMe, bool autoScroll = true)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<ChatMessageDto, bool, bool>(AppendMessage), msg, isMe, autoScroll);
                return;
            }

            // First try temp-id based completion to reuse the pending bubble created client-side
            if (isMe && TryCompleteByTempId(msg))
            {
                return;
            }

            // If this is an image we sent and we already show a pending bubble, update it instead of duplicating
            if (isMe && IsImageMessage(msg) && TryCompletePendingImage(msg))
            {
                return;
            }
            if (isMe && IsVideoMessage(msg) && TryCompletePendingVideo(msg))
            {
                return;
            }

            // Standard attachment completion path (file/audio/voice/image)
            if (isMe && (IsFileMessage(msg) || IsAudioMessage(msg) || IsVoiceMessage(msg) || IsImageMessage(msg)) && TryCompletePendingAttachment(msg))
            {
                return;
            }

            // Fallback: even if the content type is missing/incorrect, try to match any pending attachment by URL/file name to avoid duplicate bubbles (esp. voice)
            if (isMe && TryCompletePendingAttachment(msg))
            {
                return;
            }

            if (!RegisterLoadedMessage(msg))
            {
                return;
            }

            var msgControl = CreateMessageControl(msg, isMe);
            flpHistory.Controls.Add(msgControl);
            
            // Add to WebView
            AddMessageToWebView(msg, isMe);

            UpdateDeleteHistoryButtonIcon();
            if (autoScroll)
            {
                flpHistory.ScrollControlIntoView(msgControl);
            }
        }

        private static bool IsImageMessage(ChatMessageDto msg)
        {
            var type = msg.ContentType?.Trim().ToLowerInvariant();
            return type == "image" || type == "gif";
        }

        private static bool IsVideoMessage(ChatMessageDto msg)
        {
            var type = msg.ContentType?.Trim().ToLowerInvariant();
            return type == "video";
        }

        private static bool IsFileMessage(ChatMessageDto msg)
        {
            var type = msg.ContentType?.Trim().ToLowerInvariant();
            return type == "file";
        }

        private static bool IsAudioMessage(ChatMessageDto msg)
        {
            var type = msg.ContentType?.Trim().ToLowerInvariant();
            return type == "audio";
        }

        private static bool IsVoiceMessage(ChatMessageDto msg)
        {
            var type = msg.ContentType?.Trim().ToLowerInvariant();
            return type == "voice";
        }

        private bool TryCompletePendingImage(ChatMessageDto msg)
        {
            if (!string.IsNullOrWhiteSpace(msg.ClientTempId))
            {
                if (_completedTempIds.Contains(msg.ClientTempId)) return true;
                if (_pendingImageMessagesByTempId.TryGetValue(msg.ClientTempId, out var hit))
                {
                    var control = hit.control;
                    ApplyPendingCompletion(control, msg);
                    _pendingImageMessagesByTempId.Remove(msg.ClientTempId);
                    _pendingImageMessages.Remove(hit.contentKey);
                    return true;
                }
            }

            if (!_pendingImageMessages.TryGetValue(msg.Content, out var pending))
            {
                return false;
            }

            ApplyPendingCompletion(pending.control, msg);
            _pendingImageMessages.Remove(msg.Content);
            if (!string.IsNullOrWhiteSpace(pending.tempId))
            {
                _pendingImageMessagesByTempId.Remove(pending.tempId);
            }
            return true;
        }

        private bool TryCompletePendingVideo(ChatMessageDto msg)
        {
            if (!string.IsNullOrWhiteSpace(msg.ClientTempId))
            {
                if (_completedTempIds.Contains(msg.ClientTempId)) return true;
                if (_pendingVideoMessagesByTempId.TryGetValue(msg.ClientTempId, out var hit))
                {
                    ApplyPendingCompletion(hit.control, msg);
                    _pendingVideoMessagesByTempId.Remove(msg.ClientTempId);
                    _pendingVideoMessages.Remove(hit.contentKey);
                    return true;
                }
            }

            if (!_pendingVideoMessages.TryGetValue(msg.Content, out var pending))
            {
                return false;
            }

            ApplyPendingCompletion(pending.control, msg);
            _pendingVideoMessages.Remove(msg.Content);
            if (!string.IsNullOrWhiteSpace(pending.tempId))
            {
                _pendingVideoMessagesByTempId.Remove(pending.tempId);
            }
            return true;
        }

        private void ApplyPendingCompletion(ChatMessageControl control, ChatMessageDto msg)
        {
            control.MessageId = msg.MessageId;
            control.UpdateTimestamp(msg.Timestamp);
            control.MarkSendSuccess();

            if (!string.IsNullOrWhiteSpace(msg.ClientTempId))
            {
                _completedTempIds.Add(msg.ClientTempId);
                _pendingByTempId.Remove(msg.ClientTempId);
            }
        }

        private bool TryCompletePendingAttachment(ChatMessageDto msg)
        {
            void UpdateAttachmentControl(PendingAttachment pending)
            {
                var isVoice = IsVoiceMessage(msg);
                var isAudio = isVoice || IsAudioMessage(msg) || pending.IsAudio;
                pending.Control.UpdateAttachmentSource(
                    msg.Content,
                    pending.Payload,
                    msg.FileName ?? pending.FileName,
                    msg.ContentType ?? pending.Mime,
                    isAudio,
                    isVoice);
            }

            if (!string.IsNullOrWhiteSpace(msg.ClientTempId))
            {
                if (_completedTempIds.Contains(msg.ClientTempId)) return true;
                if (_pendingAttachmentsByTempId.TryGetValue(msg.ClientTempId, out var pendingByTemp))
                {
                    UpdateAttachmentControl(pendingByTemp);
                    ApplyPendingCompletion(pendingByTemp.Control, msg);
                    if (!string.IsNullOrWhiteSpace(msg.ClientTempId))
                    {
                        _pendingAttachmentsByTempId.Remove(msg.ClientTempId);
                    }
                    RemovePendingAttachmentContentEntries(pendingByTemp, msg.Content);
                    return true;
                }
            }

            if (!string.IsNullOrWhiteSpace(msg.Content) && _pendingAttachments.TryGetValue(msg.Content, out var pending))
            {
                UpdateAttachmentControl(pending);
                ApplyPendingCompletion(pending.Control, msg);
                _pendingAttachments.Remove(msg.Content);
                if (!string.IsNullOrWhiteSpace(pending.TempId))
                {
                    _pendingAttachmentsByTempId.Remove(pending.TempId);
                }
                return true;
            }

            // Fallback: some backends may return a different URL casing or add query params – align by file name
            string? contentFileName = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(msg.Content))
                {
                    if (Uri.TryCreate(msg.Content, UriKind.Absolute, out var uri))
                    {
                        contentFileName = Path.GetFileName(uri.AbsolutePath);
                    }
                    else
                    {
                        contentFileName = Path.GetFileName(msg.Content);
                    }
                }
            }
            catch { }

            if (string.IsNullOrWhiteSpace(contentFileName))
            {
                return false;
            }

            var fallbackMatch = _pendingAttachments.FirstOrDefault(kvp =>
                !string.IsNullOrWhiteSpace(kvp.Key) &&
                !string.IsNullOrWhiteSpace(kvp.Value.FileName) &&
                string.Equals(Path.GetFileName(kvp.Value.FileName), contentFileName, StringComparison.OrdinalIgnoreCase));

            if (fallbackMatch.Equals(default(KeyValuePair<string, PendingAttachment>)))
            {
                return false;
            }

            var matched = fallbackMatch.Value;
            if (!string.IsNullOrWhiteSpace(fallbackMatch.Key))
            {
                _pendingAttachments.Remove(fallbackMatch.Key);
            }
            UpdateAttachmentControl(matched);
            ApplyPendingCompletion(matched.Control, msg);
            if (!string.IsNullOrWhiteSpace(matched.TempId))
            {
                _pendingAttachmentsByTempId.Remove(matched.TempId);
            }
            return true;
        }

        private bool TryCompleteByTempId(ChatMessageDto msg)
        {
            if (string.IsNullOrWhiteSpace(msg.ClientTempId))
            {
                return false;
            }

            if (_completedTempIds.Contains(msg.ClientTempId))
            {
                return true;
            }

            // Check for WebView pending messages
            if (_pendingWebMessages.Contains(msg.ClientTempId))
            {
                _pendingWebMessages.Remove(msg.ClientTempId);
                return true;
            }

            if (_pendingByTempId.TryGetValue(msg.ClientTempId, out var control))
            {
                ApplyPendingCompletion(control, msg);
                _pendingByTempId.Remove(msg.ClientTempId);
                return true;
            }

            return false;
        }

    private void RemovePendingAttachmentContentEntries(PendingAttachment pending, string? ackContent)
        {
            // Remove both the original and any mapped URL keys that might point to the same pending record
            var keysToRemove = _pendingAttachments
                .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Key) && ReferenceEquals(kvp.Value.Control, pending.Control))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    _pendingAttachments.Remove(key);
                }
            }

            if (!string.IsNullOrWhiteSpace(pending.TempId))
            {
                _pendingAttachmentsByTempId.Remove(pending.TempId);
            }

            if (!string.IsNullOrWhiteSpace(ackContent))
            {
                _pendingAttachments.Remove(ackContent);
            }
        }

        public void ClearConversation()
        {
            if (flpHistory == null)
            {
                return;
            }

            flpHistory.SuspendLayout();
            try
            {
                for (int i = flpHistory.Controls.Count - 1; i >= 0; i--)
                {
                    var control = flpHistory.Controls[i];
                    if (_lnkLoadOlder != null && ReferenceEquals(control, _lnkLoadOlder))
                    {
                        continue;
                    }

                    flpHistory.Controls.RemoveAt(i);
                    control.Dispose();
                }
            }
            finally
            {
                flpHistory.ResumeLayout(true);
            }

            _loadedMessageIds.Clear();
            _oldestLoadedTimestamp = null;
            _hasMoreHistory = false;
            UpdateLoadOlderVisibility();
            UpdateDeleteHistoryButtonIcon();
        }

        private bool HasConversationContent()
        {
            if (flpHistory == null)
            {
                return false;
            }

            foreach (Control c in flpHistory.Controls)
            {
                if (_lnkLoadOlder != null && ReferenceEquals(c, _lnkLoadOlder))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private void EnsureDeleteHistoryIconsLoaded()
        {
            if (_btnDeleteHistory == null)
            {
                return;
            }

            if (_deleteHistoryEmptyIcon != null && _deleteHistoryFilledIcon != null)
            {
                return;
            }

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var emptyPath = Path.Combine(baseDir, "Assets", "Chat_Form", "Eistory_E.png");
            var filledPath = Path.Combine(baseDir, "Assets", "Chat_Form", "History_F.png");
            var size = new Size(_btnDeleteHistory.Width, _btnDeleteHistory.Height);

            _deleteHistoryEmptyIcon ??= LoadAbsoluteImage(emptyPath, size);
            _deleteHistoryFilledIcon ??= LoadAbsoluteImage(filledPath, size);
        }

        private void UpdateDeleteHistoryButtonIcon()
        {
            if (_btnDeleteHistory == null)
            {
                return;
            }

            EnsureDeleteHistoryIconsLoaded();

            bool hasContent = HasConversationContent();
            var target = hasContent ? _deleteHistoryFilledIcon : _deleteHistoryEmptyIcon;
            if (target != null)
            {
                _btnDeleteHistory.Image = target;
            }
        }

        public void UpdateMessageId(ChatMessageDto msg)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(() => UpdateMessageId(msg));
                return;
            }

            // Just append the message with the correct ID from server
            AppendMessage(msg, true);
        }

        private ChatMessageControl CreateMessageControl(ChatMessageDto msg, bool isMe)
        {
            string senderName;
            if (isMe)
            {
                senderName = !string.IsNullOrWhiteSpace(_currentUser.FirstName)
                    ? $"{_currentUser.FirstName} {_currentUser.LastName}"
                    : _currentUser.Username;
            }
            else
            {
                senderName = $"{_recipient.FirstName} {_recipient.LastName}";
            }

            // Extract embedded smileys from content if present (for history persistence)
            var content = msg.Content;
            var smileys = msg.SmileyFilenames ?? new List<string>();
            
            if (!string.IsNullOrEmpty(content) && content.Contains("|||SMILEYS:"))
            {
                var parts = content.Split(new[] { "|||SMILEYS:" }, StringSplitOptions.None);
                content = parts[0];
                if (parts.Length > 1)
                {
                    var savedSmileys = parts[1].Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    // Only add if not already present (to avoid duplication with live DTO)
                    if (smileys.Count == 0)
                    {
                        smileys.AddRange(savedSmileys);
                    }
                }
            }

            // Use new constructor with smiley tracking and content type
            return new ChatMessageControl(msg.MessageId, msg.SenderId, senderName, content, msg.Timestamp, !isMe, msg.IsEdited, msg.ContentType, smileys);
        }

        private bool RegisterLoadedMessage(ChatMessageDto msg)
        {
            if (msg.MessageId != 0)
            {
                if (!_loadedMessageIds.Add(msg.MessageId))
                {
                    return false;
                }
            }

            if (!_oldestLoadedTimestamp.HasValue || msg.Timestamp < _oldestLoadedTimestamp.Value)
            {
                _oldestLoadedTimestamp = msg.Timestamp;
            }

            return true;
        }

        private void InsertMessageControlAtTop(Control control)
        {
            if (flpHistory == null)
            {
                return;
            }

            if (_lnkLoadOlder != null && flpHistory.Controls.Contains(_lnkLoadOlder))
            {
                flpHistory.Controls.Add(control);
                flpHistory.Controls.SetChildIndex(control, 1);
            }
            else
            {
                flpHistory.Controls.Add(control);
                flpHistory.Controls.SetChildIndex(control, 0);
            }
        }

        private void UpdateLoadOlderVisibility()
        {
            if (_lnkLoadOlder == null)
            {
                return;
            }

            _lnkLoadOlder.Visible = _hasMoreHistory;
        }

        private void ScrollHistoryToBottom()
        {
            if (flpHistory == null || flpHistory.Controls.Count == 0)
            {
                return;
            }

            var last = flpHistory.Controls[flpHistory.Controls.Count - 1];
            flpHistory.ScrollControlIntoView(last);
        }

        private async Task LoadOlderMessagesAsync()
        {
            if (_isLoadingOlderHistory || !_hasMoreHistory || _mainForm == null || flpHistory == null)
            {
                return;
            }

            _isLoadingOlderHistory = true;
            if (_lnkLoadOlder != null)
            {
                _lnkLoadOlder.Enabled = false;
                _lnkLoadOlder.Text = "Chargement...";
            }

            try
            {
                var fullHistory = await _mainForm.GetChatHistory(_recipient.Id);
                if (fullHistory == null || fullHistory.Count == 0)
                {
                    _hasMoreHistory = false;
                    return;
                }

                var olderBatch = fullHistory
                    .Where(msg => (_oldestLoadedTimestamp == null || msg.Timestamp < _oldestLoadedTimestamp.Value)
                                   && (msg.MessageId == 0 || !_loadedMessageIds.Contains(msg.MessageId)))
                    .OrderByDescending(msg => msg.Timestamp)
                    .Take(InitialHistoryLimit)
                    .ToList();

                if (olderBatch.Count == 0)
                {
                    _hasMoreHistory = false;
                    return;
                }

                olderBatch.Reverse();

                flpHistory.SuspendLayout();
                try
                {
                    foreach (var msg in olderBatch)
                    {
                        bool isMe = msg.SenderId == _currentUser.Id;
                        if (!RegisterLoadedMessage(msg))
                        {
                            continue;
                        }

                        var control = CreateMessageControl(msg, isMe);
                        InsertMessageControlAtTop(control);
                    }
                }
                finally
                {
                    flpHistory.ResumeLayout(true);
                }

                _hasMoreHistory = olderBatch.Count >= InitialHistoryLimit;
            }
            catch
            {
                // Ignore history load errors for now; user can retry
            }
            finally
            {
                if (_lnkLoadOlder != null)
                {
                    _lnkLoadOlder.Enabled = true;
                    _lnkLoadOlder.Text = "Afficher l'historique précédent";
                }

                UpdateLoadOlderVisibility();
                _isLoadingOlderHistory = false;
            }
        }

        private void SetupInputContextMenu()
        {
            if (rtbInput == null)
            {
                return;
            }

            var menu = new ContextMenuStrip();
            var miCopy = new ToolStripMenuItem("Copier", CopyIcon, (_, __) => rtbInput.Copy());
            var miPaste = new ToolStripMenuItem("Coller", PasteIcon, (_, __) =>
            {
                if (Clipboard.ContainsText())
                {
                    rtbInput.Paste();
                }
            });

            menu.Opening += (_, __) =>
            {
                miCopy.Enabled = rtbInput.SelectionLength > 0;
                miPaste.Enabled = Clipboard.ContainsText();
            };

            menu.Items.Add(miCopy);
            menu.Items.Add(miPaste);
            rtbInput.ContextMenuStrip = menu;
        }

        private static Image? TryLoadIcon(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                return Image.FromFile(path);
            }
            catch
            {
                return null;
            }
        }

        private async void BtnDeleteChat_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Êtes-vous sûr de vouloir effacer tout l'historique de ce chat ?",
                "Confirmation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    // Clear UI locally
                    ClearConversation();
                    
                    // Delete messages locally in database
                    var response = await _mainForm.GetHttpClient().DeleteAsync(
                        $"{_mainForm.GetApiBaseUrl()}/chat/conversation/{_recipient.Id}?localOnly=true");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        // Notify recipient about deletion
                        await _mainForm.NotifyConversationDeletion(_recipient.Id);
                        PalMessageBox.Show("Conversation effacée de votre côté. Le destinataire a été notifié.");
                    }
                    else
                    {
                        PalMessageBox.Show("Erreur lors de la suppression du chat.");
                    }
                }
                catch (Exception ex)
                {
                    PalMessageBox.Show($"Erreur : {ex.Message}");
                }
            }
        }

        private static void EnsurePdfLicense()
        {
            if (_pdfLicenseConfigured) return;

            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.EnableCaching = true;
            _pdfLicenseConfigured = true;
        }

    private sealed record ExportImage(byte[] Data);

        private sealed class MessageExportContent
        {
            public List<string> Lines { get; } = new List<string>();
            public List<ExportImage> Images { get; } = new List<ExportImage>();
        }

        private MessageExportContent BuildMessageExportContent(ChatMessageDto msg)
        {
            var content = new MessageExportContent();
            var type = msg.ContentType?.Trim().ToLowerInvariant();

            switch (type)
            {
                case "rtf":
                    PopulateRtfMessageContent(msg.Content, content);
                    break;
                case "image":
                case "gif":
                    if (!TryAppendImageFromReference(content, msg.Content))
                    {
                        content.Lines.Add("[Image envoyée]");
                    }
                    break;
                case "file":
                    content.Lines.Add(BuildFileMessageLabel(msg.Content));
                    break;
                case "audio":
                    content.Lines.Add("[Message audio]");
                    break;
                case "video":
                    content.Lines.Add("[Message vidéo]");
                    break;
                default:
                    var normalized = NormalizeMessageContent(msg.Content);
                    foreach (var line in normalized.Split('\n'))
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            content.Lines.Add(line.TrimEnd());
                        }
                    }
                    break;
            }

            if (content.Lines.Count == 0 && content.Images.Count == 0)
            {
                content.Lines.Add("(message vide)");
            }

            return content;
        }

        private void PopulateRtfMessageContent(string? rtfContent, MessageExportContent target)
        {
            if (string.IsNullOrWhiteSpace(rtfContent))
            {
                target.Lines.Add("(message vide)");
                return;
            }

            string plainText = string.Empty;
            try
            {
                using var rtb = new RichTextBox { Rtf = rtfContent };
                plainText = rtb.Text?.Replace("\r\n", "\n") ?? string.Empty;
            }
            catch
            {
                plainText = string.Empty;
            }

            var processedImages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrEmpty(plainText))
            {
                plainText = plainText.Replace("\u200B", string.Empty);

                foreach (Match match in _smileyTokenRegex.Matches(plainText))
                {
                    var token = match.Value.Trim();
                    if (TryResolveSmileyImage(token, out var pngBytes))
                    {
                        var key = $"file:{token}";
                        if (processedImages.Add(key))
                        {
                            target.Images.Add(new ExportImage(pngBytes));
                        }
                    }
                }

                var sanitized = _smileyTokenRegex.Replace(plainText, string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(sanitized))
                {
                    foreach (var line in sanitized.Split('\n'))
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            target.Lines.Add(line.TrimEnd());
                        }
                    }
                }
            }

            foreach (var embeddedBytes in ExtractEmbeddedImagesFromRtf(rtfContent))
            {
                try
                {
                    var png = ConvertImageBytesToPng(embeddedBytes);
                    var key = $"hex:{Convert.ToBase64String(png)}";
                    if (processedImages.Add(key))
                    {
                        target.Images.Add(new ExportImage(png));
                    }
                }
                catch
                {
                    // ignore invalid embedded image data
                }
            }

            if (target.Lines.Count == 0 && target.Images.Count == 0)
            {
                target.Lines.Add("(message vide)");
            }
        }

        private bool TryAppendImageFromReference(MessageExportContent target, string? reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
            {
                return false;
            }

            var trimmed = reference.Trim();

            // Data URI image (used for sent pictures)
            if (trimmed.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
            {
                var commaIndex = trimmed.IndexOf(',');
                if (commaIndex > 0 && commaIndex < trimmed.Length - 1)
                {
                    try
                    {
                        var base64 = trimmed[(commaIndex + 1)..];
                        var bytes = Convert.FromBase64String(base64);
                        target.Images.Add(new ExportImage(bytes));
                        return true;
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }

            if (!TryResolveSmileyImage(trimmed, out var png))
            {
                return false;
            }

            target.Images.Add(new ExportImage(png));
            return true;
        }

        private static bool TryResolveSmileyImage(string fileName, out byte[] pngBytes)
        {
            pngBytes = Array.Empty<byte>();
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            var normalized = fileName.Trim();

            lock (_smileyExportCacheLock)
            {
                if (_smileyExportCache.TryGetValue(normalized, out var cachedBytes))
                {
                    if (cachedBytes.Length == 0)
                    {
                        return false;
                    }

                    pngBytes = cachedBytes;
                    return true;
                }
            }

            if (!ResourceImageStore.TryGetSmileyResource(normalized, out var resourceKey))
            {
                lock (_smileyExportCacheLock)
                {
                    _smileyExportCache[normalized] = Array.Empty<byte>();
                }

                return false;
            }

            var image = ResourceImageStore.LoadImage(resourceKey);
            if (image == null)
            {
                lock (_smileyExportCacheLock)
                {
                    _smileyExportCache[normalized] = Array.Empty<byte>();
                }

                return false;
            }

            try
            {
                using var bitmap = new Bitmap(image);
                using var ms = new MemoryStream();
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                var png = ms.ToArray();

                lock (_smileyExportCacheLock)
                {
                    _smileyExportCache[normalized] = png;
                }

                pngBytes = png;
                return true;
            }
            catch
            {
                lock (_smileyExportCacheLock)
                {
                    _smileyExportCache[normalized] = Array.Empty<byte>();
                }

                return false;
            }
            finally
            {
                image.Dispose();
            }
        }

        private static IEnumerable<byte[]> ExtractEmbeddedImagesFromRtf(string rtfContent)
        {
            if (string.IsNullOrEmpty(rtfContent))
                yield break;

            // Regex to extract RTF image data for PDF export
            var rtfImageRegex = new Regex(@"\{\\pict[^}]*?([0-9a-fA-F\s]+)\}", RegexOptions.Compiled | RegexOptions.Singleline);

            foreach (Match match in rtfImageRegex.Matches(rtfContent))
            {
                var hexGroup = match.Groups[1];
                if (!hexGroup.Success)
                    continue;

                var hex = Regex.Replace(hexGroup.Value, @"[^0-9A-Fa-f]", string.Empty);
                if (hex.Length < 2)
                    continue;

                if (TryParseHexToBytes(hex, out var bytes))
                {
                    yield return bytes;
                }
            }
        }

        private static byte[] ConvertImageBytesToPng(byte[] rawBytes)
        {
            using var input = new MemoryStream(rawBytes);
            using var image = Image.FromStream(input, useEmbeddedColorManagement: false, validateImageData: true);

            int targetWidth = image.Width;
            int targetHeight = image.Height;

            using var bitmap = new Bitmap(targetWidth, targetHeight, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.DrawImage(image, 0, 0, targetWidth, targetHeight);
            }

            using var output = new MemoryStream();
            bitmap.Save(output, System.Drawing.Imaging.ImageFormat.Png);
            return output.ToArray();
        }

        private static bool TryParseHexToBytes(string hex, out byte[] bytes)
        {
            bytes = Array.Empty<byte>();
            if (hex.Length % 2 != 0)
            {
                return false;
            }

            try
            {
                var buffer = new byte[hex.Length / 2];
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
                }

                bytes = buffer;
                return true;
            }
            catch
            {
                bytes = Array.Empty<byte>();
                return false;
            }
        }

        private static string BuildFileMessageLabel(string? content)
        {
            var normalized = NormalizeMessageContent(content);
            if (string.IsNullOrWhiteSpace(normalized) || normalized == "(message vide)")
            {
                return "[Fichier partagé]";
            }

            return $"[Fichier] {normalized}";
        }

        private static string NormalizeMessageContent(string? rawContent)
        {
            if (string.IsNullOrWhiteSpace(rawContent))
            {
                return "(message vide)";
            }

            var trimmed = rawContent.Trim();

            if (trimmed.StartsWith("{\\rtf", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var rtb = new RichTextBox { Rtf = trimmed };
                    var plain = rtb.Text?.Replace("\r\n", "\n").TrimEnd();

                    return string.IsNullOrWhiteSpace(plain) ? "(message vide)" : plain;
                }
                catch
                {
                    return trimmed.Replace("\\par", string.Empty).Replace("\r\n", "\n");
                }
            }

            return trimmed.Replace("\r\n", "\n");
        }

        private async void BtnSaveChatPdf_Click(object? sender, EventArgs e)
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var docsPath = Path.Combine(baseDir, "Assets", "Chat_Form", "Save_His", "save_docs.png");
                var pdfPath = Path.Combine(baseDir, "Assets", "Chat_Form", "Save_His", "save_pdf.png");
                var txtPath = Path.Combine(baseDir, "Assets", "Chat_Form", "Save_His", "save_txt.png");

                using var dlg = new SaveChatOptionsDialog(
                    LoadAbsoluteImage(docsPath, new Size(42, 42)),
                    LoadAbsoluteImage(pdfPath, new Size(42, 42)),
                    LoadAbsoluteImage(txtPath, new Size(42, 42)));

                if (sender is Control anchor)
                {
                    var screen = anchor.PointToScreen(Point.Empty);
                    dlg.Location = new Point(Math.Max(0, screen.X - 140), Math.Max(0, screen.Y + anchor.Height + 6));
                }
                else
                {
                    dlg.StartPosition = FormStartPosition.CenterParent;
                }

                if (dlg.ShowDialog(this) != DialogResult.OK || dlg.SelectedFormat == null)
                {
                    return;
                }

                switch (dlg.SelectedFormat.Value)
                {
                    case ChatSaveFormat.Pdf:
                        await ExportChatPdfAsync();
                        break;
                    case ChatSaveFormat.Txt:
                        await ExportChatTxtAsync();
                        break;
                    case ChatSaveFormat.Docs:
                        await ExportChatDocsAsync();
                        break;
                }
            }
            catch (Exception ex)
            {
                PalMessageBox.Show($"Erreur lors de l'export : {ex.Message}");
            }
        }

        private async Task ExportChatPdfAsync()
        {
            EnsurePdfLicense();

            var saveDialog = new SaveFileDialog();
            saveDialog.Filter = "PDF Files (*.pdf)|*.pdf";
            saveDialog.FileName = $"Chat_{_recipient.FirstName}_{_recipient.LastName}_{DateTime.Now:yyyyMMdd}.pdf";

            if (saveDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            var messages = await _mainForm.GetChatHistory(_recipient.Id);
            var exportTimestamp = DateTime.Now;

            if (messages == null || messages.Count == 0)
            {
                PalMessageBox.Show("Aucun message à exporter pour cette conversation.");
                return;
            }

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Segoe UI"));

                    page.Header().Column(column =>
                    {
                        column.Spacing(6);
                        column.Item().Text("Historique de conversation").FontSize(18).SemiBold();
                        column.Item().Text($"Entre {_currentUser.FirstName} {_currentUser.LastName} et {_recipient.FirstName} {_recipient.LastName}");
                        column.Item().Text($"Exporté le {exportTimestamp:dd/MM/yyyy HH:mm}").FontColor(Colors.Grey.Medium);
                    });

                    page.Content().PaddingVertical(10).Column(column =>
                    {
                        column.Spacing(12);

                        foreach (var msg in messages)
                        {
                            var senderName = msg.SenderId == _currentUser.Id
                                ? $"{_currentUser.FirstName} {_currentUser.LastName}"
                                : $"{_recipient.FirstName} {_recipient.LastName}";

                            var messageContent = BuildMessageExportContent(msg);

                            column.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingBottom(6).Column(block =>
                            {
                                block.Spacing(6);
                                block.Item().Text($"[{msg.Timestamp:dd/MM/yyyy HH:mm}] {senderName}").SemiBold();

                                if (messageContent.Lines.Count > 0)
                                {
                                    block.Item().Text(text =>
                                    {
                                        foreach (var line in messageContent.Lines)
                                        {
                                            text.Line(line);
                                        }
                                    });
                                }

                                if (messageContent.Images.Count > 0)
                                {
                                    foreach (var image in messageContent.Images)
                                    {
                                        block.Item().PaddingTop(4).Element(img =>
                                        {
                                            img.Width(PdfSmileySizePixels)
                                               .Height(PdfSmileySizePixels)
                                               .Image(image.Data);
                                        });
                                    }
                                }
                            });
                        }
                    });

                    page.Footer().AlignLeft().Text(x =>
                    {
                        x.Span("© PaL.X").FontSize(9).FontColor(Colors.Grey.Medium);
                        x.Span("  |  ");
                        x.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Medium);
                        x.Span(" / ");
                        x.TotalPages().FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });
            });

            byte[] pdfBytes;
            try
            {
                pdfBytes = await Task.Run(() => document.GeneratePdf());
            }
            catch (Exception pdfEx)
            {
                PalMessageBox.Show($"Erreur lors de la génération du PDF : {pdfEx.Message}");
                return;
            }

            try
            {
                await File.WriteAllBytesAsync(saveDialog.FileName, pdfBytes);
            }
            catch (Exception ioEx)
            {
                PalMessageBox.Show($"Erreur lors de l'écriture du fichier PDF : {ioEx.Message}");
                return;
            }

            var exportedFile = new FileInfo(saveDialog.FileName);
            if (!exportedFile.Exists || exportedFile.Length < 128)
            {
                PalMessageBox.Show("Le fichier exporté semble vide ou corrompu. Merci de réessayer.");
                return;
            }

            var result = MessageBox.Show(
                "Chat exporté avec succès !\n\nVoulez-vous ouvrir le fichier exporté ?",
                "Export réussi",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result == DialogResult.Yes)
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = saveDialog.FileName,
                        UseShellExecute = true
                    });
                }
                catch
                {
                    var folderResult = MessageBox.Show(
                        "Impossible d'ouvrir le fichier.\n\nVoulez-vous ouvrir le dossier contenant le fichier ?",
                        "Erreur",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (folderResult == DialogResult.Yes)
                    {
                        string folderPath = System.IO.Path.GetDirectoryName(saveDialog.FileName) ?? "";
                        if (!string.IsNullOrEmpty(folderPath))
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = folderPath,
                                UseShellExecute = true
                            });
                        }
                    }
                }
            }
        }

        private async Task ExportChatTxtAsync()
        {
            var saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Text Files (*.txt)|*.txt";
            saveDialog.FileName = $"Chat_{_recipient.FirstName}_{_recipient.LastName}_{DateTime.Now:yyyyMMdd}.txt";

            if (saveDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            var messages = await _mainForm.GetChatHistory(_recipient.Id);
            var exportTimestamp = DateTime.Now;

            if (messages == null || messages.Count == 0)
            {
                PalMessageBox.Show("Aucun message à exporter pour cette conversation.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Historique de conversation");
            sb.AppendLine($"Entre {_currentUser.FirstName} {_currentUser.LastName} et {_recipient.FirstName} {_recipient.LastName}");
            sb.AppendLine($"Exporté le {exportTimestamp:dd/MM/yyyy HH:mm}");
            sb.AppendLine(new string('-', 50));

            foreach (var msg in messages)
            {
                var senderName = msg.SenderId == _currentUser.Id
                    ? $"{_currentUser.FirstName} {_currentUser.LastName}"
                    : $"{_recipient.FirstName} {_recipient.LastName}";

                var content = BuildMessageExportContent(msg);
                sb.AppendLine($"[{msg.Timestamp:dd/MM/yyyy HH:mm}] {senderName}");

                foreach (var line in content.Lines)
                {
                    sb.AppendLine(line);
                }

                if (content.Images.Count > 0)
                {
                    sb.AppendLine($"[Images: {content.Images.Count}]");
                }

                sb.AppendLine();
            }

            await File.WriteAllTextAsync(saveDialog.FileName, sb.ToString(), Encoding.UTF8);
        }

        private async Task ExportChatDocsAsync()
        {
            var saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Word Document (*.docx)|*.docx";
            saveDialog.FileName = $"Chat_{_recipient.FirstName}_{_recipient.LastName}_{DateTime.Now:yyyyMMdd}.docx";

            if (saveDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            var messages = await _mainForm.GetChatHistory(_recipient.Id);
            var exportTimestamp = DateTime.Now;

            if (messages == null || messages.Count == 0)
            {
                PalMessageBox.Show("Aucun message à exporter pour cette conversation.");
                return;
            }

            var items = new List<DocxItem>
            {
                DocxItem.Paragraph("Historique de conversation", bold: true),
                DocxItem.Paragraph($"Entre {_currentUser.FirstName} {_currentUser.LastName} et {_recipient.FirstName} {_recipient.LastName}", bold: false),
                DocxItem.Paragraph($"Exporté le {exportTimestamp:dd/MM/yyyy HH:mm}", bold: false),
                DocxItem.Paragraph(string.Empty, bold: false)
            };

            foreach (var msg in messages)
            {
                var senderName = msg.SenderId == _currentUser.Id
                    ? $"{_currentUser.FirstName} {_currentUser.LastName}"
                    : $"{_recipient.FirstName} {_recipient.LastName}";

                items.Add(DocxItem.Paragraph($"[{msg.Timestamp:dd/MM/yyyy HH:mm}] {senderName}", bold: true));

                var content = BuildMessageExportContent(msg);
                foreach (var line in content.Lines)
                {
                    items.Add(DocxItem.Paragraph(line, bold: false));
                }

                if (content.Images.Count > 0)
                {
                    foreach (var image in content.Images)
                    {
                        var prepared = PrepareDocxImage(image.Data);
                        if (prepared != null)
                        {
                            items.Add(DocxItem.Image(prepared.Value.Bytes, prepared.Value.Extension, prepared.Value.Cx, prepared.Value.Cy));
                        }
                    }
                }

                items.Add(DocxItem.Paragraph(string.Empty, bold: false));
            }

            var docxBytes = BuildChatDocx(items, $"{_currentUser.FirstName} {_currentUser.LastName}");
            await File.WriteAllBytesAsync(saveDialog.FileName, docxBytes);
        }

        private sealed class DocxItem
        {
            public string? Text { get; }
            public bool Bold { get; }
            public byte[]? ImageBytes { get; }
            public string? ImageExtension { get; }
            public long ImageCx { get; }
            public long ImageCy { get; }

            private DocxItem(string? text, bool bold, byte[]? imageBytes, string? imageExtension, long imageCx, long imageCy)
            {
                Text = text;
                Bold = bold;
                ImageBytes = imageBytes;
                ImageExtension = imageExtension;
                ImageCx = imageCx;
                ImageCy = imageCy;
            }

            public static DocxItem Paragraph(string text, bool bold) => new DocxItem(text ?? string.Empty, bold, null, null, 0, 0);

            public static DocxItem Image(byte[] bytes, string extension, long cx, long cy) => new DocxItem(null, false, bytes, extension, cx, cy);

            public bool IsImage => ImageBytes != null && !string.IsNullOrWhiteSpace(ImageExtension);
        }

        private static (byte[] Bytes, string Extension, long Cx, long Cy)? PrepareDocxImage(byte[] rawBytes)
        {
            if (rawBytes == null || rawBytes.Length < 8)
            {
                return null;
            }

            const int defaultPx = 64;
            const int maxPx = 128;

            try
            {
                using var srcMs = new MemoryStream(rawBytes);
                using var img = Image.FromStream(srcMs, useEmbeddedColorManagement: false, validateImageData: true);

                int widthPx = img.Width;
                int heightPx = img.Height;
                if (widthPx <= 0 || heightPx <= 0)
                {
                    widthPx = defaultPx;
                    heightPx = defaultPx;
                }

                double scale = Math.Min(1.0, Math.Min((double)maxPx / widthPx, (double)maxPx / heightPx));
                int outW = Math.Max(1, (int)Math.Round(widthPx * scale));
                int outH = Math.Max(1, (int)Math.Round(heightPx * scale));

                using var bmp = new Bitmap(outW, outH);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.DrawImage(img, new Rectangle(0, 0, outW, outH));
                }

                using var outMs = new MemoryStream();
                bmp.Save(outMs, System.Drawing.Imaging.ImageFormat.Png);
                var pngBytes = outMs.ToArray();

                long cx = outW * 9525L;
                long cy = outH * 9525L;
                return (pngBytes, "png", cx, cy);
            }
            catch
            {
                // Fallback: try to keep bytes as-is (assume png)
                long cx = defaultPx * 9525L;
                long cy = defaultPx * 9525L;
                return (rawBytes, "png", cx, cy);
            }
        }

        private static byte[] BuildChatDocx(List<DocxItem> items, string? creator)
        {
            using var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                var imageParts = new List<(string RelId, string Path, string ContentType, string Extension, long Cx, long Cy)>();
                int imageIndex = 0;

                foreach (var item in items)
                {
                    if (!item.IsImage)
                    {
                        continue;
                    }

                    imageIndex++;
                    var ext = item.ImageExtension!.Trim('.').ToLowerInvariant();
                    if (ext != "png" && ext != "jpg" && ext != "jpeg")
                    {
                        ext = "png";
                    }

                    string contentType = ext switch
                    {
                        "jpg" => "image/jpeg",
                        "jpeg" => "image/jpeg",
                        _ => "image/png"
                    };

                    var relId = $"rIdImg{imageIndex}";
                    var path = $"media/image{imageIndex}.{ext}";
                    imageParts.Add((relId, path, contentType, ext, item.ImageCx, item.ImageCy));

                    var entry = zip.CreateEntry($"word/{path}", CompressionLevel.Optimal);
                    using var stream = entry.Open();
                    stream.Write(item.ImageBytes!, 0, item.ImageBytes!.Length);
                }

                WriteZipText(zip, "[Content_Types].xml", BuildContentTypesXml(imageParts));
                WriteZipText(zip, "_rels/.rels", BuildRootRelsXml());
                WriteZipText(zip, "word/document.xml", BuildDocumentXml(items, imageParts));
                WriteZipText(zip, "word/_rels/document.xml.rels", BuildDocumentRelsXml(imageParts));
                WriteZipText(zip, "docProps/core.xml", BuildCorePropsXml(creator));
                WriteZipText(zip, "docProps/app.xml", BuildAppPropsXml());
            }

            return ms.ToArray();
        }

        private static void WriteZipText(ZipArchive zip, string entryName, string content)
        {
            var entry = zip.CreateEntry(entryName, CompressionLevel.Optimal);
            using var stream = entry.Open();
            using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            writer.Write(content);
        }

        private static string BuildContentTypesXml(List<(string RelId, string Path, string ContentType, string Extension, long Cx, long Cy)> imageParts)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
            sb.Append("<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
            sb.Append("<Default Extension=\"xml\" ContentType=\"application/xml\"/>");

            // Image defaults (avoid duplicates)
            bool needsPng = imageParts.Any(p => p.Extension == "png");
            bool needsJpeg = imageParts.Any(p => p.Extension == "jpg" || p.Extension == "jpeg");
            if (needsPng)
            {
                sb.Append("<Default Extension=\"png\" ContentType=\"image/png\"/>");
            }
            if (needsJpeg)
            {
                sb.Append("<Default Extension=\"jpg\" ContentType=\"image/jpeg\"/>");
                sb.Append("<Default Extension=\"jpeg\" ContentType=\"image/jpeg\"/>");
            }

            sb.Append("<Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/>");
            sb.Append("<Override PartName=\"/docProps/core.xml\" ContentType=\"application/vnd.openxmlformats-package.core-properties+xml\"/>");
            sb.Append("<Override PartName=\"/docProps/app.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.extended-properties+xml\"/>");
            sb.Append("</Types>");
            return sb.ToString();
        }

        private static string BuildRootRelsXml()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                   "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                   "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/>" +
                   "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties\" Target=\"docProps/core.xml\"/>" +
                   "<Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties\" Target=\"docProps/app.xml\"/>" +
                   "</Relationships>";
        }

        private static string BuildDocumentRelsXml(List<(string RelId, string Path, string ContentType, string Extension, long Cx, long Cy)> imageParts)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");

            foreach (var img in imageParts)
            {
                sb.Append($"<Relationship Id=\"{img.RelId}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/image\" Target=\"{XmlEscape(img.Path)}\"/>");
            }

            sb.Append("</Relationships>");
            return sb.ToString();
        }

        private static string BuildCorePropsXml(string? creator)
        {
            var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var safeCreator = XmlEscape(string.IsNullOrWhiteSpace(creator) ? "PaL.X" : creator);
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                   "<cp:coreProperties xmlns:cp=\"http://schemas.openxmlformats.org/package/2006/metadata/core-properties\"" +
                   " xmlns:dc=\"http://purl.org/dc/elements/1.1/\"" +
                   " xmlns:dcterms=\"http://purl.org/dc/terms/\"" +
                   " xmlns:dcmitype=\"http://purl.org/dc/dcmitype/\"" +
                   " xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">" +
                   $"<dc:creator>{safeCreator}</dc:creator>" +
                   "<cp:lastModifiedBy>PaL.X</cp:lastModifiedBy>" +
                   $"<dcterms:created xsi:type=\"dcterms:W3CDTF\">{now}</dcterms:created>" +
                   $"<dcterms:modified xsi:type=\"dcterms:W3CDTF\">{now}</dcterms:modified>" +
                   "</cp:coreProperties>";
        }

        private static string BuildAppPropsXml()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                   "<Properties xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/extended-properties\"" +
                   " xmlns:vt=\"http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes\">" +
                   "<Application>PaL.X</Application>" +
                   "</Properties>";
        }

        private static string BuildDocumentXml(List<DocxItem> items, List<(string RelId, string Path, string ContentType, string Extension, long Cx, long Cy)> imageParts)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" ");
            sb.Append("xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" ");
            sb.Append("xmlns:wp=\"http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing\" ");
            sb.Append("xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" ");
            sb.Append("xmlns:pic=\"http://schemas.openxmlformats.org/drawingml/2006/picture\">");
            sb.Append("<w:body>");

            int currentImageIdx = 0;
            foreach (var item in items)
            {
                if (!item.IsImage)
                {
                    sb.Append("<w:p><w:r>");
                    if (item.Bold)
                    {
                        sb.Append("<w:rPr><w:b/></w:rPr>");
                    }
                    sb.Append("<w:t xml:space=\"preserve\">");
                    sb.Append(XmlEscape(item.Text ?? string.Empty));
                    sb.Append("</w:t></w:r></w:p>");
                    continue;
                }

                if (currentImageIdx >= imageParts.Count)
                {
                    continue;
                }

                var img = imageParts[currentImageIdx++];
                var name = $"Image {currentImageIdx}";
                var docPrId = currentImageIdx;

                sb.Append("<w:p><w:r><w:drawing>");
                sb.Append($"<wp:inline distT=\"0\" distB=\"0\" distL=\"0\" distR=\"0\">");
                sb.Append($"<wp:extent cx=\"{img.Cx}\" cy=\"{img.Cy}\"/>");
                sb.Append("<wp:effectExtent l=\"0\" t=\"0\" r=\"0\" b=\"0\"/>");
                sb.Append($"<wp:docPr id=\"{docPrId}\" name=\"{XmlEscape(name)}\"/>");
                sb.Append("<wp:cNvGraphicFramePr><a:graphicFrameLocks noChangeAspect=\"1\"/></wp:cNvGraphicFramePr>");
                sb.Append("<a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/picture\">");
                sb.Append("<pic:pic>");
                sb.Append("<pic:nvPicPr>");
                sb.Append($"<pic:cNvPr id=\"0\" name=\"{XmlEscape(name)}\"/>");
                sb.Append("<pic:cNvPicPr/></pic:nvPicPr>");
                sb.Append("<pic:blipFill>");
                sb.Append($"<a:blip r:embed=\"{XmlEscape(img.RelId)}\"/>");
                sb.Append("<a:stretch><a:fillRect/></a:stretch>");
                sb.Append("</pic:blipFill>");
                sb.Append("<pic:spPr>");
                sb.Append("<a:xfrm>");
                sb.Append("<a:off x=\"0\" y=\"0\"/>");
                sb.Append($"<a:ext cx=\"{img.Cx}\" cy=\"{img.Cy}\"/>");
                sb.Append("</a:xfrm>");
                sb.Append("<a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom>");
                sb.Append("</pic:spPr>");
                sb.Append("</pic:pic>");
                sb.Append("</a:graphicData></a:graphic>");
                sb.Append("</wp:inline>");
                sb.Append("</w:drawing></w:r></w:p>");
            }

            // Minimal section properties
            sb.Append("<w:sectPr>");
            sb.Append("<w:pgSz w:w=\"11906\" w:h=\"16838\"/>"); // A4
            sb.Append("<w:pgMar w:top=\"1440\" w:right=\"1440\" w:bottom=\"1440\" w:left=\"1440\" w:header=\"720\" w:footer=\"720\" w:gutter=\"0\"/>");
            sb.Append("</w:sectPr>");

            sb.Append("</w:body></w:document>");
            return sb.ToString();
        }

        private static string XmlEscape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        private Bitmap MakeGrayscale(Bitmap original)
        {
            Bitmap grayscale = new Bitmap(original.Width, original.Height);
            
            for (int y = 0; y < original.Height; y++)
            {
                for (int x = 0; x < original.Width; x++)
                {
                    Color originalColor = original.GetPixel(x, y);
                    int grayScale = (int)((originalColor.R * 0.3) + (originalColor.G * 0.59) + (originalColor.B * 0.11));
                    Color grayColor = Color.FromArgb(originalColor.A, grayScale, grayScale, grayScale);
                    grayscale.SetPixel(x, y, grayColor);
                }
            }
            
            return grayscale;
        }
        private enum VoiceState
        {
            Disabled,
            Ready,
            Recording
        }
    }
}
