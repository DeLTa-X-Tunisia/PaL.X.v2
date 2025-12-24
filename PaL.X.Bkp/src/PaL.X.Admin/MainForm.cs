using PaL.X.Admin.Properties;
using PaL.X.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PaL.X.Admin
{
    public partial class MainForm : Form
    {
        private readonly string _authToken;
        private readonly UserData _currentUser;
        private readonly HttpClient _httpClient;
        private const string ApiBaseUrl = "https://localhost:5001/api";
        
        private System.Windows.Forms.Timer _refreshTimer;
        
        // Contrôles pour le Chat Public
        private Button btnToggleChat = null!;
        private Label lblChatStatus = null!;
        private bool _isPublicChatEnabled = true;
        
        // ID de connexion stable pour l'admin
        private readonly string _adminConnectionId = Guid.NewGuid().ToString();

        public class ClientInfo
        {
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty; // Added
            public string ConnectionId { get; set; } = string.Empty;
            public DateTime ConnectedAt { get; set; }
        }

        private ImageList _clientImageList;

        public MainForm(string authToken, UserData currentUser)
        {
            InitializeComponent();
            InitializeImageList();
            InitializeChatControls();
            _authToken = authToken;
            _currentUser = currentUser;
            
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
            _httpClient = new HttpClient(handler);
            
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _authToken);
            
            // Configurer le timer de rafraîchissement
            _refreshTimer = new System.Windows.Forms.Timer();
            _refreshTimer.Interval = 3000; // 3 secondes
            _refreshTimer.Tick += RefreshTimer_Tick;
        }

        private void InitializeImageList()
        {
            _clientImageList = new ImageList
            {
                ImageSize = new Size(16, 16),
                ColorDepth = ColorDepth.Depth32Bit
            };

            TryAddClientIcon("User", "status/User_Con.ico");
            TryAddClientIcon("Admin", "status/Admin_Con.ico");
            
            // Assigner l'ImageList à la ListView existante (supposée être lstConnectedClients)
            // Note: lstConnectedClients est initialisé dans InitializeComponent, donc accessible ici
            if (lstConnectedClients != null)
            {
                lstConnectedClients.SmallImageList = _clientImageList;
            }
        }

        private void TryAddClientIcon(string key, string resourceKey)
        {
            if (_clientImageList.Images.ContainsKey(key))
            {
                return;
            }

            switch (Resources.GetObject(resourceKey))
            {
                case Bitmap bmp:
                    _clientImageList.Images.Add(key, new Bitmap(bmp));
                    break;
                case Image img:
                    _clientImageList.Images.Add(key, new Bitmap(img));
                    break;
            }
        }

        private void InitializeChatControls()
        {
            // Label Statut Chat
            lblChatStatus = new Label();
            lblChatStatus.Text = "Chat Public: Activé";
            lblChatStatus.AutoSize = true;
            lblChatStatus.Location = new System.Drawing.Point(20, 180); // Ajuster la position selon votre UI
            lblChatStatus.ForeColor = System.Drawing.Color.Green;
            lblChatStatus.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.Controls.Add(lblChatStatus);

            // Bouton Toggle Chat
            btnToggleChat = new Button();
            btnToggleChat.Text = "Désactiver Chat Public";
            btnToggleChat.Location = new System.Drawing.Point(20, 210);
            btnToggleChat.Size = new System.Drawing.Size(200, 35);
            btnToggleChat.BackColor = System.Drawing.Color.Orange;
            btnToggleChat.ForeColor = System.Drawing.Color.White;
            btnToggleChat.FlatStyle = FlatStyle.Flat;
            btnToggleChat.Click += BtnToggleChat_Click;
            this.Controls.Add(btnToggleChat);
        }

        private async void BtnToggleChat_Click(object? sender, EventArgs e)
        {
            try
            {
                string endpoint = _isPublicChatEnabled ? "chat/stop" : "chat/start";
                var content = new StringContent("", Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{ApiBaseUrl}/admin/{endpoint}", content);
                
                if (response.IsSuccessStatusCode)
                {
                    // Le timer mettra à jour l'UI
                    await LoadServiceStatus();
                }
                else
                {
                    MessageBox.Show("Erreur lors du changement d'état du chat.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            // Afficher les informations de l'administrateur
            lblUserInfo.Text = $"Administrateur: {_currentUser.Username}";
            
            // S'enregistrer comme connecté (pour apparaître dans la liste)
            await RegisterAdminConnection();

            // Configurer le menu contextuel pour la ListView
            var contextMenu = new ContextMenuStrip();
            var disconnectItem = new ToolStripMenuItem("Déconnecter ce client");
            disconnectItem.Click += (s, args) => 
            {
                if (lstConnectedClients.SelectedItems.Count > 0)
                {
                    var item = lstConnectedClients.SelectedItems[0];
                    if (item.Tag is string connectionId)
                    {
                        DisconnectSingleClient(connectionId, item.Text);
                    }
                }
            };
            contextMenu.Items.Add(disconnectItem);
            lstConnectedClients.ContextMenuStrip = contextMenu;

            // Charger l'état initial du service
            await LoadServiceStatus();
            
            // Démarrer le timer de rafraîchissement
            _refreshTimer.Start();
        }

        private async Task RegisterAdminConnection()
        {
            try
            {
                // On s'enregistre avec notre ID stable
                var payload = new { SignalRConnectionId = _adminConnectionId };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await _httpClient.PostAsync($"{ApiBaseUrl}/service/connect", content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'enregistrement de la connexion admin: {ex.Message}");
            }
        }

        private async void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            await LoadServiceStatus();
        }

        private async Task LoadServiceStatus()
        {
            try
            {
                // Récupérer le statut du service
                var response = await _httpClient.GetAsync($"{ApiBaseUrl}/admin/status");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseJson);
                    
                    // Essayer avec la casse CamelCase (API par défaut) ou PascalCase (au cas où)
                    JsonElement statusElement;
                    if (doc.RootElement.TryGetProperty("status", out statusElement) || 
                        doc.RootElement.TryGetProperty("Status", out statusElement))
                    {
                        // Mettre à jour l'état du service
                        if (statusElement.TryGetProperty("isRunning", out var isRunningElement) ||
                            statusElement.TryGetProperty("IsRunning", out isRunningElement))
                        {
                            bool isRunning = isRunningElement.GetBoolean();
                            
                            // Récupérer l'état du chat public
                            bool isPublicChatEnabled = true;
                            if (statusElement.TryGetProperty("isPublicChatEnabled", out var chatEnabledElement) ||
                                statusElement.TryGetProperty("IsPublicChatEnabled", out chatEnabledElement))
                            {
                                isPublicChatEnabled = chatEnabledElement.GetBoolean();
                            }

                            UpdateServiceUI(isRunning, isPublicChatEnabled);
                        }
                        
                        // Mettre à jour la liste des clients
                        if (statusElement.TryGetProperty("clients", out var clientsElement) ||
                            statusElement.TryGetProperty("Clients", out clientsElement))
                        {
                            UpdateClientsList(clientsElement);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du chargement du statut: {ex.Message}");
                // Si l'API est injoignable, considérer le service comme arrêté pour éviter un statut bloqué.
                UpdateServiceUI(false, false);
            }
        }

        private void UpdateServiceUI(bool isRunning, bool isPublicChatEnabled)
        {
            _isPublicChatEnabled = isPublicChatEnabled;

            // Mise à jour UI Service
            if (isRunning)
            {
                lblServiceStatus.Text = "Statut: En cours d'exécution";
                lblServiceStatus.ForeColor = System.Drawing.Color.Green;
                
                btnStartService.Visible = false;
                btnStopService.Visible = true;
                btnStopService.Enabled = true;
                btnStopService.Location = btnStartService.Location;

                // Activer les contrôles de chat seulement si le service tourne
                btnToggleChat.Enabled = true;
            }
            else
            {
                lblServiceStatus.Text = "Statut: Arrêté";
                lblServiceStatus.ForeColor = System.Drawing.Color.Red;
                
                btnStartService.Visible = true;
                btnStartService.Enabled = true;
                btnStopService.Visible = false;

                // Désactiver les contrôles de chat si le service est arrêté
                btnToggleChat.Enabled = false;
            }

            // Mise à jour UI Chat
            if (isPublicChatEnabled)
            {
                lblChatStatus.Text = "Chat Public: Activé";
                lblChatStatus.ForeColor = System.Drawing.Color.Green;
                btnToggleChat.Text = "Désactiver Chat Public";
                btnToggleChat.BackColor = System.Drawing.Color.Orange; // Warning color
            }
            else
            {
                lblChatStatus.Text = "Chat Public: Désactivé (Admin Only)";
                lblChatStatus.ForeColor = System.Drawing.Color.Red;
                btnToggleChat.Text = "Activer Chat Public";
                btnToggleChat.BackColor = System.Drawing.Color.Green; // Go color
            }
        }

        private void UpdateClientsList(JsonElement clientsElement)
        {
            // Sauvegarder la sélection actuelle
            string? selectedConnectionId = null;
            if (lstConnectedClients.SelectedItems.Count > 0)
            {
                selectedConnectionId = lstConnectedClients.SelectedItems[0].Tag as string;
            }

            lstConnectedClients.BeginUpdate();
            lstConnectedClients.Items.Clear();
            
            if (clientsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var client in clientsElement.EnumerateArray())
                {
                    string username = "Inconnu";
                    if (client.TryGetProperty("username", out var usernameProp) || 
                        client.TryGetProperty("Username", out usernameProp))
                    {
                        username = usernameProp.GetString() ?? "Inconnu";
                    }

                    string email = "";
                    if (client.TryGetProperty("email", out var emailProp) || 
                        client.TryGetProperty("Email", out emailProp))
                    {
                        email = emailProp.GetString() ?? "";
                    }

                    string role = "User";
                    if (client.TryGetProperty("role", out var roleProp) || 
                        client.TryGetProperty("Role", out roleProp))
                    {
                        role = roleProp.GetString() ?? "User";
                    }

                    string connectionId = "";
                    if (client.TryGetProperty("connectionId", out var connIdProp) || 
                        client.TryGetProperty("ConnectionId", out connIdProp))
                    {
                        connectionId = connIdProp.GetString() ?? "";
                    }

                    DateTime connectedAt = DateTime.MinValue;
                    if (client.TryGetProperty("connectedAt", out var dateProp) || 
                        client.TryGetProperty("ConnectedAt", out dateProp))
                    {
                        connectedAt = dateProp.GetDateTime();
                    }

                    var item = new ListViewItem(username);
                    
                    // Icone selon le rôle
                    if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                    {
                        item.ImageKey = "Admin";
                    }
                    else
                    {
                        item.ImageKey = "User";
                    }

                    item.SubItems.Add(email);
                    item.SubItems.Add(role);
                    item.SubItems.Add(connectedAt.ToLocalTime().ToString("HH:mm:ss"));
                    item.SubItems.Add("Connecté");
                    item.Tag = connectionId; // Store ID in Tag if needed

                    lstConnectedClients.Items.Add(item);

                    // Restaurer la sélection
                    if (connectionId == selectedConnectionId)
                    {
                        item.Selected = true;
                        item.EnsureVisible();
                    }
                }
            }
            
            lstConnectedClients.EndUpdate();
            lblConnectedClients.Text = $"Clients connectés : {lstConnectedClients.Items.Count}";
        }

        private async void btnStartService_Click(object sender, EventArgs e)
        {
            try
            {
                var response = await _httpClient.PostAsync($"{ApiBaseUrl}/admin/start", null);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseJson);
                    
                    if (doc.RootElement.TryGetProperty("Success", out var successElement) && 
                        successElement.GetBoolean())
                    {
                        MessageBox.Show("Service démarré avec succès. Les clients peuvent maintenant se connecter.",
                            "Service démarré", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        
                        // Ré-enregistrer l'admin maintenant que le service tourne
                        await RegisterAdminConnection();
                        
                        await LoadServiceStatus(); // Rafraîchir l'état
                    }
                }
                else
                {
                    MessageBox.Show("Erreur lors du démarrage du service.",
                        "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnStopService_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Êtes-vous sûr de vouloir arrêter le service? Tous les clients seront déconnectés.",
                "Confirmation d'arrêt", 
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    var response = await _httpClient.PostAsync($"{ApiBaseUrl}/admin/stop", null);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var responseJson = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(responseJson);

                        bool? isRunningFromResponse = null;
                        bool? isPublicChatFromResponse = null;

                        if (doc.RootElement.TryGetProperty("Status", out var statusElement) ||
                            doc.RootElement.TryGetProperty("status", out statusElement))
                        {
                            if (statusElement.TryGetProperty("IsRunning", out var runningElement) ||
                                statusElement.TryGetProperty("isRunning", out runningElement))
                            {
                                isRunningFromResponse = runningElement.GetBoolean();
                            }

                            if (statusElement.TryGetProperty("IsPublicChatEnabled", out var chatElement) ||
                                statusElement.TryGetProperty("isPublicChatEnabled", out chatElement))
                            {
                                isPublicChatFromResponse = chatElement.GetBoolean();
                            }
                        }

                        UpdateServiceUI(isRunningFromResponse ?? false, isPublicChatFromResponse ?? false);
                        lblServiceStatus.Refresh();

                        if (doc.RootElement.TryGetProperty("Success", out var successElement) &&
                            successElement.GetBoolean())
                        {
                            await Task.Delay(100);
                            MessageBox.Show("Service arrêté. Tous les clients ont été déconnectés.",
                                "Service arrêté", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    else
                    {
                        UpdateServiceUI(false, false);
                        MessageBox.Show("Erreur lors de l'arrêt du service.",
                            "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    UpdateServiceUI(false, false);
                    MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    BackendController.Stop();
                }

                if (!IsDisposed)
                {
                    await LoadServiceStatus();
                }
            }
        }

        private async void btnDisconnectAll_Click(object sender, EventArgs e)
        {
            if (lstConnectedClients.Items.Count == 0)
            {
                MessageBox.Show("Aucun client n'est connecté.", "Information", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir déconnecter tous les {lstConnectedClients.Items.Count} client(s)?",
                "Confirmation de déconnexion", 
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    var response = await _httpClient.PostAsync($"{ApiBaseUrl}/admin/disconnect-all", null);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Tous les clients ont été déconnectés.", 
                            "Déconnexion", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        
                        await LoadServiceStatus(); // Rafraîchir l'état
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void DisconnectSingleClient(string connectionId, string username)
        {
            var result = MessageBox.Show(
                $"Voulez-vous vraiment déconnecter l'utilisateur '{username}' ?",
                "Confirmation", 
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    var response = await _httpClient.PostAsync($"{ApiBaseUrl}/admin/disconnect-client/{connectionId}", null);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        await LoadServiceStatus(); // Rafraîchir la liste
                    }
                    else
                    {
                        MessageBox.Show("Erreur lors de la déconnexion du client.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public bool IsLogout { get; private set; } = false;

        private void btnLogout_Click(object sender, EventArgs e)
        {
            IsLogout = true;
            _refreshTimer.Stop();
            this.Close();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _refreshTimer.Stop();
            if (!IsLogout)
            {
                BackendController.Stop();
            }
        }
    }
}