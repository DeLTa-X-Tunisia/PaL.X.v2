using System;
using System.Drawing;
using System.Windows.Forms;

namespace PaL.X.Client
{
    internal enum ChatSaveFormat
    {
        Docs,
        Pdf,
        Txt
    }

    internal sealed class SaveChatOptionsDialog : Form
    {
        private readonly PictureBox _docs;
        private readonly PictureBox _pdf;
        private readonly PictureBox _txt;

        public ChatSaveFormat? SelectedFormat { get; private set; }

        public SaveChatOptionsDialog(Image? docsIcon, Image? pdfIcon, Image? txtIcon)
        {
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.WhiteSmoke;
            Padding = new Padding(12);
            AutoScaleMode = AutoScaleMode.Font;

            var iconSize = new Size(42, 42);

            _docs = CreateIconBox(docsIcon, iconSize);
            _pdf = CreateIconBox(pdfIcon, iconSize);
            _txt = CreateIconBox(txtIcon, iconSize);

            _docs.Click += (_, __) => Select(ChatSaveFormat.Docs);
            _pdf.Click += (_, __) => Select(ChatSaveFormat.Pdf);
            _txt.Click += (_, __) => Select(ChatSaveFormat.Txt);

            var layout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            layout.Controls.Add(_docs);
            layout.Controls.Add(_pdf);
            layout.Controls.Add(_txt);

            Controls.Add(layout);

            var tt = new ToolTip();
            tt.SetToolTip(_docs, "Enregistrement Docs");
            tt.SetToolTip(_pdf, "Enregistrement PDF");
            tt.SetToolTip(_txt, "Enregistrement TXT");

            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                Close();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void Select(ChatSaveFormat format)
        {
            SelectedFormat = format;
            DialogResult = DialogResult.OK;
            Close();
        }

        private static PictureBox CreateIconBox(Image? image, Size size)
        {
            var box = new PictureBox
            {
                Size = size,
                SizeMode = PictureBoxSizeMode.Zoom,
                Cursor = Cursors.Hand,
                Image = image,
                Margin = new Padding(6),
                BackColor = Color.Transparent
            };

            return box;
        }
    }
}
