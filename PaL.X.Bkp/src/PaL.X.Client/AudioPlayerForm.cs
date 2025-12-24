using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using System.Windows.Threading;
using WinForms = System.Windows.Forms;

namespace PaL.X.Client
{
    public class AudioPlayerForm : WinForms.Form
    {
        private readonly MediaPlayer _player;
        private readonly DispatcherTimer _timer;
        private readonly WinForms.TrackBar _position;
        private readonly WinForms.TrackBar _volume;
        private readonly WinForms.Label _lblTime;
        private readonly WinForms.Button _btnPlayPause;
        private readonly WinForms.Button _btnStop;
        private readonly WinForms.Panel _panel;

        private TimeSpan _duration = TimeSpan.Zero;
        private bool _isDragging;
        private string? _ownedTempFile;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool AutoPlay { get; set; } = true;

        public AudioPlayerForm(string title)
        {
            Text = string.IsNullOrWhiteSpace(title) ? "Lecture audio" : title;
            StartPosition = WinForms.FormStartPosition.CenterParent;
            FormBorderStyle = WinForms.FormBorderStyle.FixedDialog;
            MinimumSize = new System.Drawing.Size(380, 200);
            Size = new System.Drawing.Size(420, 210);
            BackColor = System.Drawing.Color.FromArgb(245, 248, 252);

            _player = new MediaPlayer();
            _player.MediaOpened += Player_MediaOpened;
            _player.MediaEnded += (_, __) => ResetPosition();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _timer.Tick += Timer_Tick;

            _btnPlayPause = CreateButton("▶", (_, __) => TogglePlayPause());
            _btnStop = CreateButton("⏹", (_, __) => StopPlayback());

            _position = new WinForms.TrackBar
            {
                Dock = WinForms.DockStyle.Top,
                Minimum = 0,
                Maximum = 1000,
                TickFrequency = 0,
                Height = 32,
                Margin = new WinForms.Padding(16, 8, 16, 0),
                BackColor = System.Drawing.Color.White
            };
            _position.MouseDown += (_, __) => _isDragging = true;
            _position.MouseUp += (_, __) => { _isDragging = false; SeekFromSlider(); };
            _position.Scroll += (_, __) => { if (_isDragging) UpdateTimeLabelFromSlider(); };

            _volume = new WinForms.TrackBar
            {
                Dock = WinForms.DockStyle.Right,
                Minimum = 0,
                Maximum = 100,
                TickFrequency = 10,
                Width = 90,
                Value = 80,
                Orientation = WinForms.Orientation.Vertical,
                Margin = new WinForms.Padding(8, 8, 8, 8),
                BackColor = System.Drawing.Color.White
            };
            _volume.Scroll += (_, __) => _player.Volume = _volume.Value / 100.0;
            _player.Volume = 0.8;

            _lblTime = new WinForms.Label
            {
                Text = "00:00 / 00:00",
                Dock = WinForms.DockStyle.Top,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Padding = new WinForms.Padding(0, 6, 0, 6),
                ForeColor = System.Drawing.Color.FromArgb(33, 43, 54),
                Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold)
            };

            var controls = new WinForms.FlowLayoutPanel
            {
                Dock = WinForms.DockStyle.Fill,
                FlowDirection = WinForms.FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new WinForms.Padding(12, 8, 12, 8),
                AutoSize = true,
                AutoSizeMode = WinForms.AutoSizeMode.GrowAndShrink,
                BackColor = System.Drawing.Color.FromArgb(250, 252, 255)
            };
            controls.Controls.Add(_btnPlayPause);
            controls.Controls.Add(_btnStop);

            _panel = new WinForms.Panel
            {
                Dock = WinForms.DockStyle.Fill,
                BackColor = System.Drawing.Color.FromArgb(240, 243, 248)
            };
            _panel.Controls.Add(controls);
            _panel.Controls.Add(_volume);

            Controls.Add(_panel);
            Controls.Add(_lblTime);
            Controls.Add(_position);

            FormClosing += (_, __) => Cleanup();
        }

        public async Task LoadFromBytesAsync(byte[] data, string suggestedExtension = ".mp3")
        {
            _ownedTempFile = Path.Combine(Path.GetTempPath(), $"palx_audio_{Guid.NewGuid():N}{suggestedExtension}");
            await File.WriteAllBytesAsync(_ownedTempFile, data);
            LoadFromPath(_ownedTempFile);
        }

        public Task LoadFromPathAsync(string path, bool deleteOnClose)
        {
            if (deleteOnClose)
            {
                _ownedTempFile = path;
            }
            LoadFromPath(path);
            return Task.CompletedTask;
        }

        private void LoadFromPath(string path)
        {
            _player.Stop();
            _player.Open(new Uri(path));
            if (AutoPlay)
            {
                _player.Play();
                _btnPlayPause.Text = "⏸";
            }
        }

        private void Player_MediaOpened(object? sender, EventArgs e)
        {
            _duration = _player.NaturalDuration.HasTimeSpan ? _player.NaturalDuration.TimeSpan : TimeSpan.Zero;
            _position.Maximum = _duration.TotalMilliseconds > 0 ? 1000 : 1;
            _timer.Start();
            UpdateTimeLabel();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_isDragging) return;
            if (_duration == TimeSpan.Zero) return;

            var current = _player.Position;
            var percent = Math.Clamp(current.TotalMilliseconds / _duration.TotalMilliseconds, 0, 1);
            _position.Value = (int)Math.Round(percent * _position.Maximum);
            UpdateTimeLabel();
        }

        private void TogglePlayPause()
        {
            // If playing, pause; otherwise play
            _player.Dispatcher.Invoke(() =>
            {
                // MediaPlayer has no explicit state; inspect Position and NaturalDuration
                if (_duration != TimeSpan.Zero && _player.Position > TimeSpan.Zero && _player.Position < _duration - TimeSpan.FromMilliseconds(250))
                {
                    _player.Pause();
                    _btnPlayPause.Text = "▶";
                }
                else
                {
                    _player.Play();
                    _btnPlayPause.Text = "⏸";
                }
            });
        }

        private void StopPlayback()
        {
            _player.Stop();
            _btnPlayPause.Text = "▶";
            ResetPosition();
        }

        private void ResetPosition()
        {
            _player.Position = TimeSpan.Zero;
            _position.Value = 0;
            UpdateTimeLabel();
        }

        private void SeekFromSlider()
        {
            if (_duration == TimeSpan.Zero) return;
            var percent = _position.Value / (double)_position.Maximum;
            var target = TimeSpan.FromMilliseconds(_duration.TotalMilliseconds * percent);
            _player.Position = target;
            UpdateTimeLabel();
        }

        private void UpdateTimeLabelFromSlider()
        {
            if (_duration == TimeSpan.Zero) return;
            var percent = _position.Value / (double)_position.Maximum;
            var target = TimeSpan.FromMilliseconds(_duration.TotalMilliseconds * percent);
            _lblTime.Text = $"{FormatTime(target)} / {FormatTime(_duration)}";
        }

        private void UpdateTimeLabel()
        {
            var current = _player.Position;
            _lblTime.Text = $"{FormatTime(current)} / {FormatTime(_duration)}";
        }

        private static string FormatTime(TimeSpan ts)
        {
            if (ts.TotalHours >= 1)
                return ts.ToString(@"hh\:mm\:ss");
            return ts.ToString(@"mm\:ss");
        }

        private WinForms.Button CreateButton(string text, EventHandler onClick)
        {
            var btn = new WinForms.Button
            {
                Text = text,
                Width = 44,
                Height = 30,
                Margin = new WinForms.Padding(6, 0, 6, 0),
                FlatStyle = WinForms.FlatStyle.Flat,
                BackColor = System.Drawing.Color.FromArgb(250, 252, 255),
                ForeColor = System.Drawing.Color.FromArgb(45, 55, 72),
                Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += onClick;
            return btn;
        }

        private void Cleanup()
        {
            try { _timer.Stop(); } catch { }
            try { _player.Stop(); } catch { }
            try { _player.Close(); } catch { }

            if (!string.IsNullOrWhiteSpace(_ownedTempFile) && System.IO.File.Exists(_ownedTempFile))
            {
                try { System.IO.File.Delete(_ownedTempFile); } catch { }
            }
        }
    }
}
