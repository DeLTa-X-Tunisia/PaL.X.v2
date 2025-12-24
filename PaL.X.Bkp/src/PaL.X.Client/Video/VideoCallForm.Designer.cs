namespace PaL.X.Client.Video
{
    partial class VideoCallForm
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
            this.mainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.headerPanel = new System.Windows.Forms.Panel();
            this._lblName = new System.Windows.Forms.Label();
            this._lblStatus = new System.Windows.Forms.Label();
            this._videoHost = new System.Windows.Forms.Panel();
            this._webView = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.bottomBar = new System.Windows.Forms.Panel();
            this.centeringTable = new System.Windows.Forms.TableLayoutPanel();
            this._buttonsPanel = new System.Windows.Forms.FlowLayoutPanel();
            this._btnCam = new System.Windows.Forms.Button();
            this._btnMic = new System.Windows.Forms.Button();
            this._btnHangup = new System.Windows.Forms.Button();
            this._btnAccept = new System.Windows.Forms.Button();
            this._btnReject = new System.Windows.Forms.Button();
            this._tt = new System.Windows.Forms.ToolTip();

            this.mainLayout.SuspendLayout();
            this.headerPanel.SuspendLayout();
            this._videoHost.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._webView)).BeginInit();
            this.bottomBar.SuspendLayout();
            this.centeringTable.SuspendLayout();
            this._buttonsPanel.SuspendLayout();
            this.SuspendLayout();

            // 
            // mainLayout
            // 
            this.mainLayout.BackColor = System.Drawing.Color.Black;
            this.mainLayout.ColumnCount = 1;
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Controls.Add(this.headerPanel, 0, 0);
            this.mainLayout.Controls.Add(this._videoHost, 0, 1);
            this.mainLayout.Controls.Add(this.bottomBar, 0, 2);
            this.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainLayout.Location = new System.Drawing.Point(0, 0);
            this.mainLayout.Name = "mainLayout";
            this.mainLayout.RowCount = 3;
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.mainLayout.Size = new System.Drawing.Size(900, 600);
            this.mainLayout.TabIndex = 0;

            // 
            // headerPanel
            // 
            this.headerPanel.BackColor = System.Drawing.Color.Transparent;
            this.headerPanel.Controls.Add(this._lblName);
            this.headerPanel.Controls.Add(this._lblStatus);
            this.headerPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headerPanel.Location = new System.Drawing.Point(0, 0);
            this.headerPanel.Margin = new System.Windows.Forms.Padding(0);
            this.headerPanel.Name = "headerPanel";
            this.headerPanel.Padding = new System.Windows.Forms.Padding(20, 10, 20, 0);
            this.headerPanel.Size = new System.Drawing.Size(900, 60);
            this.headerPanel.TabIndex = 0;

            // 
            // _lblName
            // 
            this._lblName.AutoSize = true;
            this._lblName.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this._lblName.ForeColor = System.Drawing.Color.White;
            this._lblName.Location = new System.Drawing.Point(20, 10);
            this._lblName.Name = "_lblName";
            this._lblName.Size = new System.Drawing.Size(100, 25);
            this._lblName.TabIndex = 0;
            this._lblName.Text = "Peer Name";

            // 
            // _lblStatus
            // 
            this._lblStatus.AutoSize = true;
            this._lblStatus.Font = new System.Drawing.Font("Segoe UI", 10F);
            this._lblStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this._lblStatus.Location = new System.Drawing.Point(22, 38);
            this._lblStatus.Name = "_lblStatus";
            this._lblStatus.Size = new System.Drawing.Size(80, 19);
            this._lblStatus.TabIndex = 1;
            this._lblStatus.Text = "Connexion…";

            // 
            // _videoHost
            // 
            this._videoHost.BackColor = System.Drawing.Color.Black;
            this._videoHost.Controls.Add(this._webView);
            this._videoHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this._videoHost.Location = new System.Drawing.Point(0, 60);
            this._videoHost.Margin = new System.Windows.Forms.Padding(0);
            this._videoHost.Name = "_videoHost";
            this._videoHost.Size = new System.Drawing.Size(900, 440);
            this._videoHost.TabIndex = 1;

            // 
            // _webView
            // 
            this._webView.Dock = System.Windows.Forms.DockStyle.Fill;
            this._webView.Location = new System.Drawing.Point(0, 0);
            this._webView.Name = "_webView";
            this._webView.Size = new System.Drawing.Size(900, 440);
            this._webView.TabIndex = 0;
            this._webView.ZoomFactor = 1D;

            // 
            // bottomBar
            // 
            this.bottomBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.bottomBar.Controls.Add(this.centeringTable);
            this.bottomBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bottomBar.Location = new System.Drawing.Point(0, 500);
            this.bottomBar.Margin = new System.Windows.Forms.Padding(0);
            this.bottomBar.Name = "bottomBar";
            this.bottomBar.Size = new System.Drawing.Size(900, 100);
            this.bottomBar.TabIndex = 2;

            // 
            // centeringTable
            // 
            this.centeringTable.BackColor = System.Drawing.Color.Transparent;
            this.centeringTable.ColumnCount = 3;
            this.centeringTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.centeringTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.centeringTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.centeringTable.Controls.Add(this._buttonsPanel, 1, 0);
            this.centeringTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.centeringTable.Location = new System.Drawing.Point(0, 0);
            this.centeringTable.Name = "centeringTable";
            this.centeringTable.RowCount = 1;
            this.centeringTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.centeringTable.Size = new System.Drawing.Size(900, 100);
            this.centeringTable.TabIndex = 0;

            // 
            // _buttonsPanel
            // 
            this._buttonsPanel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._buttonsPanel.AutoSize = true;
            this._buttonsPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this._buttonsPanel.BackColor = System.Drawing.Color.Transparent;
            this._buttonsPanel.Controls.Add(this._btnMic);
            this._buttonsPanel.Controls.Add(this._btnHangup);
            this._buttonsPanel.Controls.Add(this._btnCam);
            this._buttonsPanel.Controls.Add(this._btnAccept);
            this._buttonsPanel.Controls.Add(this._btnReject);
            this._buttonsPanel.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this._buttonsPanel.Location = new System.Drawing.Point(225, 5);
            this._buttonsPanel.Name = "_buttonsPanel";
            this._buttonsPanel.Size = new System.Drawing.Size(450, 90);
            this._buttonsPanel.TabIndex = 0;
            this._buttonsPanel.WrapContents = false;

            // 
            // _btnCam
            // 
            this._btnCam.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this._btnCam.Cursor = System.Windows.Forms.Cursors.Hand;
            this._btnCam.FlatAppearance.BorderSize = 0;
            this._btnCam.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnCam.Location = new System.Drawing.Point(15, 15);
            this._btnCam.Margin = new System.Windows.Forms.Padding(15);
            this._btnCam.Name = "_btnCam";
            this._btnCam.Size = new System.Drawing.Size(60, 60);
            this._btnCam.TabIndex = 0;
            this._btnCam.UseVisualStyleBackColor = false;

            // 
            // _btnMic
            // 
            this._btnMic.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this._btnMic.Cursor = System.Windows.Forms.Cursors.Hand;
            this._btnMic.FlatAppearance.BorderSize = 0;
            this._btnMic.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnMic.Location = new System.Drawing.Point(105, 15);
            this._btnMic.Margin = new System.Windows.Forms.Padding(15);
            this._btnMic.Name = "_btnMic";
            this._btnMic.Size = new System.Drawing.Size(60, 60);
            this._btnMic.TabIndex = 1;
            this._btnMic.UseVisualStyleBackColor = false;

            // 
            // _btnHangup
            // 
            this._btnHangup.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(87)))), ((int)(((byte)(87)))));
            this._btnHangup.Cursor = System.Windows.Forms.Cursors.Hand;
            this._btnHangup.FlatAppearance.BorderSize = 0;
            this._btnHangup.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnHangup.Location = new System.Drawing.Point(195, 15);
            this._btnHangup.Margin = new System.Windows.Forms.Padding(15);
            this._btnHangup.Name = "_btnHangup";
            this._btnHangup.Size = new System.Drawing.Size(60, 60);
            this._btnHangup.TabIndex = 2;
            this._btnHangup.UseVisualStyleBackColor = false;

            // 
            // _btnAccept
            // 
            this._btnAccept.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this._btnAccept.Cursor = System.Windows.Forms.Cursors.Hand;
            this._btnAccept.FlatAppearance.BorderSize = 0;
            this._btnAccept.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnAccept.Location = new System.Drawing.Point(285, 15);
            this._btnAccept.Margin = new System.Windows.Forms.Padding(15);
            this._btnAccept.Name = "_btnAccept";
            this._btnAccept.Size = new System.Drawing.Size(60, 60);
            this._btnAccept.TabIndex = 3;
            this._btnAccept.UseVisualStyleBackColor = false;
            this._btnAccept.Visible = false;

            // 
            // _btnReject
            // 
            this._btnReject.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(87)))), ((int)(((byte)(87)))));
            this._btnReject.Cursor = System.Windows.Forms.Cursors.Hand;
            this._btnReject.FlatAppearance.BorderSize = 0;
            this._btnReject.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnReject.Location = new System.Drawing.Point(375, 15);
            this._btnReject.Margin = new System.Windows.Forms.Padding(15);
            this._btnReject.Name = "_btnReject";
            this._btnReject.Size = new System.Drawing.Size(60, 60);
            this._btnReject.TabIndex = 4;
            this._btnReject.UseVisualStyleBackColor = false;
            this._btnReject.Visible = false;

            // 
            // VideoCallForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.ClientSize = new System.Drawing.Size(900, 600);
            this.Controls.Add(this.mainLayout);
            this.ForeColor = System.Drawing.Color.White;
            this.MinimumSize = new System.Drawing.Size(600, 400);
            this.Name = "VideoCallForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Appel vidéo";
            this.mainLayout.ResumeLayout(false);
            this.headerPanel.ResumeLayout(false);
            this.headerPanel.PerformLayout();
            this._videoHost.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._webView)).EndInit();
            this.bottomBar.ResumeLayout(false);
            this.centeringTable.ResumeLayout(false);
            this._buttonsPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.Panel headerPanel;
        private System.Windows.Forms.Label _lblName;
        private System.Windows.Forms.Label _lblStatus;
        private System.Windows.Forms.Panel _videoHost;
        private Microsoft.Web.WebView2.WinForms.WebView2 _webView;
        private System.Windows.Forms.Panel bottomBar;
        private System.Windows.Forms.TableLayoutPanel centeringTable;
        private System.Windows.Forms.FlowLayoutPanel _buttonsPanel;
        private System.Windows.Forms.Button _btnCam;
        private System.Windows.Forms.Button _btnMic;
        private System.Windows.Forms.Button _btnHangup;
        private System.Windows.Forms.Button _btnAccept;
        private System.Windows.Forms.Button _btnReject;
        private System.Windows.Forms.ToolTip _tt;
    }
}
