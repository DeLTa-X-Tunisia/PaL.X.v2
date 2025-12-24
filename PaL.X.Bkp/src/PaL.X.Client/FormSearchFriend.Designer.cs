namespace PaL.X.Client
{
    partial class FormSearchFriend
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.lblSearch = new System.Windows.Forms.Label();
            this.lstUsers = new System.Windows.Forms.ListView();
            this.colImage = new System.Windows.Forms.ColumnHeader();
            this.colFirstName = new System.Windows.Forms.ColumnHeader();
            this.colLastName = new System.Windows.Forms.ColumnHeader();
            this.colAge = new System.Windows.Forms.ColumnHeader();
            this.colGender = new System.Windows.Forms.ColumnHeader();
            this.colCountry = new System.Windows.Forms.ColumnHeader();
            this.SuspendLayout();
            // 
            // txtSearch
            // 
            this.txtSearch.Location = new System.Drawing.Point(12, 35);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(776, 27);
            this.txtSearch.TabIndex = 0;
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
            // 
            // lblSearch
            // 
            this.lblSearch.AutoSize = true;
            this.lblSearch.Location = new System.Drawing.Point(12, 12);
            this.lblSearch.Name = "lblSearch";
            this.lblSearch.Size = new System.Drawing.Size(250, 20);
            this.lblSearch.TabIndex = 1;
            this.lblSearch.Text = "Rechercher (Nom, Prénom, Pays) :";
            // 
            // lstUsers
            // 
            this.lstUsers.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colImage,
            this.colFirstName,
            this.colLastName,
            this.colAge,
            this.colGender,
            this.colCountry});
            this.lstUsers.FullRowSelect = true;
            this.lstUsers.GridLines = true;
            this.lstUsers.Location = new System.Drawing.Point(12, 80);
            this.lstUsers.Name = "lstUsers";
            this.lstUsers.Size = new System.Drawing.Size(776, 358);
            this.lstUsers.TabIndex = 2;
            this.lstUsers.UseCompatibleStateImageBehavior = false;
            this.lstUsers.View = System.Windows.Forms.View.Details;
            // 
            // colImage
            // 
            this.colImage.Text = "";
            this.colImage.Width = 50;
            // 
            // colFirstName
            // 
            this.colFirstName.Text = "Prénom";
            this.colFirstName.Width = 150;
            // 
            // colLastName
            // 
            this.colLastName.Text = "Nom";
            this.colLastName.Width = 150;
            // 
            // colAge
            // 
            this.colAge.Text = "Âge";
            this.colAge.Width = 80;
            // 
            // colGender
            // 
            this.colGender.Text = "Genre";
            this.colGender.Width = 100;
            // 
            // colCountry
            // 
            this.colCountry.Text = "Pays";
            this.colCountry.Width = 100;
            // 
            // FormSearchFriend
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.lstUsers);
            this.Controls.Add(this.lblSearch);
            this.Controls.Add(this.txtSearch);
            this.Name = "FormSearchFriend";
            this.Text = "Chercher un Ami";
            this.Load += new System.EventHandler(this.FormSearchFriend_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Label lblSearch;
        private System.Windows.Forms.ListView lstUsers;
        private System.Windows.Forms.ColumnHeader colImage;
        private System.Windows.Forms.ColumnHeader colFirstName;
        private System.Windows.Forms.ColumnHeader colLastName;
        private System.Windows.Forms.ColumnHeader colAge;
        private System.Windows.Forms.ColumnHeader colGender;
        private System.Windows.Forms.ColumnHeader colCountry;
    }
}
