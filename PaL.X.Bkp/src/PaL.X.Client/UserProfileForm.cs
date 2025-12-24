using PaL.X.Client.Services;
using PaL.X.Shared.DTOs;
using PaL.X.Shared.Enums;
using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows.Forms;

namespace PaL.X.Client
{
    public class UserProfileForm : Form
    {
    private readonly HttpClient _httpClient;
        private UserProfileDto _profile = null!;

        // UI Controls
    private PictureBox pbProfilePicture = null!;
    private TransparentClickablePanel pnlPhotoOverlay = null!;
    private ToolStrip _tsProfileStatus = null!;
    private ToolStripDropDownButton _ddProfileStatus = null!;
    private ImageList _profileStatusImages = null!;
        private Label lblStatus = null!;
    private Label lblName = null!;
        private Label lblRole = null!;
        private Label lblAge = null!;
        private Label lblJoined = null!;
        
        // Labels for fields
        private Label lblFirstName = null!;
        private Label lblLastName = null!;
        private Label lblGender = null!;
        private Label lblDob = null!;
        private Label lblCountry = null!;
        
        // Editable Fields
        private TextBox txtFirstName = null!;
        private TextBox txtLastName = null!;
        private DateTimePicker dtpDob = null!;
        private ComboBox cmbGender = null!;
        private TextBox txtCountry = null!;
        private Button btnSave = null!;

        // Visibility Controls (ComboBoxes)
        private ComboBox cmbVisFirstName = null!;
        private ComboBox cmbVisLastName = null!;
        private ComboBox cmbVisDob = null!;
        private ComboBox cmbVisGender = null!;
        private ComboBox cmbVisCountry = null!;
        private ComboBox cmbVisPhoto = null!;

        private byte[]? _newProfilePicture;

        // Constructor for Designer
        public UserProfileForm()
        {
            _httpClient = null!;
            InitializeComponent();
            SetupRuntimeUI();
        }

        public UserProfileForm(HttpClient httpClient)
        {
            _httpClient = httpClient;
            InitializeComponent();
            SetupRuntimeUI();
            LoadProfile();
        }

        private void InitializeComponent()
        {
            this.Text = "Profil Utilisateur";
            this.Size = new Size(500, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;

            // Profile Picture
            pbProfilePicture = new PictureBox();
            pbProfilePicture.Location = new Point(20, 20);
            pbProfilePicture.Size = new Size(100, 100);
            pbProfilePicture.SizeMode = PictureBoxSizeMode.StretchImage;
            pbProfilePicture.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(pbProfilePicture);

            lblName = new Label
            {
                Location = new Point(150, 32),
                AutoSize = true,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Text = string.Empty
            };
            this.Controls.Add(lblName);
            
            // Modern overlay for changing photo (hover)
            pnlPhotoOverlay = new TransparentClickablePanel();
            pnlPhotoOverlay.Size = new Size(pbProfilePicture.Width, pbProfilePicture.Height);
            pnlPhotoOverlay.Location = pbProfilePicture.Location;
            pnlPhotoOverlay.Visible = false;
            pnlPhotoOverlay.Cursor = Cursors.Hand;
            pnlPhotoOverlay.Paint += PnlPhotoOverlay_Paint;
            pnlPhotoOverlay.Click += BtnUploadPhoto_Click;
            this.Controls.Add(pnlPhotoOverlay);
            pnlPhotoOverlay.BringToFront();
            // Also allow clicking directly on the picture to edit
            pbProfilePicture.Click += BtnUploadPhoto_Click;
            
            // Hover behavior
            pbProfilePicture.MouseEnter += (s, e) => pnlPhotoOverlay.Visible = true;
            pbProfilePicture.MouseLeave += (s, e) =>
            {
                // Only hide if mouse truly left both picture and overlay
                var pos = PointToClient(Cursor.Position);
                if (!pbProfilePicture.Bounds.Contains(pos) && !pnlPhotoOverlay.Bounds.Contains(pos))
                    pnlPhotoOverlay.Visible = false;
            };
            pnlPhotoOverlay.MouseLeave += (s, e) =>
            {
                var pos = PointToClient(Cursor.Position);
                if (!pbProfilePicture.Bounds.Contains(pos))
                    pnlPhotoOverlay.Visible = false;
            };
            pnlPhotoOverlay.MouseEnter += (s, e) => pnlPhotoOverlay.Visible = true;

            pbProfilePicture.SizeChanged += (s, e) => SyncPhotoOverlayBounds();
            pbProfilePicture.LocationChanged += (s, e) => SyncPhotoOverlayBounds();
            SyncPhotoOverlayBounds();

            var photoToolTip = new ToolTip
            {
                AutomaticDelay = 120,
                ReshowDelay = 60,
                InitialDelay = 120
            };
            photoToolTip.SetToolTip(pnlPhotoOverlay, "Changer la photo");
            photoToolTip.SetToolTip(pbProfilePicture, "Changer la photo");

            cmbVisPhoto = new ComboBox();
            cmbVisPhoto.Items.Add("Public");
            cmbVisPhoto.Items.Add("Amis");
            cmbVisPhoto.Items.Add("Moi seul");
            cmbVisPhoto.Location = new Point(350, 55);
            cmbVisPhoto.Width = 100;
            cmbVisPhoto.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbVisPhoto.Visible = false;
            this.Controls.Add(cmbVisPhoto);

            // Status selector (modern, same as chat)
            lblStatus = new Label();
            lblStatus.Text = "Statut:";
            lblStatus.Location = new Point(20, 143);
            lblStatus.AutoSize = true;
            lblStatus.Font = new Font("Segoe UI", 10);
            this.Controls.Add(lblStatus);
            
            _tsProfileStatus = new ToolStrip();
            _tsProfileStatus.Dock = DockStyle.None;
            _tsProfileStatus.BackColor = Color.Transparent;
            _tsProfileStatus.AutoSize = true;
            _ddProfileStatus = new ToolStripDropDownButton
            {
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Text = "Statut"
            };
            // Load icons similar to chat
            _profileStatusImages = new ImageList { ImageSize = new Size(16,16), ColorDepth = ColorDepth.Depth32Bit };
            TryAddStatusImage("icon/status/en_ligne.ico", "Online");
            TryAddStatusImage("icon/status/hors_ligne.ico", "Offline");
            TryAddStatusImage("icon/status/absent.ico", "Away");
            TryAddStatusImage("icon/status/brb.ico", "BRB");
            TryAddStatusImage("icon/status/dnd.ico", "DoNotDisturb");
            TryAddStatusImage("icon/status/occupé.ico", "Busy");
            var statuses = new[]{ UserStatus.Online, UserStatus.Away, UserStatus.BRB, UserStatus.DoNotDisturb, UserStatus.Busy, UserStatus.Offline };
            foreach(var st in statuses)
            {
                var mi = new ToolStripMenuItem(st.GetDisplayName()){ Tag = st };
                if (_profileStatusImages.Images.ContainsKey(st.ToString())) mi.Image = _profileStatusImages.Images[st.ToString()];
                mi.Click += async (s,e)=>{
                    try
                    {
                        // Change my status via API/monitor through MainForm if available
                        // Find an open MainForm from Application.OpenForms
                        foreach (Form f in Application.OpenForms)
                        {
                            if (f is MainForm mf)
                            {
                                await mf.ChangeMyStatusAsync(st);
                                break;
                            }
                        }
                        _ddProfileStatus.Text = st.GetDisplayName();
                        if (_profileStatusImages.Images.ContainsKey(st.ToString())) _ddProfileStatus.Image = _profileStatusImages.Images[st.ToString()];
                        UpdateStatus(st);
                    }
                    catch { }
                };
                _ddProfileStatus.DropDownItems.Add(mi);
            }
            _tsProfileStatus.Items.Add(_ddProfileStatus);
            this.Controls.Add(_tsProfileStatus);
            // Position the selector in the top-right corner of the form
            _tsProfileStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            PositionStatusSelectorTopRight();
            this.SizeChanged += (s, e) => PositionStatusSelectorTopRight();

        
            // Role
            lblRole = new Label();
            lblRole.Text = "Rôle:";
            lblRole.Location = new Point(20, 183);
            lblRole.AutoSize = true;
            lblRole.Font = new Font("Segoe UI", 10);
            this.Controls.Add(lblRole);

            // Joined
            lblJoined = new Label();
            lblJoined.Text = "Inscrit le:";
            lblJoined.Location = new Point(20, 223);
            lblJoined.AutoSize = true;
            lblJoined.Font = new Font("Segoe UI", 10);
            this.Controls.Add(lblJoined);

            // First Name
            lblFirstName = new Label();
            lblFirstName.Text = "Prénom:";
            lblFirstName.Location = new Point(20, 263);
            lblFirstName.AutoSize = true;
            lblFirstName.Font = new Font("Segoe UI", 10);
            this.Controls.Add(lblFirstName);

            txtFirstName = new TextBox();
            txtFirstName.Location = new Point(150, 260);
            txtFirstName.Width = 180;
            txtFirstName.ReadOnly = true;
            this.Controls.Add(txtFirstName);

            cmbVisFirstName = new ComboBox();
            cmbVisFirstName.Items.Add("Public");
            cmbVisFirstName.Items.Add("Amis");
            cmbVisFirstName.Items.Add("Moi seul");
            cmbVisFirstName.Location = new Point(350, 260);
            cmbVisFirstName.Width = 100;
            cmbVisFirstName.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbVisFirstName.Visible = false;
            this.Controls.Add(cmbVisFirstName);

            // Last Name
            lblLastName = new Label();
            lblLastName.Text = "Nom:";
            lblLastName.Location = new Point(20, 303);
            lblLastName.AutoSize = true;
            lblLastName.Font = new Font("Segoe UI", 10);
            this.Controls.Add(lblLastName);

            txtLastName = new TextBox();
            txtLastName.Location = new Point(150, 300);
            txtLastName.Width = 180;
            txtLastName.ReadOnly = true;
            this.Controls.Add(txtLastName);

            cmbVisLastName = new ComboBox();
            cmbVisLastName.Items.Add("Public");
            cmbVisLastName.Items.Add("Amis");
            cmbVisLastName.Items.Add("Moi seul");
            cmbVisLastName.Location = new Point(350, 300);
            cmbVisLastName.Width = 100;
            cmbVisLastName.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbVisLastName.Visible = false;
            this.Controls.Add(cmbVisLastName);

            // Gender
            lblGender = new Label();
            lblGender.Text = "Genre:";
            lblGender.Location = new Point(20, 343);
            lblGender.AutoSize = true;
            lblGender.Font = new Font("Segoe UI", 10);
            this.Controls.Add(lblGender);

            cmbGender = new ComboBox();
            cmbGender.Items.AddRange(new object[] { "Homme", "Femme", "Autre" });
            cmbGender.Location = new Point(150, 340);
            cmbGender.Width = 180;
            cmbGender.Enabled = false;
            this.Controls.Add(cmbGender);

            cmbVisGender = new ComboBox();
            cmbVisGender.Items.Add("Public");
            cmbVisGender.Items.Add("Amis");
            cmbVisGender.Items.Add("Moi seul");
            cmbVisGender.Location = new Point(350, 340);
            cmbVisGender.Width = 100;
            cmbVisGender.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbVisGender.Visible = false;
            this.Controls.Add(cmbVisGender);

            // DOB
            lblDob = new Label();
            lblDob.Text = "Date de naissance:";
            lblDob.Location = new Point(20, 383);
            lblDob.AutoSize = true;
            lblDob.Font = new Font("Segoe UI", 10);
            this.Controls.Add(lblDob);

            dtpDob = new DateTimePicker();
            dtpDob.Format = DateTimePickerFormat.Short;
            dtpDob.Location = new Point(150, 380);
            dtpDob.Width = 180;
            dtpDob.Enabled = false;
            this.Controls.Add(dtpDob);

            cmbVisDob = new ComboBox();
            cmbVisDob.Items.Add("Public");
            cmbVisDob.Items.Add("Amis");
            cmbVisDob.Items.Add("Moi seul");
            cmbVisDob.Location = new Point(350, 380);
            cmbVisDob.Width = 100;
            cmbVisDob.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbVisDob.Visible = false;
            this.Controls.Add(cmbVisDob);

            // Age
            lblAge = new Label();
            lblAge.Text = "Âge:";
            lblAge.Location = new Point(20, 423);
            lblAge.AutoSize = true;
            lblAge.Font = new Font("Segoe UI", 10);
            this.Controls.Add(lblAge);

            // Country
            lblCountry = new Label();
            lblCountry.Text = "Pays:";
            lblCountry.Location = new Point(20, 463);
            lblCountry.AutoSize = true;
            lblCountry.Font = new Font("Segoe UI", 10);
            this.Controls.Add(lblCountry);

            txtCountry = new TextBox();
            txtCountry.Location = new Point(150, 460);
            txtCountry.Width = 180;
            txtCountry.ReadOnly = true;
            this.Controls.Add(txtCountry);

            cmbVisCountry = new ComboBox();
            cmbVisCountry.Items.Add("Public");
            cmbVisCountry.Items.Add("Amis");
            cmbVisCountry.Items.Add("Moi seul");
            cmbVisCountry.Location = new Point(350, 460);
            cmbVisCountry.Width = 100;
            cmbVisCountry.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbVisCountry.Visible = false;
            this.Controls.Add(cmbVisCountry);

            // Save Button
            btnSave = new Button();
            btnSave.Text = "Enregistrer";
            btnSave.Location = new Point(150, 520);
            btnSave.Size = new Size(200, 40);
            btnSave.BackColor = Color.DodgerBlue;
            btnSave.ForeColor = Color.White;
            btnSave.Click += BtnSave_Click;
            btnSave.Visible = false;
            this.Controls.Add(btnSave);
        }

        private void SyncPhotoOverlayBounds()
        {
            if (pnlPhotoOverlay == null || pbProfilePicture == null)
                return;

            pnlPhotoOverlay.Location = pbProfilePicture.Location;
            pnlPhotoOverlay.Size = pbProfilePicture.Size;
            pnlPhotoOverlay.Invalidate();
        }

        private void PnlPhotoOverlay_Paint(object? sender, PaintEventArgs e)
        {
            // Overlay stays transparent; no background drawing required.
            // We keep this handler to preserve potential future styling without repaint issues.
        }

        private sealed class TransparentClickablePanel : Panel
        {
            public TransparentClickablePanel()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
                BackColor = Color.Transparent;
            }

            protected override void OnPaintBackground(PaintEventArgs pevent)
            {
                // Skip background painting to remain transparent.
            }
        }

        // No edit badge rendering required anymore; overlay remains clickable without icon.

        // Helper to place status selector at top-right with a small margin
        private void PositionStatusSelectorTopRight()
        {
            if (_tsProfileStatus != null)
            {
                int margin = 10;
                var preferred = _tsProfileStatus.PreferredSize;
                _tsProfileStatus.Location = new Point(this.ClientSize.Width - preferred.Width - margin, margin);
                _tsProfileStatus.BringToFront();
            }
        }

        private void TryAddStatusImage(string resourceKey, string alias)
        {
            if (_profileStatusImages.Images.ContainsKey(alias))
            {
                return;
            }

            var image = ResourceImageStore.LoadImage(resourceKey, new Size(16, 16));
            if (image != null)
            {
                _profileStatusImages.Images.Add(alias, image);
            }
        }

        private void SetupRuntimeUI()
        {
            this.Text = "Mon Profil";

            if (pnlPhotoOverlay != null)
            {
                pnlPhotoOverlay.Visible = false;
            }

            if (cmbVisPhoto != null)
            {
                cmbVisPhoto.Visible = true;
            }

            if (txtFirstName != null)
            {
                txtFirstName.ReadOnly = false;
            }

            if (cmbVisFirstName != null)
            {
                cmbVisFirstName.Visible = true;
            }

            if (txtLastName != null)
            {
                txtLastName.ReadOnly = false;
            }

            if (cmbVisLastName != null)
            {
                cmbVisLastName.Visible = true;
            }

            if (cmbGender != null)
            {
                cmbGender.Enabled = true;
            }

            if (cmbVisGender != null)
            {
                cmbVisGender.Visible = true;
            }

            if (dtpDob != null)
            {
                dtpDob.Enabled = true;
            }

            if (cmbVisDob != null)
            {
                cmbVisDob.Visible = true;
            }

            if (txtCountry != null)
            {
                txtCountry.ReadOnly = false;
            }

            if (cmbVisCountry != null)
            {
                cmbVisCountry.Visible = true;
            }

            if (btnSave != null)
            {
                btnSave.Visible = true;
            }
        }

        private void SetVis(ComboBox cmb, VisibilityLevel level)
        {
            cmb.SelectedIndex = (int)level;
        }

        private VisibilityLevel GetVis(ComboBox cmb)
        {
            return (VisibilityLevel)cmb.SelectedIndex;
        }

        private async void LoadProfile()
        {
            try
            {
                const string url = "https://localhost:5001/api/profile";
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    _profile = await response.Content.ReadFromJsonAsync<UserProfileDto>() ?? new UserProfileDto();
                    PopulateFields();
                }
                else
                {
                    PalMessageBox.Show("Impossible de charger le profil.");
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                PalMessageBox.Show($"Erreur: {ex.Message}");
            }
        }

        private void PopulateFields()
        {
            // Profile Picture
            if (_profile.ProfilePicture != null && _profile.ProfilePicture.Length > 0)
            {
                using (var ms = new MemoryStream(_profile.ProfilePicture))
                {
                    pbProfilePicture.Image = Image.FromStream(ms);
                }
            }
            else
            {
                // Placeholder logic
                string genderIcon = "gender/autre.ico";
                if (string.Equals(_profile.Gender, "Homme", StringComparison.OrdinalIgnoreCase)) genderIcon = "gender/homme.ico";
                else if (string.Equals(_profile.Gender, "Femme", StringComparison.OrdinalIgnoreCase)) genderIcon = "gender/femme.ico";

                var fallbackImage = ResourceImageStore.LoadImage(genderIcon, new Size(pbProfilePicture.Width, pbProfilePicture.Height));
                if (fallbackImage != null)
                {
                    pbProfilePicture.Image?.Dispose();
                    pbProfilePicture.Image = fallbackImage;
                }
            }

            // Read-only info
            string statusText = _profile.CurrentStatus switch
            {
                UserStatus.Online => "En ligne",
                UserStatus.Offline => "Hors ligne",
                UserStatus.Away => "Absent",
                UserStatus.Busy => "Occupé",
                UserStatus.DoNotDisturb => "Ne pas déranger",
                UserStatus.BRB => "BRB",
                _ => "Inconnu"
            };
            
            Color statusColor = _profile.CurrentStatus switch
            {
                UserStatus.Online => Color.Green,
                UserStatus.Offline => Color.Gray,
                UserStatus.Busy => Color.Red,
                UserStatus.BRB => Color.Orange,
                UserStatus.DoNotDisturb => Color.Violet,
                UserStatus.Away => Color.Blue,
                _ => Color.Gray
            };
            
            lblStatus.Text = $"Statut: {statusText}";
            lblStatus.ForeColor = statusColor;
            // Also sync the dropdown text/icon now that _profile is loaded
            if (_ddProfileStatus != null)
            {
                _ddProfileStatus.Text = _profile.CurrentStatus.GetDisplayName();
                if (_profileStatusImages != null && _profileStatusImages.Images.ContainsKey(_profile.CurrentStatus.ToString()))
                {
                    _ddProfileStatus.Image = _profileStatusImages.Images[_profile.CurrentStatus.ToString()];
                }
            }
            
            lblRole.Text = $"Rôle: {(_profile.IsAdmin ? "Administrateur" : "Utilisateur")}";
            lblJoined.Text = $"Inscrit le: {_profile.CreatedAt:dd/MM/yyyy}";
            
            // Age Calculation
            int age = 0;

                var fullName = $"{_profile.FirstName} {_profile.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    fullName = string.IsNullOrWhiteSpace(_profile.DisplayedName)
                        ? _profile.Username ?? "Utilisateur"
                        : _profile.DisplayedName;
                }

                lblName.Text = fullName;
            if (_profile.DateOfBirth != DateTime.MinValue)
            {
                age = DateTime.Today.Year - _profile.DateOfBirth.Year;
                if (_profile.DateOfBirth.Date > DateTime.Today.AddYears(-age)) age--;
            }
            lblAge.Text = $"Âge: {(age > 0 ? age.ToString() + " ans" : "N/A")}";

            // Editable Fields
            txtFirstName.Text = _profile.FirstName;
            txtLastName.Text = _profile.LastName;
            txtCountry.Text = _profile.Country;
            
            if (!string.IsNullOrEmpty(_profile.Gender))
                cmbGender.SelectedItem = _profile.Gender;
            
            if (_profile.DateOfBirth != DateTime.MinValue)
                dtpDob.Value = _profile.DateOfBirth;

            SetVis(cmbVisFirstName, _profile.VisibilityFirstName);
            SetVis(cmbVisLastName, _profile.VisibilityLastName);
            SetVis(cmbVisDob, _profile.VisibilityDateOfBirth);
            SetVis(cmbVisGender, _profile.VisibilityGender);
            SetVis(cmbVisCountry, _profile.VisibilityCountry);
            SetVis(cmbVisPhoto, _profile.VisibilityProfilePicture);
        }

        // Mise à jour live du statut (appelée depuis MainForm via SignalR)
        public void UpdateStatus(UserStatus status)
        {
            _profile.CurrentStatus = status;

            // Libellé
            string statusText = status switch
            {
                UserStatus.Online => "En ligne",
                UserStatus.Offline => "Hors ligne",
                UserStatus.Away => "Absent",
                UserStatus.Busy => "Occupé",
                UserStatus.DoNotDisturb => "Ne pas déranger",
                UserStatus.BRB => "BRB",
                _ => "Inconnu"
            };

            // Couleur
            Color statusColor = status switch
            {
                UserStatus.Online => Color.Green,
                UserStatus.Offline => Color.Gray,
                UserStatus.Busy => Color.Red,
                UserStatus.BRB => Color.Orange,
                UserStatus.DoNotDisturb => Color.Violet,
                UserStatus.Away => Color.Blue,
                _ => Color.Gray
            };

            lblStatus.Text = $"Statut: {statusText}";
            lblStatus.ForeColor = statusColor;
            // Sync dropdown
            if (_ddProfileStatus != null)
            {
                _ddProfileStatus.Text = status.GetDisplayName();
                if (_profileStatusImages != null && _profileStatusImages.Images.ContainsKey(status.ToString()))
                {
                    _ddProfileStatus.Image = _profileStatusImages.Images[status.ToString()];
                }
            }

            // Mettre à jour l'icône de statut dans l'en-tête
            var statusIconResource = status.GetIconPath();
            var statusIcon = ResourceImageStore.LoadImage(statusIconResource, new Size(16, 16));
            if (statusIcon != null)
            {
                bool applied = false;
                foreach (Control c in this.Controls)
                {
                    if (applied)
                    {
                        break;
                    }

                    if (c is Panel pnl)
                    {
                        foreach (Control cc in pnl.Controls)
                        {
                            if (cc is PictureBox pb && pb.Size.Width == 16 && pb.Size.Height == 16)
                            {
                                pb.Image?.Dispose();
                                pb.Image = statusIcon;
                                applied = true;
                                break;
                            }
                        }
                    }
                }

                if (!applied)
                {
                    statusIcon.Dispose();
                }
            }
        }

        private void BtnUploadPhoto_Click(object? sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Images|*.jpg;*.jpeg;*.png";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    _newProfilePicture = File.ReadAllBytes(ofd.FileName);
                    using (var ms = new MemoryStream(_newProfilePicture))
                    {
                        pbProfilePicture.Image = Image.FromStream(ms);
                    }
                }
            }
        }

        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            var updateDto = new UpdateProfileDto
            {
                FirstName = txtFirstName.Text,
                LastName = txtLastName.Text,
                DisplayedName = _profile.DisplayedName, // Keep existing
                DateOfBirth = dtpDob.Value,
                Gender = cmbGender.SelectedItem?.ToString() ?? "Autre",
                Country = txtCountry.Text,
                ProfilePicture = _newProfilePicture ?? _profile.ProfilePicture,
                
                VisibilityFirstName = GetVis(cmbVisFirstName),
                VisibilityLastName = GetVis(cmbVisLastName),
                VisibilityDateOfBirth = GetVis(cmbVisDob),
                VisibilityGender = GetVis(cmbVisGender),
                VisibilityCountry = GetVis(cmbVisCountry),
                VisibilityProfilePicture = GetVis(cmbVisPhoto)
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync("https://localhost:5001/api/profile", updateDto);
                if (response.IsSuccessStatusCode)
                {
                    PalMessageBox.Show("Profil mis à jour !");
                    this.Close();
                }
                else
                {
                    PalMessageBox.Show("Erreur lors de la mise à jour.");
                }
            }
            catch (Exception ex)
            {
                PalMessageBox.Show($"Erreur: {ex.Message}");
            }
        }
    }
}
