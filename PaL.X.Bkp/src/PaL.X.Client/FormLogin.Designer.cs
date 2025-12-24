namespace PaL.X.Client
{
    partial class FormLogin : System.Windows.Forms.Form
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabLogin;
        private System.Windows.Forms.TabPage tabRegister;
        private System.Windows.Forms.TextBox txtLoginUsername;
        private System.Windows.Forms.TextBox txtLoginPassword;
        private System.Windows.Forms.Button btnLogin;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtRegisterUsername;
        private System.Windows.Forms.TextBox txtRegisterEmail;
        private System.Windows.Forms.TextBox txtRegisterPassword;
        private System.Windows.Forms.TextBox txtRegisterConfirmPassword;
        private System.Windows.Forms.Button btnRegister;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label lblStatus;

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
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabLogin = new System.Windows.Forms.TabPage();
            this.btnLogin = new System.Windows.Forms.Button();
            this.txtLoginPassword = new System.Windows.Forms.TextBox();
            this.txtLoginUsername = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.tabRegister = new System.Windows.Forms.TabPage();
            this.btnRegister = new System.Windows.Forms.Button();
            this.txtRegisterConfirmPassword = new System.Windows.Forms.TextBox();
            this.txtRegisterPassword = new System.Windows.Forms.TextBox();
            this.txtRegisterEmail = new System.Windows.Forms.TextBox();
            this.txtRegisterUsername = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.tabControl1.SuspendLayout();
            this.tabLogin.SuspendLayout();
            this.tabRegister.SuspendLayout();
            this.SuspendLayout();
            
            // tabControl1
            this.tabControl1.Controls.Add(this.tabLogin);
            this.tabControl1.Controls.Add(this.tabRegister);
            this.tabControl1.Location = new System.Drawing.Point(12, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(360, 250);
            this.tabControl1.TabIndex = 0;
            
            // tabLogin
            this.tabLogin.Controls.Add(this.btnLogin);
            this.tabLogin.Controls.Add(this.txtLoginPassword);
            this.tabLogin.Controls.Add(this.txtLoginUsername);
            this.tabLogin.Controls.Add(this.label2);
            this.tabLogin.Controls.Add(this.label1);
            this.tabLogin.Location = new System.Drawing.Point(4, 24);
            this.tabLogin.Name = "tabLogin";
            this.tabLogin.Padding = new System.Windows.Forms.Padding(3);
            this.tabLogin.Size = new System.Drawing.Size(352, 222);
            this.tabLogin.TabIndex = 0;
            this.tabLogin.Text = "Connexion";
            this.tabLogin.UseVisualStyleBackColor = true;
            
            // label1
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(30, 30);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(90, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "Nom d\'utilisateur:";
            
            // label2
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(30, 80);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(83, 15);
            this.label2.TabIndex = 1;
            this.label2.Text = "Mot de passe:";
            
            // txtLoginUsername
            this.txtLoginUsername.Location = new System.Drawing.Point(140, 27);
            this.txtLoginUsername.Name = "txtLoginUsername";
            this.txtLoginUsername.Size = new System.Drawing.Size(180, 23);
            this.txtLoginUsername.TabIndex = 2;
            
            // txtLoginPassword
            this.txtLoginPassword.Location = new System.Drawing.Point(140, 77);
            this.txtLoginPassword.Name = "txtLoginPassword";
            this.txtLoginPassword.PasswordChar = '*';
            this.txtLoginPassword.Size = new System.Drawing.Size(180, 23);
            this.txtLoginPassword.TabIndex = 3;
            
            // btnLogin
            this.btnLogin.Location = new System.Drawing.Point(140, 130);
            this.btnLogin.Name = "btnLogin";
            this.btnLogin.Size = new System.Drawing.Size(100, 30);
            this.btnLogin.TabIndex = 4;
            this.btnLogin.Text = "Se connecter";
            this.btnLogin.UseVisualStyleBackColor = true;
            this.btnLogin.Click += new System.EventHandler(this.btnLogin_Click);
            
            // tabRegister
            this.tabRegister.Controls.Add(this.btnRegister);
            this.tabRegister.Controls.Add(this.txtRegisterConfirmPassword);
            this.tabRegister.Controls.Add(this.txtRegisterPassword);
            this.tabRegister.Controls.Add(this.txtRegisterEmail);
            this.tabRegister.Controls.Add(this.txtRegisterUsername);
            this.tabRegister.Controls.Add(this.label6);
            this.tabRegister.Controls.Add(this.label5);
            this.tabRegister.Controls.Add(this.label4);
            this.tabRegister.Controls.Add(this.label3);
            this.tabRegister.Location = new System.Drawing.Point(4, 24);
            this.tabRegister.Name = "tabRegister";
            this.tabRegister.Padding = new System.Windows.Forms.Padding(3);
            this.tabRegister.Size = new System.Drawing.Size(352, 222);
            this.tabRegister.TabIndex = 1;
            this.tabRegister.Text = "Inscription";
            this.tabRegister.UseVisualStyleBackColor = true;
            
            // label3
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(30, 25);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(90, 15);
            this.label3.TabIndex = 0;
            this.label3.Text = "Nom d\'utilisateur:";
            
            // label4
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(30, 55);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(39, 15);
            this.label4.TabIndex = 1;
            this.label4.Text = "Email:";
            
            // label5
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(30, 85);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(83, 15);
            this.label5.TabIndex = 2;
            this.label5.Text = "Mot de passe:";
            
            // label6
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(30, 115);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(109, 15);
            this.label6.TabIndex = 3;
            this.label6.Text = "Confirmer le mot de passe:";
            
            // txtRegisterUsername
            this.txtRegisterUsername.Location = new System.Drawing.Point(145, 22);
            this.txtRegisterUsername.Name = "txtRegisterUsername";
            this.txtRegisterUsername.Size = new System.Drawing.Size(180, 23);
            this.txtRegisterUsername.TabIndex = 4;
            
            // txtRegisterEmail
            this.txtRegisterEmail.Location = new System.Drawing.Point(145, 52);
            this.txtRegisterEmail.Name = "txtRegisterEmail";
            this.txtRegisterEmail.Size = new System.Drawing.Size(180, 23);
            this.txtRegisterEmail.TabIndex = 5;
            
            // txtRegisterPassword
            this.txtRegisterPassword.Location = new System.Drawing.Point(145, 82);
            this.txtRegisterPassword.Name = "txtRegisterPassword";
            this.txtRegisterPassword.PasswordChar = '*';
            this.txtRegisterPassword.Size = new System.Drawing.Size(180, 23);
            this.txtRegisterPassword.TabIndex = 6;
            
            // txtRegisterConfirmPassword
            this.txtRegisterConfirmPassword.Location = new System.Drawing.Point(145, 112);
            this.txtRegisterConfirmPassword.Name = "txtRegisterConfirmPassword";
            this.txtRegisterConfirmPassword.PasswordChar = '*';
            this.txtRegisterConfirmPassword.Size = new System.Drawing.Size(180, 23);
            this.txtRegisterConfirmPassword.TabIndex = 7;
            
            // btnRegister
            this.btnRegister.Location = new System.Drawing.Point(145, 150);
            this.btnRegister.Name = "btnRegister";
            this.btnRegister.Size = new System.Drawing.Size(100, 30);
            this.btnRegister.TabIndex = 8;
            this.btnRegister.Text = "S\'inscrire";
            this.btnRegister.UseVisualStyleBackColor = true;
            this.btnRegister.Click += new System.EventHandler(this.btnRegister_Click);
            
            // lblStatus
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(12, 270);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(0, 15);
            this.lblStatus.TabIndex = 1;
            
            // FormLogin
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 311);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.tabControl1);
            this.Name = "FormLogin";
            this.Text = "PaL.X - Connexion";
            this.tabControl1.ResumeLayout(false);
            this.tabLogin.ResumeLayout(false);
            this.tabLogin.PerformLayout();
            this.tabRegister.ResumeLayout(false);
            this.tabRegister.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}