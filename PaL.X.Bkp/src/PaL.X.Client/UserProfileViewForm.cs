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
    public class UserProfileViewForm : Form
    {
        private readonly HttpClient _httpClient;
        private readonly int _targetUserId;
        private UserProfileDto _profile = null!;

        private PictureBox _picture = null!;
        private Label _lblDisplayName = null!;
        private Label _lblAlias = null!;
    private Label _lblUsername = null!;
    private Label _lblFirstName = null!;
    private Label _lblLastName = null!;
        private Label _lblStatus = null!;
        private Label _lblRole = null!;
    private Label _lblGender = null!;
    private Label _lblDob = null!;
    private Label _lblAge = null!;
        private Label _lblCountry = null!;
        private Label _lblJoined = null!;
    private TableLayoutPanel _detailsLayout = null!;

        // Designer constructor
        public UserProfileViewForm()
        {
            _httpClient = null!;
            _targetUserId = 0;
            InitializeComponent();
        }

        public UserProfileViewForm(HttpClient httpClient, int targetUserId)
        {
            _httpClient = httpClient;
            _targetUserId = targetUserId;
            InitializeComponent();
            LoadProfile();
        }

        private void InitializeComponent()
        {
            Text = "Voir Profil";
            Size = new Size(480, 540);
            BackColor = Color.White;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 150,
                Padding = new Padding(20),
                BackColor = Color.WhiteSmoke
            };
            Controls.Add(headerPanel);

            _picture = new PictureBox
            {
                Size = new Size(96, 96),
                Location = new Point(20, 26),
                SizeMode = PictureBoxSizeMode.StretchImage,
                BorderStyle = BorderStyle.FixedSingle
            };
            headerPanel.Controls.Add(_picture);

            _lblDisplayName = new Label
            {
                Location = new Point(140, 30),
                AutoSize = true,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Text = string.Empty
            };
            headerPanel.Controls.Add(_lblDisplayName);

            _lblAlias = new Label
            {
                Location = new Point(140, 65),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Italic),
                ForeColor = Color.DimGray,
                Text = string.Empty
            };
            headerPanel.Controls.Add(_lblAlias);

            _lblStatus = new Label
            {
                Location = new Point(140, 95),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Text = "Statut: --"
            };
            headerPanel.Controls.Add(_lblStatus);

            _detailsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 0,
                Padding = new Padding(20, 10, 20, 20),
                AutoScroll = true,
                BackColor = Color.White
            };
            _detailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            _detailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
            Controls.Add(_detailsLayout);

            AddDetailRow("Prénom", out _lblFirstName);
            AddDetailRow("Nom", out _lblLastName);
            AddDetailRow("Nom d'utilisateur", out _lblUsername);
            AddDetailRow("Genre", out _lblGender);
            AddDetailRow("Date de naissance", out _lblDob);
            AddDetailRow("Âge", out _lblAge);
            AddDetailRow("Pays", out _lblCountry);
            AddDetailRow("Inscrit le", out _lblJoined);
            AddDetailRow("Rôle", out _lblRole);
        }

        private void AddDetailRow(string caption, out Label valueLabel)
        {
            int rowIndex = _detailsLayout.RowCount;
            _detailsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
            _detailsLayout.RowCount++;

            var captionLabel = new Label
            {
                Text = caption + " :",
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.DimGray,
                Padding = new Padding(0, 6, 0, 6)
            };

            valueLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                Padding = new Padding(0, 6, 0, 6),
                Text = "--"
            };

            _detailsLayout.Controls.Add(captionLabel, 0, rowIndex);
            _detailsLayout.Controls.Add(valueLabel, 1, rowIndex);
        }

        private async void LoadProfile()
        {
            if (_httpClient == null)
            {
                return;
            }

            try
            {
                var response = await _httpClient.GetAsync($"https://localhost:5001/api/profile/{_targetUserId}");
                if (!response.IsSuccessStatusCode)
                {
                    PalMessageBox.Show("Impossible de charger le profil.");
                    Close();
                    return;
                }

                _profile = await response.Content.ReadFromJsonAsync<UserProfileDto>() ?? new UserProfileDto();
                PopulateFields();
            }
            catch (Exception ex)
            {
                PalMessageBox.Show($"Erreur: {ex.Message}");
                Close();
            }
        }

        private void PopulateFields()
        {
            if (_profile.ProfilePicture != null && _profile.ProfilePicture.Length > 0)
            {
                using var ms = new MemoryStream(_profile.ProfilePicture);
                _picture.Image = Image.FromStream(ms);
            }
            else
            {
                LoadFallbackPicture();
            }

            string fullName = ($"{_profile.FirstName} {_profile.LastName}").Trim();
            if (string.IsNullOrWhiteSpace(fullName))
            {
                fullName = string.IsNullOrWhiteSpace(_profile.DisplayedName)
                    ? _profile.Username ?? "Utilisateur"
                    : _profile.DisplayedName;
            }

            _lblDisplayName.Text = fullName;
            Text = $"Profil de {fullName}";

            _lblAlias.Text = string.IsNullOrWhiteSpace(_profile.DisplayedName)
                ? "Alias: N/A"
                : $"Alias: {_profile.DisplayedName}";

            _lblFirstName.Text = string.IsNullOrWhiteSpace(_profile.FirstName) ? "N/A" : _profile.FirstName;
            _lblLastName.Text = string.IsNullOrWhiteSpace(_profile.LastName) ? "N/A" : _profile.LastName;
            _lblUsername.Text = string.IsNullOrWhiteSpace(_profile.Username) ? "N/A" : _profile.Username;
            _lblGender.Text = string.IsNullOrWhiteSpace(_profile.Gender) ? "N/A" : _profile.Gender;
            _lblCountry.Text = string.IsNullOrWhiteSpace(_profile.Country) ? "N/A" : _profile.Country;

            if (_profile.CreatedAt != DateTime.MinValue)
            {
                _lblJoined.Text = _profile.CreatedAt.ToString("dd/MM/yyyy");
            }
            else
            {
                _lblJoined.Text = "N/A";
            }

            _lblRole.Text = _profile.IsAdmin ? "Administrateur" : "Utilisateur";

            if (_profile.DateOfBirth != DateTime.MinValue)
            {
                _lblDob.Text = _profile.DateOfBirth.ToString("dd/MM/yyyy");

                int age = DateTime.Today.Year - _profile.DateOfBirth.Year;
                if (_profile.DateOfBirth.Date > DateTime.Today.AddYears(-age))
                {
                    age--;
                }

                _lblAge.Text = age > 0 ? $"{age} ans" : "N/A";
            }
            else
            {
                _lblDob.Text = "N/A";
                _lblAge.Text = "N/A";
            }

            UpdateStatus(_profile.CurrentStatus);
        }

        private void LoadFallbackPicture()
        {
            string genderIcon = "gender/autre.ico";
            if (string.Equals(_profile.Gender, "Homme", StringComparison.OrdinalIgnoreCase))
            {
                genderIcon = "gender/homme.ico";
            }
            else if (string.Equals(_profile.Gender, "Femme", StringComparison.OrdinalIgnoreCase))
            {
                genderIcon = "gender/femme.ico";
            }

            var fallback = ResourceImageStore.LoadImage(genderIcon, new Size(_picture.Width, _picture.Height));
            if (fallback != null)
            {
                _picture.Image?.Dispose();
                _picture.Image = fallback;
            }
        }

        public void UpdateStatus(UserStatus status)
        {
            _profile.CurrentStatus = status;
            var (statusText, statusColor) = GetStatusDisplay(status);
            _lblStatus.Text = $"Statut: {statusText}";
            _lblStatus.ForeColor = statusColor;
        }

        private static (string Text, Color Color) GetStatusDisplay(UserStatus status)
        {
            return status switch
            {
                UserStatus.Online => ("En ligne", Color.Green),
                UserStatus.Offline => ("Hors ligne", Color.Gray),
                UserStatus.Away => ("Absent", Color.SteelBlue),
                UserStatus.Busy => ("Occupé", Color.Red),
                UserStatus.DoNotDisturb => ("Ne pas déranger", Color.MediumVioletRed),
                UserStatus.BRB => ("BRB", Color.DarkOrange),
                _ => ("Inconnu", Color.Gray)
            };
        }
    }
}
