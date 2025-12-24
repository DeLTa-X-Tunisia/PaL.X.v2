using PaL.X.Client.Services;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PaL.X.Client
{
    public class FormFriendRequest : Form
    {
        public string ResponseType { get; private set; } = "Refuse";
        public string Reason { get; private set; } = "Inconnu";

        private string _requesterName;
        private Label lblMessage = null!;
        private TextBox txtReason = null!;
        
        private Button btnAccept = null!;
        private Button btnAcceptAdd = null!;
        private Button btnRefuse = null!;

        // Constructeur pour le Designer
        public FormFriendRequest()
        {
            _requesterName = "Utilisateur";
            InitializeComponent();
        }

        public FormFriendRequest(string requesterName)
        {
            _requesterName = requesterName;
            InitializeComponent();
            lblMessage.Text = $"{_requesterName} souhaite vous ajouter dans sa liste d'amis";
            SetupIcons();
        }

        private void InitializeComponent()
        {
            this.Text = "Demande d'ami";
            this.Size = new Size(400, 350);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            lblMessage = new Label();
            lblMessage.Text = "Utilisateur souhaite vous ajouter dans sa liste d'amis";
            lblMessage.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblMessage.TextAlign = ContentAlignment.MiddleCenter;
            lblMessage.Dock = DockStyle.Top;
            lblMessage.Height = 60;
            this.Controls.Add(lblMessage);

            int btnY = 70;
            int btnHeight = 50;
            int btnWidth = 360;
            int btnX = 20;

            // Button Accept
            btnAccept = new Button();
            btnAccept.Text = "Accepter";
            btnAccept.Location = new Point(btnX, btnY);
            btnAccept.Size = new Size(btnWidth, btnHeight);
            btnAccept.TextAlign = ContentAlignment.MiddleLeft;
            btnAccept.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnAccept.Click += (s, e) => { ResponseType = "Accept"; this.DialogResult = DialogResult.OK; this.Close(); };
            this.Controls.Add(btnAccept);

            btnY += 60;

            // Button Accept & Add
            btnAcceptAdd = new Button();
            btnAcceptAdd.Text = "Accepter et ajouter dans ma liste d'amis";
            btnAcceptAdd.Location = new Point(btnX, btnY);
            btnAcceptAdd.Size = new Size(btnWidth, btnHeight);
            btnAcceptAdd.TextAlign = ContentAlignment.MiddleLeft;
            btnAcceptAdd.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnAcceptAdd.Click += (s, e) => { ResponseType = "AcceptAdd"; this.DialogResult = DialogResult.OK; this.Close(); };
            this.Controls.Add(btnAcceptAdd);

            btnY += 60;

            // Button Refuse
            btnRefuse = new Button();
            btnRefuse.Text = "Refuser";
            btnRefuse.Location = new Point(btnX, btnY);
            btnRefuse.Size = new Size(btnWidth, btnHeight);
            btnRefuse.TextAlign = ContentAlignment.MiddleLeft;
            btnRefuse.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnRefuse.Click += (s, e) => { ResponseType = "Refuse"; Reason = string.IsNullOrWhiteSpace(txtReason.Text) ? "Inconnu" : txtReason.Text; this.DialogResult = DialogResult.OK; this.Close(); };
            this.Controls.Add(btnRefuse);

            btnY += 60;

            // Reason TextBox
            var lblReason = new Label();
            lblReason.Text = "Raison (si refus) :";
            lblReason.Location = new Point(btnX, btnY);
            lblReason.AutoSize = true;
            this.Controls.Add(lblReason);

            txtReason = new TextBox();
            txtReason.Location = new Point(btnX, btnY + 20);
            txtReason.Size = new Size(btnWidth, 25);
            this.Controls.Add(txtReason);
        }

        private void SetupIcons()
        {
            ApplyButtonIcon(btnAccept, "icon/message/accepte.ico");
            ApplyButtonIcon(btnAcceptAdd, "icon/message/accepte_ajoute.ico");
            ApplyButtonIcon(btnRefuse, "icon/message/refuse.ico");
        }

        private static void ApplyButtonIcon(Button button, string resourceKey)
        {
            var image = ResourceImageStore.LoadImage(resourceKey, new Size(32, 32));
            if (image == null)
            {
                return;
            }

            button.Image?.Dispose();
            button.Image = image;
            button.Padding = new Padding(10, 0, 0, 0);
        }
    }
}
