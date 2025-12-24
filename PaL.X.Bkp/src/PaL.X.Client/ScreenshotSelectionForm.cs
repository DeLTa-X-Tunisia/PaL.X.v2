using System;
using System.Drawing;
using System.Windows.Forms;

namespace PaL.X.Client
{
    internal sealed class ScreenshotSelectionForm : Form
    {
        public Rectangle SelectedRegion { get; private set; }

        private bool _isDragging;
        private Point _start;
        private Rectangle _currentRect;

        public ScreenshotSelectionForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            DoubleBuffered = true;
            Cursor = Cursors.Cross;
            Bounds = SystemInformation.VirtualScreen;

            BackColor = Color.Black;
            Opacity = 0.20; // slight dim overlay

            KeyPreview = true;
            KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            };

            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
        }

        private void OnMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            _isDragging = true;
            _start = PointToScreen(e.Location);
            _currentRect = new Rectangle(_start, Size.Empty);
            Invalidate();
        }

        private void OnMouseMove(object? sender, MouseEventArgs e)
        {
            if (!_isDragging) return;
            var end = PointToScreen(e.Location);
            _currentRect = NormalizeRect(_start, end);
            Invalidate();
        }

        private void OnMouseUp(object? sender, MouseEventArgs e)
        {
            if (!_isDragging || e.Button != MouseButtons.Left) return;
            _isDragging = false;
            var end = PointToScreen(e.Location);
            _currentRect = NormalizeRect(_start, end);

            if (_currentRect.Width < 5 || _currentRect.Height < 5)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            SelectedRegion = _currentRect;
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            using var selectionPen = new Pen(Color.DeepSkyBlue, 2);
            selectionPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
            using var fillBrush = new SolidBrush(Color.FromArgb(40, Color.DeepSkyBlue));

            if (_currentRect.Width > 0 && _currentRect.Height > 0)
            {
                e.Graphics.FillRectangle(fillBrush, _currentRect);
                e.Graphics.DrawRectangle(selectionPen, _currentRect);
            }
        }

        private static Rectangle NormalizeRect(Point start, Point end)
        {
            int x1 = Math.Min(start.X, end.X);
            int y1 = Math.Min(start.Y, end.Y);
            int x2 = Math.Max(start.X, end.X);
            int y2 = Math.Max(start.Y, end.Y);
            return new Rectangle(x1, y1, x2 - x1, y2 - y1);
        }
    }
}
