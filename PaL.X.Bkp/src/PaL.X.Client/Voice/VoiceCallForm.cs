using System;
using System.Drawing;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
using PaL.X.Client.Services;

namespace PaL.X.Client.Voice
{
    /// <summary>
    /// Fenêtre d’appel vocal (UI only). Événements exposés pour branchement SignalR/WebRTC ultérieur.
    /// </summary>
    public class VoiceCallForm : Form
    {
        private readonly Label _lblName = new();
        private readonly Label _lblStatus = new();
        private readonly Label _lblDuration = new();
        private readonly PictureBox _picAvatar = new();
        private readonly Timer _durationTimer = new();
        private readonly DateTime _startedAt;

    private readonly Button _btnMute = new();
    private readonly Button _btnVolume = new();
    private readonly Button _btnHangup = new();
    private readonly Button _btnAccept = new();
    private readonly Button _btnReject = new();
    private readonly Button _btnOpenChat = new();
    private readonly ToolTip _ttButtons = new();

    private FlowLayoutPanel? _buttonsPanel;

        private readonly TrackBar _volumeSlider = new();
        private readonly Panel _volumePanel = new();

        private bool _muted;
        private readonly bool _incoming;
        private bool _isInCallMode;

        public event EventHandler? HangupRequested;
        public event EventHandler<bool>? MuteToggled;
        public event EventHandler<int>? VolumeChanged;
        public event EventHandler? AcceptRequested;
        public event EventHandler? RejectRequested;
        public event EventHandler? OpenChatRequested;

        public bool IsMuted => _muted;
        public bool IsInCallMode => _isInCallMode;
        public string CurrentStatus => _lblStatus.Text;
        public bool IsIncoming => _incoming;

        public VoiceCallForm(string peerName, Image? avatar = null, bool incoming = false)
        {
            Text = "Appel vocal";
            BackColor = Color.White;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = true;
            Size = new Size(440, 300);

            _ttButtons.ShowAlways = true;

            _incoming = incoming;
            _isInCallMode = !incoming; // sorties: déjà en mode appel; entrantes: en attente
            _startedAt = DateTime.UtcNow;

            BuildLayout(peerName, avatar);

            _durationTimer.Interval = 1000;
            _durationTimer.Tick += (_, __) => UpdateDuration();
            _durationTimer.Start();

            UpdateDuration();
            UpdateMuteUi();

            if (_incoming)
            {
                _lblStatus.Text = "Appel entrant";
            }
        }

        private void BuildLayout(string peerName, Image? avatar)
        {
            _picAvatar.Size = new Size(64, 64);
            _picAvatar.SizeMode = PictureBoxSizeMode.Zoom;
            _picAvatar.Image = avatar ?? VoiceIcons.IncomingIcon;
            _picAvatar.BackColor = Color.White;
            _picAvatar.Location = new Point(20, 20);

            _lblName.Text = peerName;
            _lblName.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            _lblName.ForeColor = Color.FromArgb(32, 32, 32);
            _lblName.Location = new Point(100, 20);
            _lblName.AutoSize = true;

            _lblStatus.Text = "Connexion…";
            _lblStatus.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            _lblStatus.ForeColor = Color.FromArgb(90, 90, 90);
            _lblStatus.Location = new Point(100, 48);
            _lblStatus.AutoSize = true;

            _lblDuration.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            _lblDuration.ForeColor = Color.FromArgb(90, 90, 90);
            _lblDuration.Location = new Point(100, 70);
            _lblDuration.AutoSize = true;

            var buttonsPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Dock = DockStyle.Bottom,
                Height = 110,
                Padding = new Padding(12, 10, 12, 10),
                BackColor = Color.FromArgb(245, 246, 248)
            };

            ConfigureIconButton(_btnMute, VoiceIcons.MicMuteIcon, "Couper/activer le micro", (_, __) => ToggleMute());
            ConfigureIconButton(_btnVolume, VoiceIcons.VolumeIcon, "Volume", (_, __) => ToggleVolumePanel());

            ConfigureIconButton(_btnOpenChat, VoiceIcons.OpenChatIcon, "Ouvrir chat privé", (_, __) => OpenChatRequested?.Invoke(this, EventArgs.Empty), new Size(44, 44));

            ConfigureIconButton(_btnHangup, VoiceIcons.HangupIcon, "Raccrocher", (_, __) => HangupRequested?.Invoke(this, EventArgs.Empty));
            _btnHangup.BackColor = Color.FromArgb(232, 67, 67);
            _btnHangup.ForeColor = Color.White;

            ConfigureIconButton(_btnAccept, VoiceIcons.AcceptIcon, "Accepter l'appel", (_, __) => AcceptRequested?.Invoke(this, EventArgs.Empty));
            _btnAccept.BackColor = Color.FromArgb(54, 179, 126);
            _btnAccept.ForeColor = Color.White;

            ConfigureIconButton(_btnReject, VoiceIcons.HangupIcon, "Refuser l'appel", (_, __) => RejectRequested?.Invoke(this, EventArgs.Empty));
            _btnReject.BackColor = Color.FromArgb(232, 67, 67);
            _btnReject.ForeColor = Color.White;

            _volumePanel.Visible = false;
            _volumePanel.AutoSize = true;
            _volumePanel.Padding = new Padding(6);
            _volumePanel.BackColor = Color.White;
            _volumePanel.BorderStyle = BorderStyle.FixedSingle;

            _volumeSlider.Orientation = Orientation.Horizontal;
            _volumeSlider.Minimum = 0;
            _volumeSlider.Maximum = 100;
            _volumeSlider.Value = 80;
            _volumeSlider.TickFrequency = 20;
            _volumeSlider.Width = 160;
            _volumeSlider.Scroll += (_, __) => VolumeChanged?.Invoke(this, _volumeSlider.Value);

            _volumePanel.Controls.Add(_volumeSlider);

            if (_incoming)
            {
                buttonsPanel.Controls.Add(_btnOpenChat);
                buttonsPanel.Controls.Add(_btnAccept);
                buttonsPanel.Controls.Add(_btnReject);
            }
            else
            {
                buttonsPanel.Controls.Add(_btnOpenChat);
                buttonsPanel.Controls.Add(_btnMute);
                buttonsPanel.Controls.Add(_btnVolume);
                buttonsPanel.Controls.Add(_volumePanel);
                buttonsPanel.Controls.Add(_btnHangup);
            }

            Controls.Add(_picAvatar);
            Controls.Add(_lblName);
            Controls.Add(_lblStatus);
            Controls.Add(_lblDuration);
            Controls.Add(buttonsPanel);
            _buttonsPanel = buttonsPanel;
        }

        private static void ConfigureButton(Button btn, string text, Image? icon, EventHandler onClick)
        {
            btn.Text = " " + text;
            btn.Image = icon;
            btn.ImageAlign = ContentAlignment.MiddleLeft;
            btn.TextAlign = ContentAlignment.MiddleRight;
            btn.AutoSize = true;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(220, 220, 220);
            btn.BackColor = Color.White;
            btn.ForeColor = Color.FromArgb(40, 40, 40);
            btn.Padding = new Padding(10, 6, 10, 6);
            btn.Margin = new Padding(4, 0, 4, 0);
            btn.Click += onClick;
        }

        private void ConfigureIconButton(Button btn, Image? icon, string tooltip, EventHandler onClick, Size? size = null)
        {
            btn.Text = string.Empty;
            btn.Image = icon;
            btn.ImageAlign = ContentAlignment.MiddleCenter;
            btn.AutoSize = false;
            btn.Size = size ?? new Size(44, 44);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(220, 220, 220);
            btn.BackColor = Color.White;
            btn.ForeColor = Color.FromArgb(40, 40, 40);
            btn.Padding = new Padding(6);
            btn.Margin = new Padding(6, 0, 6, 0);
            btn.Click += onClick;

            if (_ttButtons != null)
            {
                _ttButtons.SetToolTip(btn, tooltip);
            }
        }

        private void ToggleMute()
        {
            _muted = !_muted;
            UpdateMuteUi();
            MuteToggled?.Invoke(this, _muted);
        }

        private void ToggleVolumePanel()
        {
            _volumePanel.Visible = !_volumePanel.Visible;
        }

        public void SwitchToInCallMode()
        {
            if (_buttonsPanel != null)
            {
                _buttonsPanel.Controls.Clear();
                _buttonsPanel.Controls.Add(_btnOpenChat);
                _buttonsPanel.Controls.Add(_btnMute);
                _buttonsPanel.Controls.Add(_btnVolume);
                _buttonsPanel.Controls.Add(_volumePanel);
                _buttonsPanel.Controls.Add(_btnHangup);
            }
            _btnAccept.Visible = false;
            _btnReject.Visible = false;
            _btnMute.Visible = true;
            _btnVolume.Visible = true;
            _btnHangup.Visible = true;
            _lblStatus.Text = "En appel";
            _isInCallMode = true;
            UpdateMuteUi();
        }

        private void UpdateMuteUi()
        {
            _btnMute.Image = _muted ? VoiceIcons.MicMuteIcon : VoiceIcons.MicOnIcon;
            _lblStatus.Text = _muted ? "Micro coupé" : "En appel";
        }

        private void UpdateDuration()
        {
            var span = DateTime.UtcNow - _startedAt;
            _lblDuration.Text = $"Durée : {span:mm\\:ss}";
        }

        public void SetStatus(string status)
        {
            _lblStatus.Text = status;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _durationTimer.Stop();
                _durationTimer.Dispose();
                _picAvatar.Image = null;
            }
            base.Dispose(disposing);
        }
    }

    internal static class VoiceIcons
    {
        private static readonly Size MicSize = new Size(32, 32);

    public static Image? HangupIcon { get; } = LoadAbsolute(@"C:\Users\azizi\OneDrive\Desktop\PaL.X\Voice\hang-up.png", MicSize);
    public static Image? VolumeIcon { get; } = LoadAbsolute(@"C:\Users\azizi\OneDrive\Desktop\PaL.X\Voice\Volume.png");
    public static Image? IncomingIcon { get; } = LoadAbsolute(@"C:\Users\azizi\OneDrive\Desktop\PaL.X\Voice\appel_entrant.png");
    public static Image? AcceptIcon { get; } = LoadAbsolute(@"C:\Users\azizi\OneDrive\Desktop\PaL.X\Voice\accepter_call.png", MicSize)
                          ?? LoadAbsolute(@"C:\Users\azizi\OneDrive\Desktop\PaL.X\Voice\appel_entrant.png", MicSize);
        public static Image? MicOnIcon { get; } = LoadAbsolute(@"C:\Users\azizi\OneDrive\Desktop\PaL.X\Voice\mic_on.png", MicSize);
        public static Image? MicMuteIcon { get; } = LoadAbsolute(@"C:\Users\azizi\OneDrive\Desktop\PaL.X\Voice\mic_mute.png", MicSize);
        public static Image? MicOffIcon { get; } = LoadAbsolute(@"C:\Users\azizi\OneDrive\Desktop\PaL.X\Voice\mic_desabled.png", MicSize)
                                                    ?? LoadAbsolute(@"C:\Users\azizi\OneDrive\Desktop\PaL.X\Voice\mic_off.png", MicSize);
        public static Image? OpenChatIcon { get; } = LoadAbsolute(@"C:\Users\azizi\OneDrive\Desktop\PaL.X\various\pm_chat.png", new Size(32, 32));

        public static Image? LoadAbsolute(string path, Size? size = null)
        {
            try
            {
                if (!System.IO.File.Exists(path))
                {
                    return null;
                }

                using var src = Image.FromFile(path);
                if (size.HasValue)
                {
                    return new Bitmap(src, size.Value);
                }
                return new Bitmap(src);
            }
            catch
            {
                return null;
            }
        }
    }
}