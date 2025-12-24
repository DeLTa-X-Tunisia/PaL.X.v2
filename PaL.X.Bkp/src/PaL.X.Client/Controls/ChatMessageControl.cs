using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using PaL.X.Client.Properties;
using PaL.X.Client.Services;
using PaL.X.Client.Voice;
using PaL.X.Client;
using System.Drawing.Imaging;
using System.Net;
using PaL.X.Shared.DTOs;

namespace PaL.X.Client.Controls
{
    public class ChatMessageControl : UserControl
    {
        private readonly FlowLayoutPanel _flpContent = new();
        private readonly Label _lblSender = new();
        private readonly Label _lblTime = new();
        private readonly Panel _statusPanel = new();
        private readonly ProgressBar _progressBar = new();
        private readonly Label _progressLabel = new();
        private readonly PictureBox _stateIcon = new();
        private readonly LinkLabel _retryLink = new();
        private readonly RichTextBox _rtbTemp; // Temporary RichTextBox for RTF parsing

        private bool _isIncoming;
        private readonly string _contentType;
        private byte[]? _binaryPayload;
        private string? _videoSourceUrl;
        private string? _attachmentSourceUrl;
        private string? _attachmentFileName;
        private string? _attachmentMime;
        private bool _isAudioAttachment;
        private bool _isVoiceAttachment;
        private string? _imageUrl;
        private SendState _sendState;
        private bool _showStatusArea;
        private string? _textContentForCopy;

        private PictureBox? _imagePictureBox;
        private PictureBox? _videoPictureBox;
        private PictureBox? _attachmentPictureBox;
        private ContextMenuStrip? _imageContextMenu;
        private ContextMenuStrip? _videoContextMenu;
        private ContextMenuStrip? _attachmentContextMenu;

        private static readonly Regex RtfImageRegex = new Regex(@"\{\\pict[^}]*?([0-9a-fA-F\s]+)\}", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly HttpClient HttpClient = new();

        private static readonly Lazy<Image?> FileBadgeSource = new(() => TryLoadIcon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Icons", "file.png")));
        private static readonly Lazy<Image?> AudioBadgeSource = new(() => TryLoadIcon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Icons", "audio.png")));
        private static readonly Lazy<Image?> VoiceBadgeSource = new(() =>
        {
            var absolute = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Voice", "volume_normal_blue_98174", "AudioMsg.png");
            var img = TryLoadIcon(absolute);
            if (img != null) return new Bitmap(img, new Size(24, 24));

            var fallbackOriginal = TryLoadIcon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Voice", "message_audio.png"));
            if (fallbackOriginal != null) return new Bitmap(fallbackOriginal, new Size(24, 24));

            var fallback = TryLoadIcon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Icons", "voice.png"));
            return fallback != null ? new Bitmap(fallback, new Size(24, 24)) : null;
        });
        private static readonly Lazy<Image?> VideoBadgeSource = new(() => TryLoadIcon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Icons", "video.png")));

    private static readonly Image IconCopyText = SystemIcons.Information.ToBitmap();
    private static readonly Image IconOpen = TryLoadIcon(@"C:\Users\azizi\OneDrive\Desktop\PaL.X\various\EditScreen.png") ?? SystemIcons.Application.ToBitmap();
    private static readonly Image IconSave = TryLoadIcon(@"C:\Users\azizi\OneDrive\Desktop\PaL.X\context\Save.png") ?? SystemIcons.Shield.ToBitmap();
    private static readonly Image IconCopyLink = TryLoadIcon(@"C:\Users\azizi\OneDrive\Desktop\PaL.X\various\SendScreen.png") ?? SystemIcons.Information.ToBitmap();
    private static readonly Image IconClose = TryLoadIcon(@"C:\Users\azizi\OneDrive\Desktop\PaL.X\various\fermer.ico") ?? SystemIcons.Error.ToBitmap();
    private static readonly Image IconFullSize = IconOpen;
    private static readonly Image IconDownload = SystemIcons.Information.ToBitmap();
    private static readonly Image IconOpenLink = SystemIcons.Information.ToBitmap();

        private static readonly Size VideoThumbSize = new Size(200, 120);

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int MessageId { get; set; }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SenderId { get; }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DateTime Timestamp { get; private set; }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsEdited { get; }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string OriginalContent { get; private set; } = string.Empty;
        public event EventHandler? RetryRequested;

        public enum SendState
        {
            None,
            Sending,
            Success,
            Error
        }

        public ChatMessageControl(int messageId, int senderId, string senderName, string messageContent, DateTime time, bool isIncoming, bool isEdited)
            : this(messageId, senderId, senderName, messageContent, time, isIncoming, isEdited, "text", null, SendState.None, false)
        {
        }

        // Constructor with explicit content type
    public ChatMessageControl(int messageId, int senderId, string senderName, string messageContent, DateTime time, bool isIncoming, bool isEdited, string? contentType, List<string>? smileyFilenames = null, SendState initialState = SendState.None, bool showStatusArea = false)
        {
            MessageId = messageId;
            SenderId = senderId;
            Timestamp = time;
            IsEdited = isEdited;
            OriginalContent = messageContent;
            _isIncoming = isIncoming;
            _contentType = (contentType ?? "text").Trim().ToLowerInvariant();
            _sendState = initialState;
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.Padding = new Padding(5);
            this.Margin = new Padding(0, 5, 0, 5);
            this.BackColor = Color.Transparent;
            _rtbTemp = new RichTextBox(); // For RTF parsing
            _rtbTemp.Visible = false;

            InitializeControls(senderName, time);
            InitializeStatusControls(showStatusArea || initialState != SendState.None);
            ApplyInitialSendState(initialState);

            var plainTextForCopy = ExtractPlainTextForCopy(messageContent);

            // Route content based on type
            if (_contentType == "image" && TryAddImageFromContent(messageContent))
            {
                // Already handled
                return;
            }
            if (_contentType == "video" && TryAddVideoFromContent(messageContent))
            {
                return;
            }

            if (_contentType == "file" && TryAddAttachmentFromContent(messageContent, isAudio: false, isVoice: false))
            {
                return;
            }

            if (_contentType == "audio" && TryAddAttachmentFromContent(messageContent, isAudio: true, isVoice: false))
            {
                return;
            }

            if (_contentType == "voice" && TryAddAttachmentFromContent(messageContent, isAudio: true, isVoice: true))
            {
                return;
            }

            if (_contentType == "call" && TryAddCallSummary(messageContent))
            {
                return;
            }

            if (_contentType == "video_call_event" && TryAddVideoCallEvent(messageContent))
            {
                return;
            }

            if (smileyFilenames != null)
            {
                ParseAndAddContentWithSmileys(messageContent, smileyFilenames);
            }
            else
            {
                ParseAndAddContent(messageContent);
            }

            SetupTextContextMenu(plainTextForCopy);
        }

        private bool TryAddVideoCallEvent(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return false;

            VideoCallEventDto? evt = null;
            try
            {
                evt = JsonSerializer.Deserialize<VideoCallEventDto>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return false;
            }

            if (evt == null || string.IsNullOrWhiteSpace(evt.EventType))
            {
                return false;
            }

            var whenLocal = (evt.AtUtc == default ? DateTime.UtcNow : evt.AtUtc).ToLocalTime();
            var isStart = evt.EventType.Trim().Equals("started", StringComparison.OrdinalIgnoreCase);
            var label = isStart
                ? $"Appel vidéo démarré .. {whenLocal:HH:mm}"
                : $"Appel vidéo terminé .. {whenLocal:HH:mm}";

            var iconFile = isStart ? "Msg_Chat_En_Appel_Video.png" : "Msg_Chat_Appel_Video_Terminé.png";
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Video CaLL", iconFile);
            var icon = TryLoadIcon(iconPath);

            var container = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 4, 0, 4),
                Padding = new Padding(6),
                BackColor = Color.FromArgb(245, 246, 248)
            };

            var pic = new PictureBox
            {
                Size = new Size(44, 44),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = icon,
                Margin = new Padding(4)
            };

            var lbl = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Text = label,
                ForeColor = Color.FromArgb(40, 40, 40),
                Margin = new Padding(4, 14, 4, 4)
            };

            container.Controls.Add(pic);
            container.Controls.Add(lbl);
            container.FlowDirection = FlowDirection.LeftToRight;

            _flpContent.FlowDirection = FlowDirection.LeftToRight;
            _flpContent.Controls.Add(container);
            return true;
        }

        private string ExtractPlainTextForCopy(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return string.Empty;

            try
            {
                if (content.StartsWith("{\\rtf", StringComparison.OrdinalIgnoreCase))
                {
                    _rtbTemp.Rtf = content;
                    return _rtbTemp.Text ?? string.Empty;
                }
            }
            catch { }

            return content;
        }

        private void SetupTextContextMenu(string plainText)
        {
            _textContentForCopy = string.IsNullOrWhiteSpace(plainText) ? null : plainText;
            if (string.IsNullOrWhiteSpace(_textContentForCopy))
            {
                return;
            }

            var menu = new ContextMenuStrip();
            menu.Items.Add(new ToolStripMenuItem("Copier", IconCopyText, (_, __) => Clipboard.SetText(_textContentForCopy))
            {
                Enabled = !string.IsNullOrWhiteSpace(_textContentForCopy)
            });

            this.ContextMenuStrip = menu;
            _flpContent.ContextMenuStrip = menu;
        }

        private void InitializeControls(string senderName, DateTime time)
        {
            // Container for the whole message block
            var mainContainer = new FlowLayoutPanel();
            mainContainer.FlowDirection = FlowDirection.TopDown;
            mainContainer.AutoSize = true;
            mainContainer.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            mainContainer.WrapContents = false;
            mainContainer.BackColor = Color.White; // Same as chat background
            mainContainer.Padding = new Padding(8);
            // Rounded corners hack: standard panels don't support it easily, keeping it simple for now.
            
            // Sender Label
            // _lblSender already initialized to avoid nullability warnings
            _lblSender.Text = senderName;
            _lblSender.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            _lblSender.ForeColor = _isIncoming ? Color.DarkBlue : Color.DarkGreen;
            _lblSender.AutoSize = true;
            mainContainer.Controls.Add(_lblSender);

            // Content Flow (for mixed text + images)
            // _flpContent already initialized to avoid nullability warnings
            _flpContent.FlowDirection = FlowDirection.LeftToRight;
            _flpContent.WrapContents = true;
            _flpContent.AutoSize = true;
            _flpContent.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _flpContent.MaximumSize = new Size(400, 0); // Limit width
            _flpContent.Margin = new Padding(0, 5, 0, 5);
            mainContainer.Controls.Add(_flpContent);

            // Time Label
            // _lblTime already initialized to avoid nullability warnings
            _lblTime.Text = time.ToShortTimeString();
            _lblTime.Font = new Font("Segoe UI", 7, FontStyle.Regular);
            _lblTime.ForeColor = Color.Gray;
            _lblTime.AutoSize = true;
            _lblTime.Margin = new Padding(0, 2, 0, 0);
            // Align right
            // FlowLayoutPanel doesn't support individual alignment easily, so we just add it.
            mainContainer.Controls.Add(_lblTime);

            this.Controls.Add(mainContainer);
        }

        private void InitializeStatusControls(bool visible)
        {
            _statusPanel.AutoSize = true;
            _statusPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _statusPanel.Margin = new Padding(0, 4, 0, 2);
            _statusPanel.Padding = new Padding(0, 0, 0, 0);
            _statusPanel.BackColor = Color.Transparent;

            var statusFlow = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            _progressBar.Width = 120;
            _progressBar.Height = 8;
            _progressBar.Style = ProgressBarStyle.Continuous;
            _progressBar.Minimum = 0;
            _progressBar.Maximum = 100;
            _progressBar.Value = 0;
            _progressBar.Margin = new Padding(0, 0, 6, 0);

            _progressLabel.AutoSize = true;
            _progressLabel.Font = new Font("Segoe UI", 8F, FontStyle.Regular);
            _progressLabel.ForeColor = Color.Gray;
            _progressLabel.Margin = new Padding(0, 0, 6, 0);
            _progressLabel.Text = "0%";

            _stateIcon.Size = new Size(16, 16);
            _stateIcon.Margin = new Padding(0, 0, 6, 0);
            _stateIcon.SizeMode = PictureBoxSizeMode.Zoom;
            _stateIcon.Visible = false;

            _retryLink.Text = "Réessayer";
            _retryLink.AutoSize = true;
            _retryLink.LinkColor = Color.Red;
            _retryLink.Margin = new Padding(0);
            _retryLink.Visible = false;
            _retryLink.Click += (s, e) => RetryRequested?.Invoke(this, EventArgs.Empty);

            statusFlow.Controls.Add(_progressBar);
            statusFlow.Controls.Add(_progressLabel);
            statusFlow.Controls.Add(_stateIcon);
            statusFlow.Controls.Add(_retryLink);

            _statusPanel.Controls.Add(statusFlow);
            _statusPanel.Visible = visible;

            // Insert status panel just above time label
            if (this.Controls.Count > 0 && this.Controls[0] is FlowLayoutPanel main)
            {
                var timeIndex = main.Controls.Contains(_lblTime) ? main.Controls.IndexOf(_lblTime) : main.Controls.Count;
                main.Controls.Add(_statusPanel);
                if (timeIndex >= 0 && timeIndex < main.Controls.Count)
                {
                    main.Controls.SetChildIndex(_statusPanel, timeIndex);
                }
            }
        }

        private void ApplyInitialSendState(SendState state)
        {
            switch (state)
            {
                case SendState.Sending:
                    SetSendProgress(1);
                    break;
                case SendState.Success:
                    MarkSendSuccess();
                    break;
                case SendState.Error:
                    MarkSendError();
                    break;
                default:
                    _statusPanel.Visible = false;
                    break;
            }
        }
        
        // NOUVELLE MÉTHODE: Parse avec smileys explicites
        private void ParseAndAddContentWithSmileys(string content, List<string>? smileyFilenames)
        {
            System.Diagnostics.Debug.WriteLine($"[SMILEY PARSE] Parsing content with {smileyFilenames?.Count ?? 0} smileys");
            
            if (smileyFilenames == null || smileyFilenames.Count == 0)
            {
                // Pas de smileys, juste du texte
                ParseAndAddContent(content);
                return;
            }
            
            // Parse le RTF pour extraire le texte formaté ET les images
            if (content.StartsWith("{\\rtf"))
            {
                _rtbTemp.Rtf = content;
                string plainText = _rtbTemp.Text;
                System.Diagnostics.Debug.WriteLine($"[SMILEY PARSE] RTF plain text: '{plainText}' (length: {plainText.Length})");
                
                // Compte les placeholders d'images dans le texte
                int placeholderCount = plainText.Count(c => c == '\uFFFC');
                System.Diagnostics.Debug.WriteLine($"[SMILEY PARSE] Found {placeholderCount} image placeholders in text");
                
                // FIX: Always use the placeholder logic if placeholders exist, even if counts don't match perfectly
                // This handles cases where some smileys might be merged or split in weird ways
                if (placeholderCount > 0)
                {
                    int smileyIndex = 0;
                    int textStart = 0;
                    
                    for (int i = 0; i < plainText.Length; i++)
                    {
                        if (plainText[i] == '\uFFFC') // Placeholder d'image
                        {
                            // Ajoute le texte avant l'image
                            if (i > textStart)
                            {
                                AddFormattedTextRange(textStart, i);
                            }
                            
                            // Ajoute le smiley
                            if (smileyIndex < smileyFilenames.Count)
                            {
                                string smileyFile = smileyFilenames[smileyIndex];
                                System.Diagnostics.Debug.WriteLine($"[SMILEY PARSE] Adding smiley at position {i}: {smileyFile}");
                                AddSmileyDirect(smileyFile);
                                smileyIndex++;
                            }
                            else 
                            {
                                // If we run out of filenames but have placeholders, try to reuse the last one or show a generic placeholder
                                // This is a fallback for the "missing smiley" issue
                                System.Diagnostics.Debug.WriteLine($"[SMILEY PARSE] Warning: More placeholders than filenames. Placeholder at {i}");
                            }
                            
                            textStart = i + 1;
                        }
                    }
                    
                    // Ajoute le texte restant
                    if (textStart < plainText.Length)
                    {
                        AddFormattedTextRange(textStart, plainText.Length);
                    }

                    // If we have leftover smileys that weren't associated with a placeholder (e.g. pasted at end without placeholder?)
                    // Append them to the end
                    while (smileyIndex < smileyFilenames.Count)
                    {
                         string smileyFile = smileyFilenames[smileyIndex];
                         System.Diagnostics.Debug.WriteLine($"[SMILEY PARSE] Appending detached smiley: {smileyFile}");
                         AddSmileyDirect(smileyFile);
                         smileyIndex++;
                    }
                }
                else
                {
                    // Pas de placeholders ou nombre incorrect - ajoute texte puis smileys
                    System.Diagnostics.Debug.WriteLine($"[SMILEY PARSE] Placeholder mismatch (count={placeholderCount}) - adding text then smileys separately");
                    
                    // Retire tous les placeholders du texte
                    string cleanText = plainText.Replace("\uFFFC", "");
                    if (!string.IsNullOrWhiteSpace(cleanText))
                    {
                        // Use AddFormattedTextRange on the whole text to preserve formatting even in fallback
                        // We need to select the whole text in _rtbTemp first
                        _rtbTemp.SelectAll();
                        AddFormattedTextRange(0, _rtbTemp.TextLength);
                    }
                    
                    // Ajoute tous les smileys à la suite
                    foreach (var smileyFile in smileyFilenames)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SMILEY PARSE] Adding smiley: {smileyFile}");
                        AddSmileyDirect(smileyFile);
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[SMILEY PARSE] Finished - total controls in bubble: {_flpContent.Controls.Count}");
            }
            else
            {
                // Plain text avec smileys (fallback)
                System.Diagnostics.Debug.WriteLine($"[SMILEY PARSE] Plain text mode");
                AddText(content);
                foreach (var smileyFile in smileyFilenames)
                {
                    AddSmileyDirect(smileyFile);
                }
            }
        }
        
        // NOUVELLE MÉTHODE: Ajoute un smiley directement depuis le nom de fichier
        private void AddSmileyDirect(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                return;
                
            // Trouve la ressource correspondante
            if (ResourceImageStore.TryGetSmileyResource(filename, out var resourceKey))
            {
                var image = ResourceImageStore.LoadImage(resourceKey, new Size(36, 36));
                if (image != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[SMILEY DIRECT] ✓ Loaded {filename} from {resourceKey}, adding to FlowLayoutPanel");
                    AddImageToContent(image);
                    return;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[SMILEY DIRECT] ✗ LoadImage returned null for {resourceKey}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[SMILEY DIRECT] ✗ TryGetSmileyResource failed for {filename}");
            }
            
            // Fallback: affiche le nom du fichier
            AddText($"[{filename}]");
        }

        private void ParseAndAddContent(string content)
        {
            // Image content routed first for robustness
            if (_contentType == "image" && TryAddImageFromContent(content))
            {
                return;
            }
            if (_contentType == "video" && TryAddVideoFromContent(content))
            {
                return;
            }

            // Check if content is RTF format
            if (content.StartsWith("{\\rtf"))
            {
                ParseRtfContent(content);
            }
            else
            {
                // Legacy plain text parsing
                string pattern = @"\[smiley:(.*?)\]";
                var parts = Regex.Split(content, pattern);

                for (int i = 0; i < parts.Length; i++)
                {
                    string part = parts[i];
                    if (string.IsNullOrEmpty(part)) continue;

                    if (i % 2 != 0) // It's a smiley filename
                    {
                        AddSmiley(part);
                    }
                    else // It's text
                    {
                        AddText(part);
                    }
                }
            }
        }

        private void ParseRtfContent(string rtfContent)
        {
            System.Diagnostics.Debug.WriteLine($"[SMILEY] ParseRtfContent called with RTF length: {rtfContent?.Length ?? 0}");
            
            _rtbTemp.Rtf = rtfContent;
            string plainText = _rtbTemp.Text;

            var embeddedImages = ExtractImagesFromRtf(rtfContent);
            System.Diagnostics.Debug.WriteLine($"[SMILEY] Plain text length: {plainText.Length}, Images extracted: {embeddedImages.Count}");

            int textStart = 0;
            for (int i = 0; i < plainText.Length; i++)
            {
                if (plainText[i] != '\uFFFC')
                {
                    continue;
                }

                System.Diagnostics.Debug.WriteLine($"[SMILEY] Found image placeholder at position {i}");

                if (i > textStart)
                {
                    AddFormattedTextRange(textStart, i);
                }

                // Add embedded image as static PNG
                if (embeddedImages.Count > 0)
                {
                    var image = embeddedImages.Dequeue();
                    AddImageToContent(image);
                    System.Diagnostics.Debug.WriteLine($"[SMILEY] Added image {image.Width}x{image.Height} to content");
                }

                textStart = i + 1;
            }

            if (textStart < plainText.Length)
            {
                AddFormattedTextRange(textStart, plainText.Length);
            }

            // Cleanup remaining images
            while (embeddedImages.Count > 0)
            {
                var leftover = embeddedImages.Dequeue();
                leftover.Dispose();
            }
            
            System.Diagnostics.Debug.WriteLine($"[SMILEY] ParseRtfContent finished - total controls: {_flpContent.Controls.Count}");
        }

        private void AddSmiley(string filename)
        {
            var sanitized = SanitizeSmileyFileName(filename);
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                AddText($"[{filename}]");
                return;
            }

            if (ResourceImageStore.TryGetSmileyResource(sanitized, out var resourceKey))
            {
                var image = ResourceImageStore.LoadImage(resourceKey);
                if (image != null)
                {
                    AddImageToContent(image);
                    return;
                }
            }

            AddText($"[{sanitized}]");
        }

        private void AddImageToContent(Image image)
        {
            var pic = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(36, 36),
                BackColor = Color.Transparent,
                Margin = new Padding(2),
                Image = image
            };

            _flpContent.Controls.Add(pic);
            System.Diagnostics.Debug.WriteLine($"[SMILEY] ✓ PictureBox added to FlowLayoutPanel - Total controls: {_flpContent.Controls.Count}");

            // Cleanup on dispose
            pic.Disposed += (_, __) => image.Dispose();
        }

        private bool TryAddImageFromContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return false;

            var data = content.Trim();

            // 1) Remote URL (upload flow)
            if (Uri.TryCreate(data, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                _binaryPayload = null;
                _imageUrl = uri.ToString();

                var previewSize = new Size(180, 180);
                var horizontalPadding = Math.Max(0, (_flpContent.MaximumSize.Width - previewSize.Width) / 2);
                var container = new Panel
                {
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    Margin = new Padding(0, 4, 0, 4),
                    Padding = new Padding(horizontalPadding, 0, 0, 0),
                    BackColor = Color.Transparent
                };

                var pic = new PictureBox
                {
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Size = previewSize,
                    ImageLocation = uri.ToString(),
                    Margin = new Padding(2),
                    Cursor = Cursors.Hand
                };

                pic.LoadAsync();
                var menu = AttachRemoteImageContextMenu(pic, uri.ToString());
                container.ContextMenuStrip = menu;

                container.Controls.Add(pic);
                _flpContent.FlowDirection = FlowDirection.LeftToRight;
                _flpContent.Controls.Add(container);

                return true;
            }

            // 2) Inline base64 (legacy / small images)
            if (!data.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var commaIndex = data.IndexOf(',');
            if (commaIndex < 0 || commaIndex == data.Length - 1)
            {
                return false;
            }

            try
            {
                var base64 = data[(commaIndex + 1)..];
                var bytes = Convert.FromBase64String(base64);
                _binaryPayload = bytes;
                using var ms = new MemoryStream(bytes);
                using var original = Image.FromStream(ms, useEmbeddedColorManagement: false, validateImageData: true);

                var preview = CreateThumbnail(original, 128, 128);

                // Centered preview: wrap in a panel to control width
                var horizontalPadding = Math.Max(0, (_flpContent.MaximumSize.Width - preview.Width) / 2);
                var container = new Panel
                {
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    Margin = new Padding(0, 4, 0, 4),
                    Padding = new Padding(horizontalPadding, 0, 0, 0),
                    BackColor = Color.Transparent
                };

                var pic = new PictureBox
                {
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Size = preview.Size,
                    Image = preview,
                    Margin = new Padding(2),
                    Cursor = Cursors.Hand
                };

                var menu = AttachImageContextMenu(pic, bytes);
                container.ContextMenuStrip = menu;

                container.Controls.Add(pic);
                _flpContent.FlowDirection = FlowDirection.LeftToRight;
                _flpContent.Controls.Add(container);

                pic.Disposed += (_, __) => preview.Dispose();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TryAddVideoFromContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return false;

            var data = content.Trim();

            byte[]? bytes = null;
            if (data.StartsWith("data:video", StringComparison.OrdinalIgnoreCase))
            {
                var commaIndex = data.IndexOf(',');
                if (commaIndex < 0 || commaIndex == data.Length - 1)
                {
                    return false;
                }

                try
                {
                    var base64 = data[(commaIndex + 1)..];
                    bytes = Convert.FromBase64String(base64);
                    _binaryPayload = bytes; // reuse field for payload storage
                    _videoSourceUrl = null;
                }
                catch
                {
                    return false;
                }
            }
            else if (Uri.TryCreate(data, UriKind.Absolute, out var uri) &&
                     (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeFile))
            {
                _binaryPayload = null;
                _videoSourceUrl = uri.ToString();
            }
            else
            {
                return false;
            }

            try
            {
                var preview = CreateVideoBadge(VideoThumbSize.Width, VideoThumbSize.Height);

                var horizontalPadding = Math.Max(0, (_flpContent.MaximumSize.Width - preview.Width) / 2);
                var container = new Panel
                {
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    Margin = new Padding(0, 4, 0, 4),
                    Padding = new Padding(horizontalPadding, 0, 0, 0),
                    BackColor = Color.Transparent
                };

                var pic = new PictureBox
                {
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Size = preview.Size,
                    Image = preview,
                    Margin = new Padding(2),
                    Cursor = Cursors.Hand
                };

                var menu = AttachVideoContextMenu(pic, bytes, _videoSourceUrl);
                container.ContextMenuStrip = menu;

                container.Controls.Add(pic);
                _flpContent.FlowDirection = FlowDirection.LeftToRight;
                _flpContent.Controls.Add(container);

                pic.Disposed += (_, __) => preview.Dispose();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TryAddCallSummary(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return false;

            CallLogDto? log = null;
            try
            {
                log = JsonSerializer.Deserialize<CallLogDto>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { }

            if (log == null)
            {
                return false;
            }

            var statusLabel = log.Result?.ToLowerInvariant() switch
            {
                "rejected" => "refusé",
                "cancelled" => "annulé",
                "missed" => "manqué",
                "busy" => "occupé / en appel",
                _ => "terminé"
            };

            var duration = TimeSpan.FromSeconds(log.DurationSeconds);
            var durationText = duration.TotalSeconds > 0 ? duration.ToString("mm\\:ss") : "--:--";
            var text = $"Appel {statusLabel} • {durationText} • {log.StartedAt.ToLocalTime():HH:mm}";

            var container = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 4, 0, 4),
                Padding = new Padding(6),
                BackColor = Color.FromArgb(245, 246, 248)
            };

            var pic = new PictureBox
            {
                Size = new Size(32, 32),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = TryLoadIcon(@"C:\Users\azizi\OneDrive\Desktop\PaL.X\Voice\raccrocher.png") ?? VoiceIcons.HangupIcon ?? VoiceIcons.VolumeIcon,
                Margin = new Padding(4)
            };

            var lbl = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Text = text,
                ForeColor = Color.FromArgb(40, 40, 40),
                Margin = new Padding(4, 8, 4, 4)
            };

            container.Controls.Add(pic);
            container.Controls.Add(lbl);
            container.FlowDirection = FlowDirection.LeftToRight;

            _flpContent.FlowDirection = FlowDirection.LeftToRight;
            _flpContent.Controls.Add(container);
            return true;
        }

        private bool TryAddAttachmentFromContent(string content, bool isAudio, bool isVoice)
        {
            if (string.IsNullOrWhiteSpace(content)) return false;

            var data = content.Trim();
            byte[]? bytes = null;
            string? url = null;
            string? fileName = null;
            string? mime = null;

            if (Uri.TryCreate(data, UriKind.Absolute, out var uri) &&
                     (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeFile))
            {
                url = uri.ToString();
                fileName = Path.GetFileName(uri.AbsolutePath);
                mime = GuessMimeFromExtension(Path.GetExtension(uri.AbsolutePath));
            }
            else
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                var ext = GetExtensionFromNameOrMime(null, mime, isAudio);
                fileName = (isAudio ? "audio" : "fichier") + ext;
            }

            _binaryPayload = bytes;
            _attachmentSourceUrl = url;
            _attachmentFileName = fileName;
            _attachmentMime = mime;
            _isAudioAttachment = isAudio || isVoice || (mime?.StartsWith("audio", StringComparison.OrdinalIgnoreCase) == true);
            _isVoiceAttachment = isVoice;

            try
            {
                var badgeSize = isVoice
                    ? new Size(24, 24)
                    : (_isAudioAttachment ? new Size(32, 32) : VideoThumbSize);

                var badge = CreateAttachmentBadge(isVoice ? AttachmentKind.Voice : (_isAudioAttachment ? AttachmentKind.Audio : AttachmentKind.File), badgeSize.Width, badgeSize.Height);

                var horizontalPadding = Math.Max(0, (_flpContent.MaximumSize.Width - badge.Width) / 2);
                var container = new Panel
                {
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    Margin = new Padding(0, 4, 0, 4),
                    Padding = new Padding(horizontalPadding, 0, 0, 0),
                    BackColor = Color.Transparent
                };

                var pic = new PictureBox
                {
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Size = badge.Size,
                    Image = badge,
                    Margin = new Padding(2),
                    Cursor = Cursors.Hand
                };

                // Keep a reference so we can rebind actions when the server sends back the final URL
                if (_attachmentPictureBox != null)
                {
                    _attachmentPictureBox.Click -= AttachmentPic_Click;
                    _attachmentPictureBox.DoubleClick -= AttachmentPic_Click;
                }

                _attachmentPictureBox = pic;
                pic.Click += AttachmentPic_Click;
                pic.DoubleClick += AttachmentPic_Click;

                _attachmentContextMenu?.Dispose();
                _attachmentContextMenu = AttachAttachmentContextMenu(pic, bytes, url, fileName, _isAudioAttachment, mime);
                container.ContextMenuStrip = _attachmentContextMenu;

                container.Controls.Add(pic);
                _flpContent.FlowDirection = FlowDirection.LeftToRight;
                _flpContent.Controls.Add(container);

                pic.Disposed += (_, __) => badge.Dispose();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async void AttachmentPic_Click(object? sender, EventArgs e)
        {
            await OpenAttachmentAsync(_binaryPayload, _attachmentSourceUrl, _attachmentFileName, _attachmentMime, _isAudioAttachment || _isVoiceAttachment);
        }

        private static Bitmap CreateThumbnail(Image source, int maxWidth, int maxHeight)
        {
            var ratio = Math.Min((double)maxWidth / source.Width, (double)maxHeight / source.Height);
            ratio = Math.Min(1.0, ratio);

            var width = (int)Math.Round(source.Width * ratio);
            var height = (int)Math.Round(source.Height * ratio);

            var thumb = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(thumb);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);
            g.DrawImage(source, new Rectangle(0, 0, width, height));
            return thumb;
        }

        private static Bitmap ResizeWithPadding(Image source, int targetWidth, int targetHeight)
        {
            var bmp = new Bitmap(targetWidth, targetHeight, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);

            var ratio = Math.Min((double)targetWidth / source.Width, (double)targetHeight / source.Height);
            var width = (int)Math.Round(source.Width * ratio);
            var height = (int)Math.Round(source.Height * ratio);
            var x = (targetWidth - width) / 2;
            var y = (targetHeight - height) / 2;

            g.DrawImage(source, new Rectangle(x, y, width, height));
            return bmp;
        }

        private static void FillRoundedRectangle(Graphics g, Brush brush, Rectangle bounds, int radius)
        {
            using var path = RoundedRect(bounds, radius);
            g.FillPath(brush, path);
        }

        private static void DrawRoundedRectangle(Graphics g, Pen pen, Rectangle bounds, int radius)
        {
            using var path = RoundedRect(bounds, radius);
            g.DrawPath(pen, path);
        }

        private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int d = radius * 2;
            var arc = new Rectangle(bounds.Location, new Size(d, d));

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - d;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - d;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        public void SetSendProgress(int percent)
        {
            _sendState = SendState.Sending;
            _statusPanel.Visible = true;
            _progressBar.Visible = true;
            _progressLabel.Visible = true;
            _stateIcon.Visible = false;
            _retryLink.Visible = false;

            var clamped = Math.Max(0, Math.Min(100, percent));
            _progressBar.Value = clamped;
            _progressLabel.Text = $"{clamped}%";
        }

        public void MarkSendSuccess()
        {
            _sendState = SendState.Success;
            _statusPanel.Visible = true;
            _progressBar.Visible = false;
            _progressLabel.Visible = false;
            _stateIcon.Visible = true;
            _stateIcon.Image = SystemIcons.Shield.ToBitmap();
            _retryLink.Visible = false;
        }

        public void MarkSendError(string? message = null)
        {
            _sendState = SendState.Error;
            _statusPanel.Visible = true;
            _progressBar.Visible = false;
            _progressLabel.Visible = false;
            _stateIcon.Visible = true;
            _stateIcon.Image = SystemIcons.Error.ToBitmap();
            _retryLink.Visible = true;

            if (!string.IsNullOrWhiteSpace(message))
            {
                var tt = new ToolTip();
                tt.SetToolTip(_stateIcon, message);
            }
        }

        public void UpdateVideoSource(string? newUrl, byte[]? payload = null)
        {
            _videoSourceUrl = newUrl;
            if (payload != null)
            {
                _binaryPayload = payload;
            }
        }

        public void UpdateTimestamp(DateTime time)
        {
            Timestamp = time;
            _lblTime.Text = time.ToShortTimeString();
        }

        private ContextMenuStrip AttachImageContextMenu(PictureBox pic, byte[] bytes)
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add(new ToolStripMenuItem("Ouvrir l'image", IconOpen, (s, e) => ShowFullSize(bytes, _imageUrl)));
            menu.Items.Add(new ToolStripMenuItem("Enregistrer sous...", IconSave, (s, e) => SaveImageAs(bytes)));

            var copyItem = new ToolStripMenuItem("Copier le lien", IconCopyLink, (s, e) => Clipboard.SetText(_imageUrl ?? string.Empty))
            {
                Enabled = !string.IsNullOrWhiteSpace(_imageUrl)
            };
            menu.Items.Add(copyItem);

            pic.ContextMenuStrip = menu;
            pic.Click += (s, e) => ShowFullSize(bytes);
            return menu;
        }

        private ContextMenuStrip AttachRemoteImageContextMenu(PictureBox pic, string url)
        {
            var menu = new ContextMenuStrip();

            menu.Items.Add(new ToolStripMenuItem("Ouvrir l'image", IconOpen, async (s, e) => await ShowFullSizeRemoteAsync(url)));
            menu.Items.Add(new ToolStripMenuItem("Enregistrer sous...", IconSave, async (s, e) => await SaveRemoteImageAsync(url)));
            menu.Items.Add(new ToolStripMenuItem("Copier le lien", IconCopyLink, (s, e) => Clipboard.SetText(url)));

            pic.ContextMenuStrip = menu;
            pic.Click += async (s, e) => await ShowFullSizeRemoteAsync(url);
            return menu;
        }

        private ContextMenuStrip AttachVideoContextMenu(PictureBox pic, byte[]? bytes, string? videoUrl)
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add(new ToolStripMenuItem("Lire", IconOpen, async (s, e) => await PlayVideoAsync(bytes, videoUrl, fullscreen:false)));
            menu.Items.Add(new ToolStripMenuItem("Plein écran", IconFullSize, async (s, e) => await PlayVideoAsync(bytes, videoUrl, fullscreen:true)));
            menu.Items.Add(new ToolStripMenuItem("Enregistrer sous…", IconSave, async (s, e) => await SaveVideoAsAsync(bytes, videoUrl)));
            var copyItem = new ToolStripMenuItem("Copier le lien", IconCopyLink, (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(videoUrl))
                {
                    Clipboard.SetText(videoUrl);
                }
            })
            {
                Enabled = !string.IsNullOrWhiteSpace(videoUrl)
            };
            menu.Items.Add(copyItem);

            pic.ContextMenuStrip = menu;
            pic.Click += async (s, e) => await PlayVideoAsync(bytes, videoUrl, fullscreen:false);
            return menu;
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

        private void OpenImage(byte[] bytes)
        {
            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), $"palx_img_{Guid.NewGuid():N}.png");
                File.WriteAllBytes(tempPath, bytes);
                Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
            }
            catch
            {
                // Best effort
            }
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch
            {
                // ignore
            }
        }

        private async Task<byte[]?> FetchImageBytesAsync(string url)
        {
            try
            {
                using var http = new HttpClient();
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("image/*"));
                var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
                if (!resp.IsSuccessStatusCode)
                {
                    return null;
                }

                return await resp.Content.ReadAsByteArrayAsync();
            }
            catch
            {
                return null;
            }
        }

        private async Task ShowFullSizeRemoteAsync(string url)
        {
            var bytes = await FetchImageBytesAsync(url);
            if (bytes == null || bytes.Length == 0) return;
            ShowFullSize(bytes, url);
        }

        private async Task SaveRemoteImageAsync(string url)
        {
            var bytes = await FetchImageBytesAsync(url);
            if (bytes == null || bytes.Length == 0) return;
            SaveImageAs(bytes);
        }

        private async Task OpenRemoteImageAsync(string url)
        {
            var bytes = await FetchImageBytesAsync(url);
            if (bytes == null || bytes.Length == 0) return;
            OpenImage(bytes);
        }

        private async Task PlayVideoAsync(byte[]? bytes, string? videoUrl, bool fullscreen = false)
        {
            try
            {
                using var viewer = new VideoPlayerForm("Lecture vidéo") { StartInFullscreen = fullscreen };
                if (bytes != null)
                {
                    await viewer.LoadFromBytesAsync(bytes);
                }
                else if (!string.IsNullOrWhiteSpace(videoUrl))
                {
                    await viewer.LoadFromUrlAsync(videoUrl);
                }
                else
                {
                    return;
                }

                viewer.ShowDialog();
            }
            catch
            {
                // ignore
            }
        }

        private void SaveImageAs(byte[] bytes)
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "PNG Image|*.png|JPEG Image|*.jpg;*.jpeg|GIF Image|*.gif|All files|*.*",
                FileName = "image.png"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllBytes(sfd.FileName, bytes);
                }
                catch
                {
                    // ignore
                }
            }
        }

        private async Task SaveVideoAsAsync(byte[]? bytes, string? videoUrl)
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "Vidéo|*.mp4;*.mov;*.avi;*.webm|Tous les fichiers|*.*",
                FileName = "video.mp4"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if (bytes != null)
                    {
                        File.WriteAllBytes(sfd.FileName, bytes);
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(videoUrl))
                    {
                        if (Uri.TryCreate(videoUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeFile)
                        {
                            File.Copy(uri.LocalPath, sfd.FileName, overwrite: true);
                        }
                        else
                        {
                            using var http = new HttpClient();
                            var data = await http.GetByteArrayAsync(videoUrl);
                            File.WriteAllBytes(sfd.FileName, data);
                        }
                    }
                }
                catch
                {
                    // ignore
                }
            }
        }

        private void ShowFullSize(byte[] bytes, string? sourceUrl = null)
        {
            try
            {
                using var ms = new MemoryStream(bytes);
                using var img = Image.FromStream(ms);

                var viewer = new ImageViewerForm((Image)img.Clone(), sourceUrl);
                viewer.FormClosed += (_, __) => viewer.Dispose();
                viewer.Show();
            }
            catch
            {
                // ignore
            }
        }

        private sealed class ImageViewerForm : Form
        {
            private readonly PictureBox _pictureBox;
            private readonly Panel _scrollPanel;
            private readonly Image _image;
            private readonly string? _sourceUrl;

            public ImageViewerForm(Image image, string? sourceUrl)
            {
                _image = image;
                _sourceUrl = sourceUrl;

                Text = "Image (taille réelle)";
                BackColor = Color.White;
                StartPosition = FormStartPosition.CenterParent;

                var screen = Screen.FromPoint(Cursor.Position).WorkingArea;
                Size = new Size(Math.Min(image.Width + 80, (int)(screen.Width * 0.9)), Math.Min(image.Height + 120, (int)(screen.Height * 0.9)));

                _scrollPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    AutoScroll = true,
                    BackColor = Color.White
                };

                _pictureBox = new PictureBox
                {
                    Image = _image,
                    SizeMode = PictureBoxSizeMode.AutoSize,
                    BackColor = Color.Transparent,
                    Location = new Point(0, 0)
                };

                _scrollPanel.Controls.Add(_pictureBox);
                _scrollPanel.Resize += (_, __) => CenterImage();

                AttachContextMenu();

                Controls.Add(_scrollPanel);

                CenterImage();
            }

            private void AttachContextMenu()
            {
                var menu = new ContextMenuStrip();

                menu.Items.Add(new ToolStripMenuItem("Enregistrer sous", IconSave, (_, __) => SaveImageAs()));

                if (!string.IsNullOrWhiteSpace(_sourceUrl))
                {
                    menu.Items.Add(new ToolStripMenuItem("Copier le lien", IconCopyLink, (_, __) => Clipboard.SetText(_sourceUrl!)));
                }

                menu.Items.Add(new ToolStripMenuItem("Fermer", IconClose, (_, __) => Close()));

                _pictureBox.ContextMenuStrip = menu;
                _scrollPanel.ContextMenuStrip = menu;
            }

            private void CenterImage()
            {
                var x = Math.Max((_scrollPanel.ClientSize.Width - _pictureBox.Width) / 2, 0);
                var y = Math.Max((_scrollPanel.ClientSize.Height - _pictureBox.Height) / 2, 0);
                _pictureBox.Location = new Point(x, y);
            }

            private void SaveImageAs()
            {
                using var sfd = new SaveFileDialog
                {
                    FileName = "image.png",
                    Filter = "PNG (*.png)|*.png|JPEG (*.jpg)|*.jpg|Bitmap (*.bmp)|*.bmp|Tous les fichiers|*.*"
                };

                if (sfd.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    var format = ImageFormat.Png;
                    var ext = Path.GetExtension(sfd.FileName).ToLowerInvariant();
                    if (ext == ".jpg" || ext == ".jpeg") format = ImageFormat.Jpeg;
                    else if (ext == ".bmp") format = ImageFormat.Bmp;

                    _image.Save(sfd.FileName, format);
                }
                catch
                {
                    // ignore
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _pictureBox.Image = null;
                    _image.Dispose();
                }
                base.Dispose(disposing);
            }
        }

        private void ShowVideoDetails(byte[]? bytes, string? videoUrl)
        {
            try
            {
                string msg;
                if (bytes != null)
                {
                    var sizeKb = bytes.Length / 1024.0;
                    msg = $"Taille : {sizeKb:F1} KB\nFormat : vidéo intégrée";
                }
                else if (!string.IsNullOrWhiteSpace(videoUrl))
                {
                    msg = $"Vidéo distante\nURL : {videoUrl}";
                }
                else
                {
                    msg = "Vidéo : aucune donnée";
                }

                MessageBox.Show(msg, "Détails vidéo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch { }
        }

        private enum AttachmentKind
        {
            File,
            Audio,
            Voice
        }

        private static Image CreateAttachmentBadge(AttachmentKind kind, int width, int height)
        {
            Image? src = kind switch
            {
                AttachmentKind.Audio => AudioBadgeSource.Value,
                AttachmentKind.Voice => VoiceBadgeSource.Value,
                _ => FileBadgeSource.Value
            };
            if (src != null)
            {
                return ResizeWithPadding(src, width, height);
            }

            // Fallback flat badge
            var fallback = new Bitmap(width, height);
            using var g = Graphics.FromImage(fallback);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var bg = kind == AttachmentKind.Audio ? Color.FromArgb(40, 80, 140) : Color.FromArgb(38, 50, 68);
            using var bgBrush = new SolidBrush(bg);
            FillRoundedRectangle(g, bgBrush, new Rectangle(0, 0, width - 1, height - 1), 12);
            using var pen = new Pen(Color.FromArgb(120, Color.White), 2);
            DrawRoundedRectangle(g, pen, new Rectangle(1, 1, width - 3, height - 3), 10);

            var text = kind switch
            {
                AttachmentKind.Audio => "♪",
                AttachmentKind.Voice => "🎙",
                _ => "📄"
            };
            using var font = new Font("Segoe UI", Math.Max(12, width / 3f), FontStyle.Bold, GraphicsUnit.Pixel);
            var size = g.MeasureString(text, font);
            g.DrawString(text, font, Brushes.White, (width - size.Width) / 2f, (height - size.Height) / 2f);
            return fallback;
        }

        private ContextMenuStrip AttachAttachmentContextMenu(PictureBox pic, byte[]? bytes, string? url, string fileName, bool isAudio, string? mime)
        {
            var menu = new ContextMenuStrip();
            Image? badgeIcon = null;
            try
            {
                var src = isAudio ? AudioBadgeSource.Value : FileBadgeSource.Value;
                if (src != null)
                {
                    badgeIcon = ResizeWithPadding(src, 28, 28);
                }
            }
            catch { }

            var resolvedUrl = _attachmentSourceUrl ?? url;
            menu.Items.Add(new ToolStripMenuItem(isAudio ? "Lire / Ouvrir" : "Ouvrir", badgeIcon ?? IconOpen, async (_, __) => await OpenAttachmentAsync(_binaryPayload ?? bytes, resolvedUrl, _attachmentFileName ?? fileName, _attachmentMime ?? mime, _isAudioAttachment || _isVoiceAttachment)));
            menu.Items.Add(new ToolStripMenuItem("Enregistrer sous…", IconSave, async (_, __) => await SaveAttachmentAsAsync(_binaryPayload ?? bytes, resolvedUrl, _attachmentFileName ?? fileName, _attachmentMime ?? mime, _isAudioAttachment || _isVoiceAttachment)));

            var copyItem = new ToolStripMenuItem("Copier le lien", IconCopyLink, (_, __) =>
            {
                if (!string.IsNullOrWhiteSpace(resolvedUrl))
                {
                    Clipboard.SetText(resolvedUrl);
                }
            })
            {
                Enabled = !string.IsNullOrWhiteSpace(resolvedUrl)
            };
            menu.Items.Add(copyItem);

            pic.ContextMenuStrip = menu;
            menu.Disposed += (_, __) => badgeIcon?.Dispose();
            return menu;
        }

        public void UpdateAttachmentSource(string? newUrl, byte[]? payload, string? fileName, string? mime, bool isAudio, bool isVoice)
        {
            if (!string.IsNullOrWhiteSpace(newUrl))
            {
                _attachmentSourceUrl = newUrl;
            }

            if (payload != null && payload.Length > 0)
            {
                _binaryPayload = payload;
            }

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                _attachmentFileName = fileName;
            }

            if (!string.IsNullOrWhiteSpace(mime))
            {
                _attachmentMime = mime;
            }

            _isVoiceAttachment = isVoice;
            _isAudioAttachment = isAudio || isVoice || (_attachmentMime?.StartsWith("audio", StringComparison.OrdinalIgnoreCase) == true);

            if (_attachmentPictureBox != null)
            {
                _attachmentContextMenu?.Dispose();
                _attachmentContextMenu = AttachAttachmentContextMenu(_attachmentPictureBox, _binaryPayload, _attachmentSourceUrl, _attachmentFileName ?? "fichier", _isAudioAttachment, _attachmentMime);
                _attachmentPictureBox.ContextMenuStrip = _attachmentContextMenu;
            }
        }

        private async Task OpenAttachmentAsync(byte[]? bytes, string? url, string? fileName, string? mime, bool isAudio)
        {
            if (isAudio)
            {
                await PlayAudioAttachmentAsync(bytes, url, fileName, mime);
                return;
            }

            var localPath = await EnsureLocalAttachmentAsync(bytes, url, fileName, mime, isAudio);
            if (string.IsNullOrWhiteSpace(localPath))
            {
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(localPath) { UseShellExecute = true });
            }
            catch
            {
                // Best effort
            }
        }

        private async Task PlayAudioAttachmentAsync(byte[]? bytes, string? url, string? fileName, string? mime)
        {
            var localPath = await EnsureLocalAttachmentAsync(bytes, url, fileName, mime, isAudio: true);
            if (string.IsNullOrWhiteSpace(localPath))
            {
                return;
            }

            try
            {
                var title = "Lecture audio";
                var form = new AudioPlayerForm(title)
                {
                    AutoPlay = true
                };

                // If we created a temp file (bytes or remote URL), allow cleanup on close
                var deleteOnClose = bytes != null || (url?.StartsWith("http", StringComparison.OrdinalIgnoreCase) == true);
                await form.LoadFromPathAsync(localPath, deleteOnClose);
                form.Show();
            }
            catch
            {
                // Ignore playback failures to avoid crashing UI
            }
        }

        private async Task SaveAttachmentAsAsync(byte[]? bytes, string? url, string? fileName, string? mime, bool isAudio)
        {
            var suggested = string.IsNullOrWhiteSpace(fileName)
                ? (isAudio ? "audio" : "fichier") + GetExtensionFromNameOrMime(fileName, mime, isAudio)
                : fileName;

            using var sfd = new SaveFileDialog
            {
                FileName = suggested,
                Filter = "Tous les fichiers|*.*"
            };

            if (sfd.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            try
            {
                if (bytes != null)
                {
                    await File.WriteAllBytesAsync(sfd.FileName, bytes);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(url))
                {
                    var data = await DownloadBytesAsync(url);
                    if (data != null)
                    {
                        await File.WriteAllBytesAsync(sfd.FileName, data);
                    }
                }
            }
            catch
            {
                // ignore
            }
        }

        private void ShowAttachmentDetails(string? fileName, string? url, string? mime, byte[]? bytes)
        {
            try
            {
                var sb = new StringBuilder();
                if (!string.IsNullOrWhiteSpace(fileName)) sb.AppendLine($"Nom : {fileName}");
                if (!string.IsNullOrWhiteSpace(url)) sb.AppendLine($"URL : {url}");
                if (!string.IsNullOrWhiteSpace(mime)) sb.AppendLine($"MIME : {mime}");
                if (bytes != null) sb.AppendLine($"Taille : {bytes.Length / 1024.0:F1} KB");
                if (sb.Length == 0) sb.AppendLine("Aucun détail disponible.");

                MessageBox.Show(sb.ToString(), "Détails", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch { }
        }

        private async Task<string?> EnsureLocalAttachmentAsync(byte[]? bytes, string? url, string? fileName, string? mime, bool isAudio)
        {
            try
            {
                if (bytes != null)
                {
                    var ext = GetExtensionFromNameOrMime(fileName, mime, isAudio);
                    var tempPath = Path.Combine(Path.GetTempPath(), $"palx_att_{Guid.NewGuid():N}{ext}");
                    await File.WriteAllBytesAsync(tempPath, bytes);
                    return tempPath;
                }

                if (!string.IsNullOrWhiteSpace(url))
                {
                    var data = await DownloadBytesAsync(url);
                    if (data == null)
                    {
                        return null;
                    }

                    var ext = GetExtensionFromNameOrMime(fileName, mime, isAudio);
                    var tempPath = Path.Combine(Path.GetTempPath(), $"palx_att_{Guid.NewGuid():N}{ext}");
                    await File.WriteAllBytesAsync(tempPath, data);
                    return tempPath;
                }
            }
            catch
            {
                // ignore
            }

            return null;
        }

        private static async Task<byte[]?> DownloadBytesAsync(string url)
        {
            try
            {
                using var client = new HttpClient();
                return await client.GetByteArrayAsync(url);
            }
            catch
            {
                return null;
            }
        }

        private static string GetExtensionFromNameOrMime(string? fileName, string? mime, bool isAudio)
        {
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                var ext = Path.GetExtension(fileName);
                if (!string.IsNullOrWhiteSpace(ext))
                {
                    return ext;
                }
            }

            if (!string.IsNullOrWhiteSpace(mime))
            {
                var lower = mime.ToLowerInvariant();
                if (lower.Contains("mpeg")) return ".mp3";
                if (lower.Contains("wav")) return ".wav";
                if (lower.Contains("ogg")) return ".ogg";
                if (lower.Contains("flac")) return ".flac";
                if (lower.Contains("m4a")) return ".m4a";
                if (lower.Contains("pdf")) return ".pdf";
                if (lower.Contains("zip")) return ".zip";
                if (lower.Contains("json")) return ".json";
                if (lower.Contains("csv")) return ".csv";
            }

            return isAudio ? ".mp3" : ".bin";
        }

        private static string? GuessMimeFromExtension(string? extension)
        {
            if (string.IsNullOrWhiteSpace(extension)) return null;
            extension = extension.ToLowerInvariant();
            return extension switch
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
                _ => "application/octet-stream"
            };
        }

        private static Image CreateVideoBadge(int width, int height)
        {
            var res = VideoBadgeSource.Value;
            if (res != null)
            {
                return ResizeWithPadding(res, width, height);
            }

            var fallback = new Bitmap(width, height);
            using var g = Graphics.FromImage(fallback);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var bgColor = Color.FromArgb(38, 50, 68);
            var accent = Color.FromArgb(72, 142, 245);
            using var bgBrush = new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(0, 0, width, height), bgColor, Color.FromArgb(28, 38, 52), 45f);
            FillRoundedRectangle(g, bgBrush, new Rectangle(0, 0, width - 1, height - 1), 14);

            using var borderPen = new Pen(Color.FromArgb(80, Color.White), 2);
            DrawRoundedRectangle(g, borderPen, new Rectangle(1, 1, width - 3, height - 3), 12);

            // Draw simplified clapper lines
            using var accentPen = new Pen(accent, 3);
            g.DrawLine(accentPen, width * 0.18f, height * 0.28f, width * 0.82f, height * 0.28f);
            g.DrawLine(accentPen, width * 0.18f, height * 0.36f, width * 0.82f, height * 0.36f);

            // Play triangle
            var center = new PointF(width / 2f, height / 2f + 6);
            var tri = new[]
            {
                new PointF(center.X - width * 0.12f, center.Y - height * 0.12f),
                new PointF(center.X - width * 0.12f, center.Y + height * 0.12f),
                new PointF(center.X + width * 0.16f, center.Y),
            };
            using var playBrush = new SolidBrush(Color.White);
            g.FillPolygon(playBrush, tri);

            return fallback;
        }

        private void AddFormattedTextRange(int start, int end)
        {
            if (end <= start)
            {
                return;
            }

            _rtbTemp.Select(start, end - start);
            string segment = _rtbTemp.SelectedText;
            if (string.IsNullOrEmpty(segment))
            {
                return;
            }

            segment = segment.Replace("\u200B", string.Empty)
                             .Replace("\uFFFC", string.Empty)
                             .Replace("\uFEFF", string.Empty);

            // Capture formatting from the first character of the range
            // This is a simplification; mixed formatting within a range isn't fully supported by this Label approach
            var selectionFont = _rtbTemp.SelectionFont;
            var font = selectionFont != null ? (Font)selectionFont.Clone() : new Font("Segoe UI", 10, FontStyle.Regular);
            var color = _rtbTemp.SelectionColor;
            if (color.IsEmpty) color = Color.Black;

            AddText(segment, font, color);
        }

        private void AddText(string text)
        {
            AddText(text, new Font("Segoe UI", 10, FontStyle.Regular), Color.Black);
        }

        private void AddText(string text, Font font, Color color)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var sanitized = text.Replace("\u200B", string.Empty)
                                .Replace("\uFFFC", string.Empty)
                                .Replace("\uFEFF", string.Empty);
            if (string.IsNullOrEmpty(sanitized))
            {
                return;
            }

            var lbl = new Label
            {
                Text = sanitized,
                Font = font,
                ForeColor = color.IsEmpty ? Color.Black : color,
                AutoSize = true,
                MaximumSize = new Size(380, 0),
                Margin = new Padding(0, 5, 0, 5),
                BackColor = Color.Transparent
            };

            _flpContent.Controls.Add(lbl);
        }

        private static Queue<Image> ExtractImagesFromRtf(string rtfContent)
        {
            var queue = new Queue<Image>();
            if (string.IsNullOrEmpty(rtfContent))
            {
                System.Diagnostics.Debug.WriteLine("[SMILEY] RTF content is null or empty");
                return queue;
            }

            var matches = RtfImageRegex.Matches(rtfContent);
            System.Diagnostics.Debug.WriteLine($"[SMILEY] Found {matches.Count} RTF image matches");

            foreach (Match match in matches)
            {
                var hexGroup = match.Groups[1];
                if (!hexGroup.Success)
                {
                    System.Diagnostics.Debug.WriteLine("[SMILEY] Hex group not found in match");
                    continue;
                }

                var hex = Regex.Replace(hexGroup.Value, @"[^0-9A-Fa-f]", string.Empty);
                if (hex.Length < 2)
                {
                    System.Diagnostics.Debug.WriteLine($"[SMILEY] Hex too short: {hex.Length} chars");
                    continue;
                }

                if (!TryParseHexToBytes(hex, out var bytes))
                {
                    System.Diagnostics.Debug.WriteLine($"[SMILEY] Failed to parse hex to bytes");
                    continue;
                }

                try
                {
                    using var ms = new MemoryStream(bytes);
                    var image = Image.FromStream(ms, useEmbeddedColorManagement: false, validateImageData: true);
                    queue.Enqueue(image);
                    System.Diagnostics.Debug.WriteLine($"[SMILEY] Successfully extracted image: {image.Width}x{image.Height}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SMILEY] Failed to create image from bytes: {ex.Message}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"[SMILEY] Total images extracted: {queue.Count}");
            return queue;
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

        private static string SanitizeSmileyFileName(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(filename.Length);
            foreach (var ch in filename)
            {
                if (ch == '\u200B' || ch == '\uFEFF' || ch == '?')
                {
                    continue;
                }

                if (char.IsControl(ch))
                {
                    continue;
                }

                builder.Append(ch);
            }

            return builder.ToString().Trim();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _rtbTemp?.Dispose();
            }
            base.Dispose(disposing);
        }

    }
}
