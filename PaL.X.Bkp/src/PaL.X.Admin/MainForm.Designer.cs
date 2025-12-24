namespace PaL.X.Admin
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblWelcome;
        private System.Windows.Forms.Label lblUserInfo;
        private System.Windows.Forms.Button btnLogout;
        private System.Windows.Forms.GroupBox grpServiceControl;
        private System.Windows.Forms.Button btnStartService;
        private System.Windows.Forms.Button btnStopService;
        private System.Windows.Forms.Label lblServiceStatus;
        private System.Windows.Forms.Label lblConnectedClients;
        private System.Windows.Forms.ListView lstConnectedClients;
        private System.Windows.Forms.Button btnDisconnectAll;
        private System.Windows.Forms.Label lblAdminTitle;

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
            this.lblWelcome = new System.Windows.Forms.Label();
            this.lblUserInfo = new System.Windows.Forms.Label();
            this.btnLogout = new System.Windows.Forms.Button();
            this.grpServiceControl = new System.Windows.Forms.GroupBox();
            this.lblServiceStatus = new System.Windows.Forms.Label();
            this.btnStopService = new System.Windows.Forms.Button();
            this.btnStartService = new System.Windows.Forms.Button();
            this.lblConnectedClients = new System.Windows.Forms.Label();
            this.lstConnectedClients = new System.Windows.Forms.ListView();
            this.btnDisconnectAll = new System.Windows.Forms.Button();
            this.lblAdminTitle = new System.Windows.Forms.Label();
            this.grpServiceControl.SuspendLayout();
            this.SuspendLayout();
            
            // lblWelcome
            this.lblWelcome.AutoSize = true;
            this.lblWelcome.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblWelcome.Location = new System.Drawing.Point(20, 50);
            this.lblWelcome.Name = "lblWelcome";
            this.lblWelcome.Size = new System.Drawing.Size(189, 25);
            this.lblWelcome.TabIndex = 0;
            this.lblWelcome.Text = "Panel Administrateur";
            
            // lblUserInfo
            this.lblUserInfo.AutoSize = true;
            this.lblUserInfo.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblUserInfo.Location = new System.Drawing.Point(20, 85);
            this.lblUserInfo.Name = "lblUserInfo";
            this.lblUserInfo.Size = new System.Drawing.Size(44, 19);
            this.lblUserInfo.TabIndex = 1;
            this.lblUserInfo.Text = "label2";
            
            // btnLogout
            this.btnLogout.Location = new System.Drawing.Point(350, 20);
            this.btnLogout.Name = "btnLogout";
            this.btnLogout.Size = new System.Drawing.Size(100, 30);
            this.btnLogout.TabIndex = 2;
            this.btnLogout.Text = "Déconnexion";
            this.btnLogout.UseVisualStyleBackColor = true;
            this.btnLogout.Click += new System.EventHandler(this.btnLogout_Click);
            
            // grpServiceControl
            this.grpServiceControl.Controls.Add(this.lblServiceStatus);
            this.grpServiceControl.Controls.Add(this.btnStopService);
            this.grpServiceControl.Controls.Add(this.btnStartService);
            this.grpServiceControl.Location = new System.Drawing.Point(20, 120);
            this.grpServiceControl.Name = "grpServiceControl";
            this.grpServiceControl.Size = new System.Drawing.Size(200, 150);
            this.grpServiceControl.TabIndex = 3;
            this.grpServiceControl.TabStop = false;
            this.grpServiceControl.Text = "Contrôle du Service";
            
            // btnStartService
            this.btnStartService.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.btnStartService.Location = new System.Drawing.Point(20, 60);
            this.btnStartService.Name = "btnStartService";
            this.btnStartService.Size = new System.Drawing.Size(160, 30);
            this.btnStartService.TabIndex = 0;
            this.btnStartService.Text = "Démarrer le Service";
            this.btnStartService.UseVisualStyleBackColor = false;
            this.btnStartService.Click += new System.EventHandler(this.btnStartService_Click);
            
            // btnStopService
            this.btnStopService.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.btnStopService.Enabled = false;
            this.btnStopService.Location = new System.Drawing.Point(20, 100);
            this.btnStopService.Name = "btnStopService";
            this.btnStopService.Size = new System.Drawing.Size(160, 30);
            this.btnStopService.TabIndex = 1;
            this.btnStopService.Text = "Arrêter le Service";
            this.btnStopService.UseVisualStyleBackColor = false;
            this.btnStopService.Click += new System.EventHandler(this.btnStopService_Click);
            
            // lblServiceStatus
            this.lblServiceStatus.AutoSize = true;
            this.lblServiceStatus.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblServiceStatus.Location = new System.Drawing.Point(20, 30);
            this.lblServiceStatus.Name = "lblServiceStatus";
            this.lblServiceStatus.Size = new System.Drawing.Size(126, 15);
            this.lblServiceStatus.TabIndex = 2;
            this.lblServiceStatus.Text = "Statut: Non démarré";
            
            // lblConnectedClients
            this.lblConnectedClients.AutoSize = true;
            this.lblConnectedClients.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblConnectedClients.Location = new System.Drawing.Point(240, 120);
            this.lblConnectedClients.Name = "lblConnectedClients";
            this.lblConnectedClients.Size = new System.Drawing.Size(147, 19);
            this.lblConnectedClients.TabIndex = 4;
            this.lblConnectedClients.Text = "Clients connectés : 0";
            
            // lstConnectedClients
            this.lstConnectedClients = new System.Windows.Forms.ListView();
            this.lstConnectedClients.Location = new System.Drawing.Point(240, 150);
            this.lstConnectedClients.Name = "lstConnectedClients";
            this.lstConnectedClients.Size = new System.Drawing.Size(400, 150);
            this.lstConnectedClients.TabIndex = 5;
            this.lstConnectedClients.View = System.Windows.Forms.View.Details;
            this.lstConnectedClients.FullRowSelect = true;
            this.lstConnectedClients.GridLines = true;
            this.lstConnectedClients.Columns.Add("Utilisateur", 100);
            this.lstConnectedClients.Columns.Add("Email", 150);
            this.lstConnectedClients.Columns.Add("Role", 60);
            this.lstConnectedClients.Columns.Add("Heure", 80);
            this.lstConnectedClients.Columns.Add("Statut", 70);
            
            // btnDisconnectAll
            this.btnDisconnectAll.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(224)))), ((int)(((byte)(192)))));
            this.btnDisconnectAll.Location = new System.Drawing.Point(240, 310);
            this.btnDisconnectAll.Name = "btnDisconnectAll";
            this.btnDisconnectAll.Size = new System.Drawing.Size(400, 30);
            this.btnDisconnectAll.TabIndex = 6;
            this.btnDisconnectAll.Text = "Déconnecter tous les clients";
            this.btnDisconnectAll.UseVisualStyleBackColor = false;
            this.btnDisconnectAll.Click += new System.EventHandler(this.btnDisconnectAll_Click);
            
            // lblAdminTitle
            this.lblAdminTitle.AutoSize = true;
            this.lblAdminTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblAdminTitle.ForeColor = System.Drawing.Color.Red;
            this.lblAdminTitle.Location = new System.Drawing.Point(20, 15);
            this.lblAdminTitle.Name = "lblAdminTitle";
            this.lblAdminTitle.Size = new System.Drawing.Size(203, 30);
            this.lblAdminTitle.TabIndex = 7;
            
            // MainForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(660, 360);
            this.Controls.Add(this.lblAdminTitle);
            this.Controls.Add(this.btnDisconnectAll);
            this.Controls.Add(this.lstConnectedClients);
            this.Controls.Add(this.lblConnectedClients);
            this.Controls.Add(this.grpServiceControl);
            this.Controls.Add(this.btnLogout);
            this.Controls.Add(this.lblUserInfo);
            this.Controls.Add(this.lblWelcome);
            this.Name = "MainForm";
            this.Text = "PaL.X - Administration";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.grpServiceControl.ResumeLayout(false);
            this.grpServiceControl.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}