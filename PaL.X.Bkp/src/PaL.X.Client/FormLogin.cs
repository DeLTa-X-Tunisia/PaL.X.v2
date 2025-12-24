using PaL.X.Client.Services;
using PaL.X.Shared.DTOs;
using PaL.X.Shared.Models;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace PaL.X.Client
{
    public partial class FormLogin : Form
    {
        private readonly HttpClient _httpClient;
        private const string ApiBaseUrl = "https://localhost:5001/api";
        private CheckBox chkInvisibleLogin = null!;

        public string AuthToken { get; private set; } = string.Empty;
        public UserData? CurrentUser { get; private set; }
        public bool ConnectOffline => chkInvisibleLogin?.Checked ?? false;

        public FormLogin()
        {
            InitializeComponent();
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
            _httpClient = new HttpClient(handler);
            lblStatus.Text = string.Empty;
            
            // Add invisible login checkbox programmatically
            InitializeInvisibleLoginCheckbox();
        }

        private void InitializeInvisibleLoginCheckbox()
        {
            chkInvisibleLogin = new CheckBox();
            chkInvisibleLogin.Text = "Se connecter en mode Hors Ligne";
            chkInvisibleLogin.AutoSize = true;
            chkInvisibleLogin.Font = new System.Drawing.Font("Segoe UI", 9F);
            
            // Position it below the password field (you may need to adjust coordinates based on your form layout)
            chkInvisibleLogin.Location = new System.Drawing.Point(txtLoginPassword.Location.X, txtLoginPassword.Location.Y + txtLoginPassword.Height + 10);
            
            // Add to the login tab page
            tabLogin.Controls.Add(chkInvisibleLogin);
            chkInvisibleLogin.BringToFront();
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            // Validation des entrées
            if (string.IsNullOrWhiteSpace(txtLoginUsername.Text) || 
                string.IsNullOrWhiteSpace(txtLoginPassword.Text))
            {
                lblStatus.Text = "Veuillez remplir tous les champs";
                lblStatus.ForeColor = System.Drawing.Color.Red;
                return;
            }

            btnLogin.Enabled = false;
            lblStatus.Text = "Connexion en cours...";
            lblStatus.ForeColor = System.Drawing.Color.Blue;

            try
            {
                var loginRequest = new LoginRequest
                {
                    Username = txtLoginUsername.Text.Trim(),
                    Password = txtLoginPassword.Text,
                    ConnectOffline = chkInvisibleLogin.Checked,
                    DeviceSerial = DeviceIdentityService.GetDeviceIdentity()
                };

                var json = JsonSerializer.Serialize(loginRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Timeout de 10 secondes
                var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
                
                var response = await _httpClient.PostAsync($"{ApiBaseUrl}/auth/login", content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseJson, options);

                    if (loginResponse?.Success == true)
                    {
                        // Vérifier si le service est disponible avant de fermer
                        lblStatus.Text = "Vérification du service...";
                        var serviceResponse = await _httpClient.GetAsync($"{ApiBaseUrl}/service/check", cancellationToken);
                        
                        bool serviceAvailable = false;
                        if (serviceResponse.IsSuccessStatusCode)
                        {
                            var serviceJson = await serviceResponse.Content.ReadAsStringAsync();
                            using var doc = JsonDocument.Parse(serviceJson);
                            if (doc.RootElement.TryGetProperty("serviceAvailable", out var availProp) || 
                                doc.RootElement.TryGetProperty("ServiceAvailable", out availProp))
                            {
                                serviceAvailable = availProp.GetBoolean();
                            }
                        }

                        if (serviceAvailable)
                        {
                            AuthToken = loginResponse.Token;
                            CurrentUser = loginResponse.User;
                            
                            lblStatus.Text = "Connexion réussie!";
                            lblStatus.ForeColor = System.Drawing.Color.Green;
                            
                            // Petite pause pour montrer le message
                            await Task.Delay(500);
                            
                            this.DialogResult = DialogResult.OK;
                            this.Close();
                        }
                        else
                        {
                            lblStatus.Text = "Le service est actuellement arrêté par l'administrateur.";
                            lblStatus.ForeColor = System.Drawing.Color.Red;
                            // On ne ferme pas la fenêtre, l'utilisateur doit attendre
                        }
                    }
                    else
                    {
                        lblStatus.Text = loginResponse?.Message ?? "Échec de la connexion";
                        lblStatus.ForeColor = System.Drawing.Color.Red;
                    }
                }
                else
                {
                    lblStatus.Text = $"Erreur serveur: {response.StatusCode}";
                    lblStatus.ForeColor = System.Drawing.Color.Red;
                }
            }
            catch (TaskCanceledException)
            {
                lblStatus.Text = "Timeout: Le serveur ne répond pas";
                lblStatus.ForeColor = System.Drawing.Color.Red;
            }
            catch (HttpRequestException ex)
            {
                lblStatus.Text = $"Impossible de joindre le serveur: {ex.Message}";
                lblStatus.ForeColor = System.Drawing.Color.Red;
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Erreur: {ex.Message}";
                lblStatus.ForeColor = System.Drawing.Color.Red;
            }
            finally
            {
                btnLogin.Enabled = true;
            }
        }

        private async void btnRegister_Click(object sender, EventArgs e)
        {
            if (txtRegisterPassword.Text != txtRegisterConfirmPassword.Text)
            {
                lblStatus.Text = "Les mots de passe ne correspondent pas";
                lblStatus.ForeColor = System.Drawing.Color.Red;
                return;
            }

            try
            {
                var registerRequest = new RegisterRequest
                {
                    Username = txtRegisterUsername.Text,
                    Email = txtRegisterEmail.Text,
                    Password = txtRegisterPassword.Text,
                    ConfirmPassword = txtRegisterConfirmPassword.Text
                };

                var json = JsonSerializer.Serialize(registerRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{ApiBaseUrl}/auth/register", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var registerResponse = JsonSerializer.Deserialize<LoginResponse>(responseJson, options);

                    if (registerResponse?.Success == true)
                    {
                        lblStatus.Text = "Inscription réussie! Vous pouvez maintenant vous connecter.";
                        lblStatus.ForeColor = System.Drawing.Color.Green;
                        
                        // Basculer vers l'onglet de connexion
                        tabControl1.SelectedTab = tabLogin;
                        
                        // Pré-remplir le nom d'utilisateur
                        txtLoginUsername.Text = txtRegisterUsername.Text;
                        txtLoginPassword.Clear();
                    }
                    else
                    {
                        lblStatus.Text = registerResponse?.Message ?? "Échec de l'inscription";
                        lblStatus.ForeColor = System.Drawing.Color.Red;
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    lblStatus.Text = $"Erreur lors de l'inscription: {error}";
                    lblStatus.ForeColor = System.Drawing.Color.Red;
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Erreur: {ex.Message}";
                lblStatus.ForeColor = System.Drawing.Color.Red;
            }
        }
    }
}