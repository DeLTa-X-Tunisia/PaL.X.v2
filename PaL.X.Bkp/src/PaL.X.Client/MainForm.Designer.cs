namespace PaL.X.Client
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblWelcome;
        private System.Windows.Forms.Label lblUserInfo;
        private System.Windows.Forms.Button btnLogout;
        private System.Windows.Forms.Label lblConnectionStatus;
        private System.Windows.Forms.Timer timerConnectionCheck;
        private System.Windows.Forms.Button btnSearchFriend;
        private System.Windows.Forms.Button btnMyProfile;
    private System.Windows.Forms.Button btnBlockedUsers;
        private System.Windows.Forms.FlowLayoutPanel flpActions;
        private System.Windows.Forms.Label lblFriends;
        private System.Windows.Forms.ListView lstFriends;
        private System.Windows.Forms.ColumnHeader colFriendName;
        private System.Windows.Forms.ColumnHeader colFriendStatus;

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
            this.components = new System.ComponentModel.Container();
            this.lblWelcome = new System.Windows.Forms.Label();
            this.lblUserInfo = new System.Windows.Forms.Label();
            this.btnLogout = new System.Windows.Forms.Button();
            this.lblConnectionStatus = new System.Windows.Forms.Label();
            this.timerConnectionCheck = new System.Windows.Forms.Timer(this.components);
            this.btnSearchFriend = new System.Windows.Forms.Button();
            this.btnMyProfile = new System.Windows.Forms.Button();
            this.btnBlockedUsers = new System.Windows.Forms.Button();
            this.flpActions = new System.Windows.Forms.FlowLayoutPanel();
            this.lblFriends = new System.Windows.Forms.Label();
            this.lstFriends = new System.Windows.Forms.ListView();
            this.colFriendName = new System.Windows.Forms.ColumnHeader();
            this.colFriendStatus = new System.Windows.Forms.ColumnHeader();
            this.SuspendLayout();
            
            // lblWelcome
            this.lblWelcome.AutoSize = true;
            this.lblWelcome.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblWelcome.Location = new System.Drawing.Point(50, 50);
            this.lblWelcome.Name = "lblWelcome";
            this.lblWelcome.Size = new System.Drawing.Size(300, 32);
            this.lblWelcome.TabIndex = 0;
            this.lblWelcome.Text = "Bienvenue dans PaL.X !";
            
            // lblUserInfo
            this.lblUserInfo.AutoSize = true;
            this.lblUserInfo.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblUserInfo.Location = new System.Drawing.Point(50, 100);
            this.lblUserInfo.Name = "lblUserInfo";
            this.lblUserInfo.Size = new System.Drawing.Size(52, 21);
            this.lblUserInfo.TabIndex = 1;
            this.lblUserInfo.Text = "label2";
            
            // btnLogout
            this.btnLogout.Name = "btnLogout";
            this.btnLogout.Size = new System.Drawing.Size(100, 32);
            this.btnLogout.TabIndex = 2;
            this.btnLogout.Text = "Déconnexion";
            this.btnLogout.UseVisualStyleBackColor = true;
            this.btnLogout.Margin = new System.Windows.Forms.Padding(0);
            this.btnLogout.AutoSize = true;
            this.btnLogout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnLogout.MinimumSize = new System.Drawing.Size(0, 32);
            this.btnLogout.Click += new System.EventHandler(this.btnLogout_Click);
            
            // lblConnectionStatus
            this.lblConnectionStatus.AutoSize = true;
            this.lblConnectionStatus.Location = new System.Drawing.Point(50, 150);
            this.lblConnectionStatus.Name = "lblConnectionStatus";
            this.lblConnectionStatus.Size = new System.Drawing.Size(117, 15);
            this.lblConnectionStatus.TabIndex = 3;
            this.lblConnectionStatus.Text = "Statut de connexion:";
            this.lblConnectionStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            
            // timerConnectionCheck
            this.timerConnectionCheck.Interval = 5000; // 5 secondes
            this.timerConnectionCheck.Tick += new System.EventHandler(this.timerConnectionCheck_Tick);

            // btnSearchFriend
            this.btnSearchFriend.Name = "btnSearchFriend";
            this.btnSearchFriend.Size = new System.Drawing.Size(120, 32);
            this.btnSearchFriend.TabIndex = 4;
            this.btnSearchFriend.Text = "Chercher Ami";
            this.btnSearchFriend.UseVisualStyleBackColor = true;
            this.btnSearchFriend.AutoSize = true;
            this.btnSearchFriend.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnSearchFriend.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this.btnSearchFriend.MinimumSize = new System.Drawing.Size(0, 32);
            this.btnSearchFriend.Click += new System.EventHandler(this.BtnSearchFriend_Click);

            // btnMyProfile
            this.btnMyProfile.Name = "btnMyProfile";
            this.btnMyProfile.Size = new System.Drawing.Size(120, 32);
            this.btnMyProfile.TabIndex = 5;
            this.btnMyProfile.Text = "Mon Profil";
            this.btnMyProfile.UseVisualStyleBackColor = true;
            this.btnMyProfile.AutoSize = true;
            this.btnMyProfile.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnMyProfile.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this.btnMyProfile.MinimumSize = new System.Drawing.Size(0, 32);
            this.btnMyProfile.Click += new System.EventHandler(this.BtnMyProfile_Click);

            // btnBlockedUsers
            this.btnBlockedUsers.Name = "btnBlockedUsers";
            this.btnBlockedUsers.Size = new System.Drawing.Size(120, 32);
            this.btnBlockedUsers.TabIndex = 6;
            this.btnBlockedUsers.Text = "Bloqués";
            this.btnBlockedUsers.UseVisualStyleBackColor = true;
            this.btnBlockedUsers.AutoSize = true;
            this.btnBlockedUsers.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnBlockedUsers.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this.btnBlockedUsers.MinimumSize = new System.Drawing.Size(0, 32);
            this.btnBlockedUsers.Click += new System.EventHandler(this.BtnBlockedUsers_Click);

            // flpActions
            this.flpActions.AutoSize = false;
            this.flpActions.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flpActions.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpActions.Location = new System.Drawing.Point(50, 190);
            this.flpActions.Name = "flpActions";
            this.flpActions.Size = new System.Drawing.Size(420, 40);
            this.flpActions.TabIndex = 9;
            this.flpActions.WrapContents = false;
            this.flpActions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.flpActions.Margin = new System.Windows.Forms.Padding(0);
            this.flpActions.Padding = new System.Windows.Forms.Padding(0);
            this.flpActions.AutoScroll = true;
            this.flpActions.Controls.Add(this.btnSearchFriend);
            this.flpActions.Controls.Add(this.btnMyProfile);
            this.flpActions.Controls.Add(this.btnBlockedUsers);
            this.flpActions.Controls.Add(this.btnLogout);

            // lblFriends
            this.lblFriends.AutoSize = true;
            this.lblFriends.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblFriends.Location = new System.Drawing.Point(50, 240);
            this.lblFriends.Name = "lblFriends";
            this.lblFriends.Size = new System.Drawing.Size(90, 19);
            this.lblFriends.TabIndex = 7;
            this.lblFriends.Text = "Liste d'Amis";
            this.lblFriends.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));

            // lstFriends
            this.lstFriends.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colFriendName,
            this.colFriendStatus});
            this.lstFriends.FullRowSelect = true;
            this.lstFriends.Location = new System.Drawing.Point(50, 265);
            this.lstFriends.Name = "lstFriends";
            this.lstFriends.OwnerDraw = true;
            this.lstFriends.Size = new System.Drawing.Size(400, 320);
            this.lstFriends.TabIndex = 8;
            this.lstFriends.UseCompatibleStateImageBehavior = false;
            this.lstFriends.View = System.Windows.Forms.View.Details;
            this.lstFriends.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));

            // colFriendName
            this.colFriendName.Text = "Ami";
            this.colFriendName.Width = 180;

            // colFriendStatus
            this.colFriendStatus.Text = "Statut";
            this.colFriendStatus.Width = 100;
            
            // MainForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(500, 650);
            this.Controls.Add(this.lstFriends);
            this.Controls.Add(this.lblFriends);
            this.Controls.Add(this.flpActions);
            this.Controls.Add(this.lblConnectionStatus);
            this.Controls.Add(this.lblUserInfo);
            this.Controls.Add(this.lblWelcome);
            this.Name = "MainForm";
            this.Text = "PaL.X - Application Client";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}