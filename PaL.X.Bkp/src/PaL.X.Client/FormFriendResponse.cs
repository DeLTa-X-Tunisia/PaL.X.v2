using PaL.X.Client.Services;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PaL.X.Client
{
    public class FormFriendResponse : Form
    {
        private Label lblMessage = null!;
        private PictureBox picIcon = null!;

        // Constructeur pour le Designer
        public FormFriendResponse()
        {
            InitializeComponent();
        }

        public FormFriendResponse(string responderName, string responseType, string reason)
        {
            InitializeComponent();
            SetupContent(responderName, responseType, reason);
        }

        private void InitializeComponent()
        {
            this.Text = "Réponse demande d'ami";
            this.Size = new Size(400, 200);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            lblMessage = new Label();
            lblMessage.Font = new Font("Segoe UI", 10);
            lblMessage.TextAlign = ContentAlignment.MiddleLeft;
            lblMessage.Location = new Point(80, 20);
            lblMessage.Size = new Size(300, 100);
            this.Controls.Add(lblMessage);

            picIcon = new PictureBox();
            picIcon.Location = new Point(20, 30);
            picIcon.Size = new Size(48, 48);
            picIcon.SizeMode = PictureBoxSizeMode.StretchImage;
            this.Controls.Add(picIcon);

            var btnOk = new Button();
            btnOk.Text = "OK";
            btnOk.Location = new Point(150, 120);
            btnOk.DialogResult = DialogResult.OK;
            this.Controls.Add(btnOk);
            this.AcceptButton = btnOk;
        }

        private void SetupContent(string responderName, string responseType, string reason)
        {
            string message = "";
            string iconResource = string.Empty;

            switch (responseType)
            {
                case "Accept":
                    message = $"{responderName} a accepté que vous l'ajoutiez dans votre liste d'amis.";
                    iconResource = "icon/message/accepte.ico";
                    break;
                case "AcceptAdd":
                    message = $"{responderName} a accepté votre demande et vous a ajouté dans sa liste d'amis.";
                    iconResource = "icon/message/accepte_ajoute.ico";
                    break;
                case "Refuse":
                    message = $"{responderName} a refusé votre demande d'ajout.\nRaison : {reason}";
                    iconResource = "icon/message/refuse.ico";
                    break;
                default:
                    message = "Réponse inconnue.";
                    break;
            }

            lblMessage.Text = message;

            if (!string.IsNullOrWhiteSpace(iconResource))
            {
                var image = ResourceImageStore.LoadImage(iconResource, new Size(48, 48));
                if (image != null)
                {
                    picIcon.Image?.Dispose();
                    picIcon.Image = image;
                }
            }
        }
    }
}
