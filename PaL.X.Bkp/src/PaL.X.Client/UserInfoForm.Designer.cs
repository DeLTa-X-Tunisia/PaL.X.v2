namespace PaL.X.Client
{
    partial class UserInfoForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblProgress;
        private System.Windows.Forms.Label lblChecklist;
        private System.Windows.Forms.TextBox txtFirstName;
        private System.Windows.Forms.TextBox txtLastName;
        private System.Windows.Forms.TextBox txtDisplayedName;
        private System.Windows.Forms.DateTimePicker dtpDateOfBirth;
        private System.Windows.Forms.ComboBox cmbGender;
        private System.Windows.Forms.TextBox txtCountry;
        private System.Windows.Forms.PictureBox pbProfilePicture;
        private System.Windows.Forms.Button btnBrowsePhoto;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Label lblFirstName;
        private System.Windows.Forms.Label lblLastName;
        private System.Windows.Forms.Label lblDisplayedName;
        private System.Windows.Forms.Label lblDateOfBirth;
        private System.Windows.Forms.Label lblGender;
        private System.Windows.Forms.Label lblCountry;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblProgress = new System.Windows.Forms.Label();
            this.lblChecklist = new System.Windows.Forms.Label();
            this.txtFirstName = new System.Windows.Forms.TextBox();
            this.txtLastName = new System.Windows.Forms.TextBox();
            this.txtDisplayedName = new System.Windows.Forms.TextBox();
            this.dtpDateOfBirth = new System.Windows.Forms.DateTimePicker();
            this.cmbGender = new System.Windows.Forms.ComboBox();
            this.txtCountry = new System.Windows.Forms.TextBox();
            this.pbProfilePicture = new System.Windows.Forms.PictureBox();
            this.btnBrowsePhoto = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.lblFirstName = new System.Windows.Forms.Label();
            this.lblLastName = new System.Windows.Forms.Label();
            this.lblDisplayedName = new System.Windows.Forms.Label();
            this.lblDateOfBirth = new System.Windows.Forms.Label();
            this.lblGender = new System.Windows.Forms.Label();
            this.lblCountry = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pbProfilePicture)).BeginInit();
            this.SuspendLayout();
            
            // lblTitle
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblTitle.Location = new System.Drawing.Point(20, 20);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(200, 25);
            this.lblTitle.Text = "Complétez votre profil";

            // lblProgress
            this.lblProgress.AutoSize = true;
            this.lblProgress.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblProgress.Location = new System.Drawing.Point(20, 55);
            this.lblProgress.Name = "lblProgress";
            this.lblProgress.Size = new System.Drawing.Size(100, 19);
            this.lblProgress.Text = "Progression: 0%";

            // lblChecklist
            this.lblChecklist.AutoSize = true;
            this.lblChecklist.ForeColor = System.Drawing.Color.Red;
            this.lblChecklist.Location = new System.Drawing.Point(350, 60);
            this.lblChecklist.Name = "lblChecklist";
            this.lblChecklist.Size = new System.Drawing.Size(150, 100);
            this.lblChecklist.Text = "Champs manquants:\n- Prénom\n- Nom\n- Pseudo\n- Date de naissance\n- Genre\n- Pays";

            // Controls Layout
            int y = 100;
            int labelX = 20;
            int controlX = 150;
            int gap = 40;

            // FirstName
            this.lblFirstName.Location = new System.Drawing.Point(labelX, y);
            this.lblFirstName.Text = "Prénom *";
            this.txtFirstName.Location = new System.Drawing.Point(controlX, y);
            this.txtFirstName.Size = new System.Drawing.Size(180, 23);
            this.txtFirstName.TextChanged += new System.EventHandler(this.Input_Changed);
            y += gap;

            // LastName
            this.lblLastName.Location = new System.Drawing.Point(labelX, y);
            this.lblLastName.Text = "Nom *";
            this.txtLastName.Location = new System.Drawing.Point(controlX, y);
            this.txtLastName.Size = new System.Drawing.Size(180, 23);
            this.txtLastName.TextChanged += new System.EventHandler(this.Input_Changed);
            y += gap;

            // DisplayedName
            this.lblDisplayedName.Location = new System.Drawing.Point(labelX, y);
            this.lblDisplayedName.Text = "Pseudo *";
            this.txtDisplayedName.Location = new System.Drawing.Point(controlX, y);
            this.txtDisplayedName.Size = new System.Drawing.Size(180, 23);
            this.txtDisplayedName.TextChanged += new System.EventHandler(this.Input_Changed);
            y += gap;

            // DateOfBirth
            this.lblDateOfBirth.Location = new System.Drawing.Point(labelX, y);
            this.lblDateOfBirth.Text = "Date de naissance *";
            this.dtpDateOfBirth.Location = new System.Drawing.Point(controlX, y);
            this.dtpDateOfBirth.Size = new System.Drawing.Size(180, 23);
            this.dtpDateOfBirth.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpDateOfBirth.ValueChanged += new System.EventHandler(this.Input_Changed);
            y += gap;

            // Gender
            this.lblGender.Location = new System.Drawing.Point(labelX, y);
            this.lblGender.Text = "Genre *";
            this.cmbGender.Location = new System.Drawing.Point(controlX, y);
            this.cmbGender.Size = new System.Drawing.Size(180, 23);
            this.cmbGender.Items.AddRange(new object[] { "Homme", "Femme", "Autre" });
            this.cmbGender.SelectedIndexChanged += new System.EventHandler(this.Input_Changed);
            y += gap;

            // Country
            this.lblCountry.Location = new System.Drawing.Point(labelX, y);
            this.lblCountry.Text = "Pays *";
            this.txtCountry.Location = new System.Drawing.Point(controlX, y);
            this.txtCountry.Size = new System.Drawing.Size(180, 23);
            this.txtCountry.TextChanged += new System.EventHandler(this.Input_Changed);
            y += gap;

            // Profile Picture
            this.pbProfilePicture.Location = new System.Drawing.Point(350, 100);
            this.pbProfilePicture.Size = new System.Drawing.Size(120, 120);
            this.pbProfilePicture.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbProfilePicture.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            
            this.btnBrowsePhoto.Location = new System.Drawing.Point(350, 230);
            this.btnBrowsePhoto.Size = new System.Drawing.Size(120, 30);
            this.btnBrowsePhoto.Text = "Choisir Photo";
            this.btnBrowsePhoto.Click += new System.EventHandler(this.btnBrowsePhoto_Click);

            // Save Button
            this.btnSave.Location = new System.Drawing.Point(controlX, y + 20);
            this.btnSave.Size = new System.Drawing.Size(180, 40);
            this.btnSave.Text = "Enregistrer et Continuer";
            this.btnSave.Enabled = false;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);

            // Form
            this.ClientSize = new System.Drawing.Size(550, 450);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblProgress);
            this.Controls.Add(this.lblChecklist);
            this.Controls.Add(this.lblFirstName);
            this.Controls.Add(this.txtFirstName);
            this.Controls.Add(this.lblLastName);
            this.Controls.Add(this.txtLastName);
            this.Controls.Add(this.lblDisplayedName);
            this.Controls.Add(this.txtDisplayedName);
            this.Controls.Add(this.lblDateOfBirth);
            this.Controls.Add(this.dtpDateOfBirth);
            this.Controls.Add(this.lblGender);
            this.Controls.Add(this.cmbGender);
            this.Controls.Add(this.lblCountry);
            this.Controls.Add(this.txtCountry);
            this.Controls.Add(this.pbProfilePicture);
            this.Controls.Add(this.btnBrowsePhoto);
            this.Controls.Add(this.btnSave);
            
            this.Name = "UserInfoForm";
            this.Text = "Complétion du Profil";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            ((System.ComponentModel.ISupportInitialize)(this.pbProfilePicture)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
