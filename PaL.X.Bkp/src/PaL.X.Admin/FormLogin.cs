using PaL.X.Shared.DTOs;
using PaL.X.Shared.Models;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace PaL.X.Admin
{
    public partial class FormLogin : Form
    {
        private readonly HttpClient _httpClient;
        private const string ApiBaseUrl = "https://localhost:5001/api";
        
        // Nouveaux contrôles pour la gestion du backend
        private Button btnToggleBackend = null!;
        private Label lblBackendStatus = null!;
        private System.Windows.Forms.Timer timerBackendCheck = null!;
        private bool _isBackendRunning = false;

        public string AuthToken { get; private set; } = string.Empty;
        public UserData? CurrentUser { get; private set; }

        public FormLogin()
        {
            InitializeComponent();
            InitializeBackendControls();

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
            _httpClient = new HttpClient(handler);
            lblStatus.Text = string.Empty;
            
            // Désactiver le login au démarrage
            btnLogin.Enabled = false;
            
            // Démarrer le timer de vérification
            timerBackendCheck.Start();
        }

        private void InitializeBackendControls()
        {
            // Ajuster la taille du formulaire pour faire de la place sous le TabControl
            // TabControl va de Y=40 à Y=290 (40+250)
            this.Height = 400; 

            // Bouton Start/Stop Backend
            btnToggleBackend = new Button();
            btnToggleBackend.Text = "Démarrer Backend";
            btnToggleBackend.Location = new System.Drawing.Point(12, 310); // Y=310 est bien sous le TabControl (290)
            btnToggleBackend.Size = new System.Drawing.Size(120, 30);
            btnToggleBackend.Click += BtnToggleBackend_Click;
            this.Controls.Add(btnToggleBackend);
            // S'assurer que le bouton est au premier plan
            btnToggleBackend.BringToFront();

            // Label Statut Backend
            lblBackendStatus = new Label();
            lblBackendStatus.Text = "Backend arrêté";
            lblBackendStatus.AutoSize = true;
            lblBackendStatus.Location = new System.Drawing.Point(140, 318);
            lblBackendStatus.ForeColor = System.Drawing.Color.Red;
            this.Controls.Add(lblBackendStatus);
            lblBackendStatus.BringToFront();

            // Timer
            timerBackendCheck = new System.Windows.Forms.Timer();
            timerBackendCheck.Interval = 2000; // Vérifier toutes les 2 secondes
            timerBackendCheck.Tick += TimerBackendCheck_Tick;
        }

        private void BtnToggleBackend_Click(object? sender, EventArgs e)
        {
            if (_isBackendRunning)
            {
                BackendController.Stop();
                btnToggleBackend.Text = "Démarrer Backend";
                lblBackendStatus.Text = "Arrêt en cours...";
                lblBackendStatus.ForeColor = System.Drawing.Color.Orange;
                btnLogin.Enabled = false;
                _isBackendRunning = false;
            }
            else
            {
                BackendController.Start();
                btnToggleBackend.Text = "Démarrer...";
                btnToggleBackend.Enabled = false; // Désactiver temporairement
                lblBackendStatus.Text = "Démarrage...";
                lblBackendStatus.ForeColor = System.Drawing.Color.Orange;
            }
        }

        private async void TimerBackendCheck_Tick(object? sender, EventArgs e)
        {
            try
            {
                // Essayer de contacter l'API (endpoint public)
                var response = await _httpClient.GetAsync($"{ApiBaseUrl}/service/check");
                
                if (response.IsSuccessStatusCode)
                {
                    if (!_isBackendRunning)
                    {
                        _isBackendRunning = true;
                        btnToggleBackend.Text = "Arrêter Backend";
                        btnToggleBackend.Enabled = true;
                        lblBackendStatus.Text = "Backend démarré";
                        lblBackendStatus.ForeColor = System.Drawing.Color.Green;
                        btnLogin.Enabled = true;
                    }
                }
                else
                {
                    HandleBackendDown();
                }
            }
            catch
            {
                HandleBackendDown();
            }
        }

        private void HandleBackendDown()
        {
            // If it was running, mark it down.
            // If we're in a "starting" state (button disabled), also reset UI so the user isn't stuck.
            if (_isBackendRunning || (btnToggleBackend != null && btnToggleBackend.Enabled == false))
            {
                _isBackendRunning = false;
                btnToggleBackend.Text = "Démarrer Backend";
                btnToggleBackend.Enabled = true;
                lblBackendStatus.Text = "Backend arrêté";
                lblBackendStatus.ForeColor = System.Drawing.Color.Red;
                btnLogin.Enabled = false;
            }
        }

        // S'assurer que le backend est arrêté si on ferme l'application complètement depuis le Login
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            // Si on ferme le formulaire et que ce n'est pas pour se connecter (DialogResult != OK)
            if (this.DialogResult != DialogResult.OK)
            {
                BackendController.Stop();
            }
        }


        private async void btnLogin_Click(object sender, EventArgs e)
        {
            try
            {
                var loginRequest = new LoginRequest
                {
                    Username = txtLoginUsername.Text,
                    Password = txtLoginPassword.Text
                };

                var json = JsonSerializer.Serialize(loginRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{ApiBaseUrl}/auth/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseJson, options);

                    if (loginResponse?.Success == true)
                    {
                        // Vérifier que l'utilisateur est un admin
                        if (loginResponse.User == null || !loginResponse.User.IsAdmin)
                        {
                            lblStatus.Text = "Accès refusé: Cet utilisateur n'est pas administrateur";
                            lblStatus.ForeColor = System.Drawing.Color.Red;
                            return;
                        }

                        AuthToken = loginResponse.Token;
                        CurrentUser = loginResponse.User;
                        
                        lblStatus.Text = "Connexion admin réussie!";
                        lblStatus.ForeColor = System.Drawing.Color.Green;
                        
                        // Fermer le formulaire de login et ouvrir la MainForm Admin
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    else
                    {
                        lblStatus.Text = loginResponse?.Message ?? "Échec de la connexion";
                        lblStatus.ForeColor = System.Drawing.Color.Red;
                    }
                }
                else
                {
                    // Tenter de lire le message d'erreur du serveur
                    string errorMessage = "Erreur de connexion au serveur";
                    try
                    {
                        var errorJson = await response.Content.ReadAsStringAsync();
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var errorResponse = JsonSerializer.Deserialize<LoginResponse>(errorJson, options);
                        if (!string.IsNullOrEmpty(errorResponse?.Message))
                        {
                            errorMessage = errorResponse.Message;
                        }
                    }
                    catch
                    {
                        // Ignorer si le JSON est invalide
                        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            errorMessage = "Nom d'utilisateur ou mot de passe incorrect";
                        }
                        else
                        {
                            // Afficher le code d'erreur et le contenu brut pour le débogage
                            string rawContent = "";
                            try { rawContent = await response.Content.ReadAsStringAsync(); } catch { }
                            
                            // Limiter la longueur du contenu brut pour l'affichage
                            if (rawContent.Length > 100) rawContent = rawContent.Substring(0, 100) + "...";
                            
                            errorMessage = $"Erreur serveur ({response.StatusCode}): {rawContent}";
                        }
                    }

                    lblStatus.Text = errorMessage;
                    lblStatus.ForeColor = System.Drawing.Color.Red;
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Erreur: {ex.Message}";
                lblStatus.ForeColor = System.Drawing.Color.Red;
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
                    ConfirmPassword = txtRegisterConfirmPassword.Text,
                    IsAdmin = true
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
                        lblStatus.Text = "Inscription admin réussie! Vous pouvez maintenant vous connecter.";
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