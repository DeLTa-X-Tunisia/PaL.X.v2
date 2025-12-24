using System;
using System.Drawing;
using System.Windows.Forms;

namespace PaL.X.Client
{
    internal enum ScreenshotPreviewAction
    {
        None,
        Send,
        Edit,
        Cancel
    }

    internal sealed class ScreenshotPreviewForm : Form
    {
        private readonly Image _image;
        public ScreenshotPreviewAction ActionResult { get; private set; } = ScreenshotPreviewAction.None;
        public Image ResultImage => _image;

        public ScreenshotPreviewForm(Image image, Image? sendIcon, Image? editIcon, Image? cancelIcon)
        {
            _image = (Image)image.Clone();

            Text = "Aperçu de la capture";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MinimizeBox = false;
            MaximizeBox = false;
            Size = new Size(Math.Min(1100, _image.Width + 80), Math.Min(800, _image.Height + 160));

            var pic = new PictureBox
            {
                Dock = DockStyle.Fill,
                Image = _image,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };

            var btnSend = new Button
            {
                Text = "Envoyer",
                Image = sendIcon,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                AutoSize = true,
                Padding = new Padding(8, 4, 8, 4)
            };
            btnSend.Click += (_, __) => { ActionResult = ScreenshotPreviewAction.Send; DialogResult = DialogResult.OK; Close(); };

            var btnEdit = new Button
            {
                Text = "Modifier",
                Image = editIcon,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                AutoSize = true,
                Padding = new Padding(8, 4, 8, 4)
            };
            btnEdit.Click += (_, __) => { ActionResult = ScreenshotPreviewAction.Edit; DialogResult = DialogResult.Retry; Close(); };

            var btnCancel = new Button
            {
                Text = "Annuler",
                Image = cancelIcon,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                AutoSize = true,
                Padding = new Padding(8, 4, 8, 4)
            };
            btnCancel.Click += (_, __) => { ActionResult = ScreenshotPreviewAction.Cancel; DialogResult = DialogResult.Cancel; Close(); };

            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Padding = new Padding(12),
                BackColor = Color.WhiteSmoke
            };
            buttonsPanel.Controls.AddRange(new Control[] { btnCancel, btnEdit, btnSend });

            Controls.Add(pic);
            Controls.Add(buttonsPanel);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _image.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
