using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PaL.X.Client
{
    public class PalMessageBox : Form
    {
        private Label lblMessage = null!;
        private Label lblHighlight = null!;
        private PictureBox picIcon = null!;
        private FlowLayoutPanel pnlButtons = null!;
        private Panel pnlHeader = null!;
        private Label lblTitle = null!;

        public PalMessageBox()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Size = new Size(450, 220);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Color.White;

            var pnlBorder = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(200, 200, 200),
                Padding = new Padding(1)
            };
            Controls.Add(pnlBorder);

            var container = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            pnlBorder.Controls.Add(container);

            pnlHeader = new Panel
            {
                Height = 40,
                Dock = DockStyle.Top,
                BackColor = Color.WhiteSmoke,
                Padding = new Padding(15, 0, 0, 0)
            };
            container.Controls.Add(pnlHeader);

            lblTitle = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(64, 64, 64),
                Location = new Point(15, 10)
            };
            pnlHeader.Controls.Add(lblTitle);

            pnlButtons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Bottom,
                Height = 60,
                Padding = new Padding(0, 15, 20, 10),
                BackColor = Color.WhiteSmoke
            };
            container.Controls.Add(pnlButtons);

            var pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };
            container.Controls.Add(pnlContent);
            pnlContent.BringToFront();

            picIcon = new PictureBox
            {
                Size = new Size(48, 48),
                Location = new Point(20, 20),
                SizeMode = PictureBoxSizeMode.StretchImage
            };
            pnlContent.Controls.Add(picIcon);

            lblHighlight = new Label
            {
                Location = new Point(80, 20),
                Size = new Size(330, 24),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.Black,
                Visible = false
            };
            pnlContent.Controls.Add(lblHighlight);

            lblMessage = new Label
            {
                Location = new Point(80, 20),
                Size = new Size(330, 80),
                TextAlign = ContentAlignment.TopLeft,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(50, 50, 50),
                AutoSize = false
            };
            pnlContent.Controls.Add(lblMessage);
        }

        public static DialogResult Show(string text)
        {
            return Show(text, "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static DialogResult Show(string text, string caption)
        {
            return Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons)
        {
            return Show(text, caption, buttons, MessageBoxIcon.None);
        }

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            using (var msgBox = new PalMessageBox())
            {
                msgBox.lblTitle.Text = caption;
                msgBox.ApplyMessage(text);
                msgBox.SetupIcon(icon);
                msgBox.SetupButtons(buttons);
                msgBox.TopMost = true;
                msgBox.BringToFront();
                return msgBox.ShowDialog();
            }
        }

        private void ApplyMessage(string text)
        {
            const string startTag = "<b>";
            const string endTag = "</b>";

            if (!string.IsNullOrEmpty(text) && text.Contains(startTag) && text.Contains(endTag))
            {
                int start = text.IndexOf(startTag, StringComparison.Ordinal);
                int end = text.IndexOf(endTag, start + startTag.Length, StringComparison.Ordinal);
                if (start >= 0 && end > start)
                {
                    string bold = text.Substring(start + startTag.Length, end - (start + startTag.Length)).Trim();
                    lblHighlight.Text = bold;
                    lblHighlight.Visible = true;

                    string before = text.Substring(0, start);
                    string after = text.Substring(end + endTag.Length);
                    string remaining = (before + after).TrimStart();

                    lblMessage.Text = remaining;
                    lblMessage.Top = lblHighlight.Bottom + 6;
                    return;
                }
            }

            lblHighlight.Visible = false;
            lblMessage.Top = 20;
            lblMessage.Text = text ?? string.Empty;
        }

        private void SetupIcon(MessageBoxIcon icon)
        {
            pnlHeader.BackColor = Color.WhiteSmoke;
            pnlButtons.BackColor = Color.WhiteSmoke;
            lblMessage.ForeColor = Color.FromArgb(50, 50, 50);
            lblTitle.ForeColor = Color.FromArgb(64, 64, 64);

            switch (icon)
            {
                case MessageBoxIcon.Error:
                    picIcon.Image = SystemIcons.Error.ToBitmap();
                    lblTitle.ForeColor = Color.FromArgb(185, 28, 28);
                    lblMessage.ForeColor = Color.FromArgb(120, 15, 15);
                    pnlHeader.BackColor = Color.FromArgb(255, 235, 238);
                    pnlButtons.BackColor = Color.FromArgb(252, 228, 236);
                    break;
                case MessageBoxIcon.Warning:
                    picIcon.Image = SystemIcons.Warning.ToBitmap();
                    lblTitle.ForeColor = Color.FromArgb(255, 193, 7);
                    break;
                case MessageBoxIcon.Information:
                    picIcon.Image = SystemIcons.Information.ToBitmap();
                    lblTitle.ForeColor = Color.FromArgb(0, 122, 204);
                    break;
                case MessageBoxIcon.Question:
                    picIcon.Image = SystemIcons.Question.ToBitmap();
                    lblTitle.ForeColor = Color.FromArgb(0, 122, 204);
                    break;
                default:
                    picIcon.Visible = false;
                    lblMessage.Left = 20;
                    lblMessage.Width += 60;
                    break;
            }
        }

        private void SetupButtons(MessageBoxButtons buttons)
        {
            switch (buttons)
            {
                case MessageBoxButtons.OK:
                    AddButton("OK", DialogResult.OK, true);
                    break;
                case MessageBoxButtons.OKCancel:
                    AddButton("Annuler", DialogResult.Cancel, false);
                    AddButton("OK", DialogResult.OK, true);
                    break;
                case MessageBoxButtons.YesNo:
                    AddButton("Non", DialogResult.No, false);
                    AddButton("Oui", DialogResult.Yes, true);
                    break;
            }

            CenterButtons();
        }

        private void AddButton(string text, DialogResult result, bool isPrimary)
        {
            var btn = new Button
            {
                Text = text,
                DialogResult = result,
                Size = new Size(90, 32),
                Margin = new Padding(10, 0, 0, 0),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Regular)
            };
            btn.FlatAppearance.BorderSize = 0;

            if (isPrimary)
            {
                btn.BackColor = Color.FromArgb(0, 122, 204);
                btn.ForeColor = Color.White;
            }
            else
            {
                btn.BackColor = Color.FromArgb(224, 224, 224);
                btn.ForeColor = Color.Black;
            }

            pnlButtons.Controls.Add(btn);
        }

        private void CenterButtons()
        {
            if (pnlButtons == null || pnlButtons.Controls.Count == 0)
            {
                return;
            }

            int totalWidth = pnlButtons.Controls.Cast<Control>()
                .Sum(c => c.Width + c.Margin.Horizontal);

            int availableWidth = pnlButtons.ClientSize.Width;
            int leftPadding = Math.Max(0, (availableWidth - totalWidth) / 2);

            pnlButtons.Padding = new Padding(leftPadding, pnlButtons.Padding.Top, 0, pnlButtons.Padding.Bottom);
        }
    }
}
