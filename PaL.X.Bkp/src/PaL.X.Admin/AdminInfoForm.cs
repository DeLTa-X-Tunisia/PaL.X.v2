using PaL.X.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PaL.X.Admin
{
    public partial class AdminInfoForm : Form
    {
        private readonly string _authToken;
        private readonly UserData _currentUser;
        private readonly HttpClient _httpClient;
        private const string ApiBaseUrl = "https://localhost:5001/api";
        private byte[]? _profilePictureBytes;

        // Constructeur pour le Designer
        public AdminInfoForm()
        {
            InitializeComponent();
            _authToken = string.Empty;
            _currentUser = new UserData();
            _httpClient = new HttpClient();
        }

        public AdminInfoForm(string authToken, UserData currentUser)
        {
            InitializeComponent();
            if (DesignMode) return;

            _authToken = authToken;
            _currentUser = currentUser;

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);

            LoadProfile();
        }

        private async void LoadProfile()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{ApiBaseUrl}/profile");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var profile = JsonSerializer.Deserialize<UserProfileDto>(json, options);

                    if (profile != null && profile.IsComplete)
                    {
                        // Populate fields if they exist (resuming)
                        txtFirstName.Text = profile.FirstName;
                        txtLastName.Text = profile.LastName;
                        txtDisplayedName.Text = profile.DisplayedName;
                        dtpDateOfBirth.Value = profile.DateOfBirth == DateTime.MinValue ? DateTime.Now : profile.DateOfBirth;
                        cmbGender.SelectedItem = profile.Gender;
                        txtCountry.Text = profile.Country;
                        
                        if (profile.ProfilePicture != null && profile.ProfilePicture.Length > 0)
                        {
                            _profilePictureBytes = profile.ProfilePicture;
                            using (var ms = new MemoryStream(_profilePictureBytes))
                            {
                                pbProfilePicture.Image = Image.FromStream(ms);
                            }
                        }
                    }
                }
                UpdateProgress();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement du profil: {ex.Message}");
            }
        }

        private void Input_Changed(object sender, EventArgs e)
        {
            UpdateProgress();
        }

        private void UpdateProgress()
        {
            var missingFields = new List<string>();
            int totalFields = 6;
            int completedFields = 0;

            if (!string.IsNullOrWhiteSpace(txtFirstName.Text)) completedFields++; else missingFields.Add("Prénom");
            if (!string.IsNullOrWhiteSpace(txtLastName.Text)) completedFields++; else missingFields.Add("Nom");
            if (!string.IsNullOrWhiteSpace(txtDisplayedName.Text)) completedFields++; else missingFields.Add("Pseudo");
            if (dtpDateOfBirth.Value < DateTime.Today) completedFields++; else missingFields.Add("Date de naissance"); // Simple check
            if (cmbGender.SelectedItem != null) completedFields++; else missingFields.Add("Genre");
            if (!string.IsNullOrWhiteSpace(txtCountry.Text)) completedFields++; else missingFields.Add("Pays");

            int percentage = (int)((double)completedFields / totalFields * 100);
            lblProgress.Text = $"Progression: {percentage}%";
            
            if (missingFields.Count > 0)
            {
                lblChecklist.Text = "Champs manquants:\n- " + string.Join("\n- ", missingFields);
                lblChecklist.ForeColor = Color.Red;
                btnSave.Enabled = false;
            }
            else
            {
                lblChecklist.Text = "Profil complet !";
                lblChecklist.ForeColor = Color.Green;
                btnSave.Enabled = true;
            }
        }

        private void btnBrowsePhoto_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var bytes = File.ReadAllBytes(ofd.FileName);
                        // Simple size check (e.g., 2MB)
                        if (bytes.Length > 2 * 1024 * 1024)
                        {
                            MessageBox.Show("L'image est trop volumineuse (max 2MB).");
                            return;
                        }

                        _profilePictureBytes = bytes;
                        using (var ms = new MemoryStream(_profilePictureBytes))
                        {
                            pbProfilePicture.Image = Image.FromStream(ms);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erreur lors du chargement de l'image: {ex.Message}");
                    }
                }
            }
        }

        private async void btnSave_Click(object sender, EventArgs e)
        {
            var profile = new UpdateProfileDto
            {
                FirstName = txtFirstName.Text,
                LastName = txtLastName.Text,
                DisplayedName = txtDisplayedName.Text,
                DateOfBirth = dtpDateOfBirth.Value.ToUniversalTime(),
                Gender = cmbGender.SelectedItem?.ToString() ?? "",
                Country = txtCountry.Text,
                ProfilePicture = _profilePictureBytes
            };

            try
            {
                var json = JsonSerializer.Serialize(profile);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{ApiBaseUrl}/profile", content);
                
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Profil enregistré avec succès !", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Erreur lors de l'enregistrement du profil: {response.StatusCode}\n{errorContent}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}");
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            if (Application.OpenForms.Count == 0) Application.Exit();
        }
    }
}
