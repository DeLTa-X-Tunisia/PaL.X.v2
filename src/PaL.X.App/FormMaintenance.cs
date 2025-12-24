using System;
using System.Drawing;
using System.Windows.Forms;

namespace PaL.X.App
{
    public partial class FormMaintenance : Form
    {
        public FormMaintenance()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(450, 320);
            this.BackColor = Color.White;
            
            // Draw simple border
            this.Paint += (s, e) => {
                ControlPaint.DrawBorder(e.Graphics, this.ClientRectangle, Color.LightGray, ButtonBorderStyle.Solid);
            };
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Main Layout Container
            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 6;
            
            // Vertical centering strategy:
            // Row 0: Spacer (50%)
            // Row 1: Icon (AutoSize)
            // Row 2: Title (AutoSize)
            // Row 3: Message (AutoSize)
            // Row 4: Button (AutoSize)
            // Row 5: Spacer (50%)
            
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            this.Controls.Add(layout);

            // 1. Icon
            Label lblIcon = new Label();
            lblIcon.Text = "🛑"; 
            lblIcon.Font = new Font("Segoe UI", 48, FontStyle.Regular);
            lblIcon.ForeColor = Color.FromArgb(220, 53, 69); // Red
            lblIcon.AutoSize = true;
            lblIcon.Anchor = AnchorStyles.None;
            lblIcon.TextAlign = ContentAlignment.MiddleCenter;
            lblIcon.Margin = new Padding(0, 0, 0, 5); // Small space below icon

            // 2. Title
            Label lblTitle = new Label();
            lblTitle.Text = "Système en Maintenance";
            lblTitle.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(50, 50, 50);
            lblTitle.AutoSize = true;
            lblTitle.Anchor = AnchorStyles.None;
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            lblTitle.Margin = new Padding(0, 0, 0, 15); // Space below title

            // 3. Message
            Label lblMessage = new Label();
            lblMessage.Text = "Le serveur est actuellement arrêté.\r\nVeuillez réessayer plus tard.";
            lblMessage.Font = new Font("Segoe UI", 11, FontStyle.Regular);
            lblMessage.ForeColor = Color.Gray;
            lblMessage.AutoSize = true;
            lblMessage.MaximumSize = new Size(380, 0); // Ensure wrapping if needed
            lblMessage.Anchor = AnchorStyles.None;
            lblMessage.TextAlign = ContentAlignment.MiddleCenter;
            lblMessage.Margin = new Padding(0, 0, 0, 30); // Space before button

            // 4. Button
            Button btnClose = new Button();
            btnClose.Text = "Fermer";
            btnClose.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.BackColor = Color.FromArgb(240, 240, 240);
            btnClose.ForeColor = Color.Black;
            btnClose.Size = new Size(130, 40);
            btnClose.Anchor = AnchorStyles.None;
            btnClose.Cursor = Cursors.Hand;
            btnClose.Click += (s, e) => Application.Exit();
            
            // Add controls to layout
            layout.Controls.Add(lblIcon, 0, 1);
            layout.Controls.Add(lblTitle, 0, 2);
            layout.Controls.Add(lblMessage, 0, 3);
            layout.Controls.Add(btnClose, 0, 4);

            this.ResumeLayout(false);
        }
    }
}
