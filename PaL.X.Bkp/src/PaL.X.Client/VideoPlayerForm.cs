using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using WinForms = System.Windows.Forms;
using WpfControls = System.Windows.Controls;

namespace PaL.X.Client
{
    public class VideoPlayerForm : WinForms.Form
    {
        private readonly WpfControls.MediaElement _media;
    private readonly ElementHost _host;
    private readonly WinForms.Button _btnToggleSize;
    private readonly WinForms.Button _btnPlay;
    private readonly WinForms.Button _btnPause;
    private readonly WinForms.Button _btnStop;
    private readonly WinForms.Panel _controlsPanel;
        private string? _tempFile;
        private bool _isLarge;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool StartInFullscreen { get; set; }

        public VideoPlayerForm(string title)
        {
            Text = title;
            StartPosition = WinForms.FormStartPosition.CenterParent;
            FormBorderStyle = WinForms.FormBorderStyle.SizableToolWindow;
            MinimumSize = new System.Drawing.Size(420, 320);
            Size = new System.Drawing.Size(520, 380);

            _media = new WpfControls.MediaElement
            {
                LoadedBehavior = System.Windows.Controls.MediaState.Manual,
                UnloadedBehavior = System.Windows.Controls.MediaState.Manual,
                Stretch = Stretch.Uniform,
                Volume = 1.0,
            };

            _host = new ElementHost
            {
                Dock = WinForms.DockStyle.Fill,
                Child = _media
            };

            _btnPlay = CreateControlButton("▶", (_, __) => _media.Play());
            _btnPause = CreateControlButton("⏸", (_, __) => _media.Pause());
            _btnStop = CreateControlButton("⏹", (_, __) => _media.Stop());

            _btnToggleSize = new WinForms.Button
            {
                Text = "Agrandir",
                Dock = WinForms.DockStyle.Right,
                Width = 90,
                FlatStyle = WinForms.FlatStyle.Flat,
                BackColor = System.Drawing.Color.FromArgb(245, 247, 250)
            };
            _btnToggleSize.FlatAppearance.BorderSize = 0;
            _btnToggleSize.Click += (_, __) => ToggleSize();

            _controlsPanel = new WinForms.Panel
            {
                Dock = WinForms.DockStyle.Bottom,
                Height = 46,
                Padding = new WinForms.Padding(8, 6, 8, 6),
                BackColor = System.Drawing.Color.FromArgb(235, 239, 245)
            };

            var flow = new WinForms.FlowLayoutPanel
            {
                Dock = WinForms.DockStyle.Fill,
                FlowDirection = WinForms.FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = false,
                Padding = new WinForms.Padding(0),
                Margin = new WinForms.Padding(0)
            };
            flow.Controls.Add(_btnPlay);
            flow.Controls.Add(_btnPause);
            flow.Controls.Add(_btnStop);
            flow.Controls.Add(_btnToggleSize);
            _controlsPanel.Controls.Add(flow);

            Controls.Add(_host);
            Controls.Add(_controlsPanel);

            _host.DoubleClick += (_, __) => ToggleSize();
            _host.Child.MouseLeftButtonDown += (_, e) =>
            {
                if (e.ClickCount == 2)
                {
                    ToggleFullscreen();
                }
            };
            FormClosing += (_, __) => Cleanup();

            Load += (_, __) =>
            {
                if (StartInFullscreen)
                {
                    ToggleFullscreen();
                }
            };
        }

        public async Task LoadFromBytesAsync(byte[] data, string suggestedExtension = ".mp4")
        {
            _tempFile = Path.Combine(Path.GetTempPath(), $"palx_vid_{Guid.NewGuid():N}{suggestedExtension}");
            await File.WriteAllBytesAsync(_tempFile, data);
            LoadSource(_tempFile);
        }

        public async Task LoadFromUrlAsync(string url)
        {
            var ext = Path.GetExtension(url);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".mp4";

            using var client = new System.Net.Http.HttpClient();
            var bytes = await client.GetByteArrayAsync(url);
            await LoadFromBytesAsync(bytes, ext);
        }

        private void LoadSource(string path)
        {
            _media.Stop();
            _media.Source = new Uri(path);
            _media.Position = TimeSpan.Zero;
            _media.Play();
        }

        private WinForms.Button CreateControlButton(string text, EventHandler onClick)
        {
            var btn = new WinForms.Button
            {
                Text = text,
                Width = 42,
                Height = 28,
                Margin = new WinForms.Padding(4, 0, 4, 0),
                FlatStyle = WinForms.FlatStyle.Flat,
                BackColor = System.Drawing.Color.FromArgb(250, 252, 255),
                ForeColor = System.Drawing.Color.FromArgb(45, 55, 72),
                Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += onClick;
            return btn;
        }

        private void ToggleFullscreen()
        {
            bool entering = FormBorderStyle != WinForms.FormBorderStyle.None;
            if (entering)
            {
                FormBorderStyle = WinForms.FormBorderStyle.None;
                WindowState = WinForms.FormWindowState.Maximized;
            }
            else
            {
                FormBorderStyle = WinForms.FormBorderStyle.SizableToolWindow;
                WindowState = WinForms.FormWindowState.Normal;
                Size = new System.Drawing.Size(520, 380);
            }
        }

        private void ToggleSize()
        {
            _isLarge = !_isLarge;
            if (_isLarge)
            {
                Size = new System.Drawing.Size(900, 640);
                _btnToggleSize.Text = "Réduire";
            }
            else
            {
                Size = new System.Drawing.Size(520, 380);
                _btnToggleSize.Text = "Agrandir";
            }
        }

        private void Cleanup()
        {
            try { _media.Stop(); } catch { }

            try
            {
                if (!string.IsNullOrWhiteSpace(_tempFile) && File.Exists(_tempFile))
                {
                    File.Delete(_tempFile);
                }
            }
            catch { }
        }
    }
}
