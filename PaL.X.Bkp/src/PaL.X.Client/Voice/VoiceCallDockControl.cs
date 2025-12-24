using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WinFormsTimer = System.Windows.Forms.Timer;

namespace PaL.X.Client.Voice
{
    public enum DockPosition
    {
        Top,
        Bottom
    }

    public class VoiceCallDockControl : UserControl
    {
        private readonly Label _lblStatus = new();
        private readonly Label _lblDuration = new();
    private readonly Button _btnAccept = new();
        private readonly Button _btnMute = new();
        private readonly Button _btnHangup = new();
        private readonly TrackBar _volume = new();
        private readonly Button _btnToggleDock = new();
    private readonly WinFormsTimer _durationTimer = new();

        private DateTime _startedAtUtc = DateTime.UtcNow;
        private bool _muted;
        private DockPosition _dockPosition = DockPosition.Top;
    private bool _showAccept;

        public event EventHandler<bool>? MuteToggled;
        public event EventHandler? HangupClicked;
        public event EventHandler<int>? VolumeChanged;
        public event EventHandler<DockPosition>? DockToggleRequested;
    public event EventHandler? AcceptClicked;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DockPosition DockPosition
        {
            get => _dockPosition;
            set
            {
                _dockPosition = value;
                UpdateDockIcon();
            }
        }

        public VoiceCallDockControl()
        {
            Height = 90;
            Dock = DockStyle.Top;
            BackColor = Color.White;
            Padding = new Padding(10, 8, 10, 8);

            _lblStatus.AutoSize = true;
            _lblStatus.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            _lblStatus.ForeColor = Color.FromArgb(80, 80, 80);

            _lblDuration.AutoSize = true;
            _lblDuration.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            _lblDuration.ForeColor = Color.FromArgb(60, 60, 60);

            ConfigureIconButton(_btnAccept, VoiceIcons.AcceptIcon, "Accepter l'appel", (_, __) => AcceptClicked?.Invoke(this, EventArgs.Empty), Color.FromArgb(56, 170, 90));
            ConfigureIconButton(_btnMute, VoiceIcons.MicOnIcon, "Couper/activer le micro", (_, __) => ToggleMute());
            ConfigureIconButton(_btnHangup, VoiceIcons.HangupIcon, "Raccrocher", (_, __) => HangupClicked?.Invoke(this, EventArgs.Empty));
            _btnHangup.BackColor = Color.FromArgb(232, 67, 67);
            _btnHangup.ForeColor = Color.White;

            _volume.Orientation = Orientation.Horizontal;
            _volume.Minimum = 0;
            _volume.Maximum = 100;
            _volume.Value = 80;
            _volume.Width = 140;
            _volume.TickFrequency = 20;
            _volume.Scroll += (_, __) => VolumeChanged?.Invoke(this, _volume.Value);

            ConfigureIconButton(_btnToggleDock, VoiceIcons.IncomingIcon, "Changer le dock", (_, __) => DockToggleRequested?.Invoke(this, _dockPosition == DockPosition.Top ? DockPosition.Bottom : DockPosition.Top));

            var infoPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Margin = new Padding(8, 0, 8, 0)
            };
            infoPanel.Controls.Add(_lblStatus);
            infoPanel.Controls.Add(_lblDuration);

            var buttonsPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Margin = new Padding(8, 0, 0, 0)
            };
            buttonsPanel.Controls.Add(_btnAccept);
            buttonsPanel.Controls.Add(_btnMute);
            buttonsPanel.Controls.Add(_btnHangup);
            buttonsPanel.Controls.Add(_volume);
            buttonsPanel.Controls.Add(_btnToggleDock);

            var root = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false
            };
            root.Controls.Add(infoPanel);
            root.Controls.Add(buttonsPanel);

            Controls.Add(root);

            _durationTimer.Interval = 1000;
            _durationTimer.Tick += (_, __) => UpdateDuration();
            _durationTimer.Start();

            UpdateDockIcon();
            UpdateDuration();
            UpdateMuteIcon();
            UpdateAcceptVisibility();
        }

        private void UpdateDockIcon()
        {
            var iconPath = _dockPosition == DockPosition.Top
                ? @"C:\\Users\\azizi\\OneDrive\\Desktop\\PaL.X\\various\\en_bas.png"
                : @"C:\\Users\\azizi\\OneDrive\\Desktop\\PaL.X\\various\\en_haut.png";
            var icon = VoiceIcons.LoadAbsolute(iconPath, new Size(28, 28)) ?? _btnToggleDock.Image;
            if (icon != null)
            {
                _btnToggleDock.Image = icon;
            }
        }

        private void ConfigureIconButton(Button btn, Image? icon, string tooltip, EventHandler onClick, Color? backColor = null)
        {
            btn.Text = string.Empty;
            btn.Image = icon;
            btn.ImageAlign = ContentAlignment.MiddleCenter;
            btn.AutoSize = false;
            btn.Size = new Size(40, 40);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(220, 220, 220);
            btn.BackColor = backColor ?? Color.White;
            btn.ForeColor = Color.FromArgb(40, 40, 40);
            btn.Padding = new Padding(6);
            btn.Margin = new Padding(6, 0, 6, 0);
            btn.Click += onClick;
            var tt = new ToolTip { ShowAlways = true };
            tt.SetToolTip(btn, tooltip);
        }

        private void ToggleMute()
        {
            _muted = !_muted;
            UpdateMuteIcon();
            MuteToggled?.Invoke(this, _muted);
        }

        public void SetStatus(string status)
        {
            _lblStatus.Text = status;
            // Heuristic: show accept button for incoming mentions
            _showAccept = status.IndexOf("entrant", StringComparison.OrdinalIgnoreCase) >= 0;
            UpdateAcceptVisibility();
        }

        public void SetDurationStart(DateTime startedAtUtc)
        {
            _startedAtUtc = startedAtUtc;
            UpdateDuration();
        }

        private void UpdateDuration()
        {
            var elapsed = DateTime.UtcNow - _startedAtUtc;
            if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;
            _lblDuration.Text = elapsed.ToString("mm':'ss");
        }

        public void SetMuted(bool muted)
        {
            _muted = muted;
            UpdateMuteIcon();
        }

        public void SetVolume(int value)
        {
            _volume.Value = Math.Max(_volume.Minimum, Math.Min(_volume.Maximum, value));
        }

        public void SetAcceptVisible(bool visible)
        {
            _showAccept = visible;
            UpdateAcceptVisibility();
        }

        private void UpdateMuteIcon()
        {
            _btnMute.Image = _muted ? VoiceIcons.MicMuteIcon ?? _btnMute.Image : VoiceIcons.MicOnIcon ?? _btnMute.Image;
            _btnMute.BackColor = _muted ? Color.FromArgb(240, 210, 210) : Color.White;
        }

        private void UpdateAcceptVisibility()
        {
            _btnAccept.Visible = _showAccept;
            _btnAccept.Enabled = _showAccept;
        }
    }
}
