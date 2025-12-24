using Microsoft.Web.WebView2.Core;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Json;
using PaL.X.Shared;
using PaL.X.Data;
using PaL.X.Core;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;

namespace PaL.X.App;

public partial class Form1 : Form
{
    private Microsoft.Web.WebView2.WinForms.WebView2 webView;
    private const string AssetsPath = @"C:\Users\azizi\OneDrive\Desktop\PaL.X\PaL.X.v2.assets";
    private const string ApiUrl = "http://localhost:5030";
    private readonly HttpClient _httpClient;
    private PaL.X.Shared.Models.User _currentUser;
    private System.Windows.Forms.Timer _heartbeatTimer;

    public Form1()
    {
        InitializeComponent();
        _httpClient = new HttpClient { BaseAddress = new Uri(ApiUrl) };
        InitializeWebView();
        InitializeHeartbeat();
        
        // Borderless and Compact
        this.FormBorderStyle = FormBorderStyle.None;
        this.Size = new Size(400, 550);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MaximizeBox = false;
    }

    private async void InitializeWebView()
    {
        webView = new Microsoft.Web.WebView2.WinForms.WebView2();
        webView.Dock = DockStyle.Fill;
        this.Controls.Add(webView);

        await webView.EnsureCoreWebView2Async();

        // Map wwwroot
        string wwwroot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
        webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "app.pal.x", 
            wwwroot, 
            CoreWebView2HostResourceAccessKind.Allow
        );

        // Map Smileys
        string smileysPath = Path.Combine(AssetsPath, "Smiley");
        if (Directory.Exists(smileysPath))
        {
            webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "smileys.pal.x", 
                smileysPath, 
                CoreWebView2HostResourceAccessKind.Allow
            );
        }

        webView.CoreWebView2.WebMessageReceived += WebView_WebMessageReceived;
        webView.NavigationCompleted += WebView_NavigationCompleted;
        
        // Start with Login
        webView.Source = new Uri("https://app.pal.x/login.html");
    }

    private async void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
             await webView.ExecuteScriptAsync("if(typeof setTitle === 'function') setTitle('PaL.X Client');");
        }
    }

    private async void WebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            string json = e.WebMessageAsJson;
            var message = JsonSerializer.Deserialize<JsonElement>(json);
            
            if (message.TryGetProperty("type", out var typeProp))
            {
                string type = typeProp.GetString() ?? "";
                
                switch (type)
                {
                    case "drag":
                        ReleaseCapture();
                        SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                        break;
                    case "login":
                         if (message.TryGetProperty("payload", out var loginPayload))
                         {
                             string u = loginPayload.GetProperty("username").GetString() ?? "";
                             string p = loginPayload.GetProperty("password").GetString() ?? "";
                             await HandleLogin(u, p);
                         }
                         break;
                    case "register":
                         if (message.TryGetProperty("payload", out var regPayload))
                         {
                             string u = regPayload.GetProperty("username").GetString() ?? "";
                             string p = regPayload.GetProperty("password").GetString() ?? "";
                             await HandleRegister(u, p);
                         }
                         break;
                    case "cancel":
                        Application.Exit();
                        break;
                    case "mainReady":
                        if (_currentUser != null)
                        {
                            string avatarUrl = null;
                            string fullName = _currentUser.Username;
                            try 
                            {
                                var profileResponse = await _httpClient.GetAsync($"/api/users/{_currentUser.Username}/profile");
                                if (profileResponse.IsSuccessStatusCode)
                                {
                                    var profile = await profileResponse.Content.ReadFromJsonAsync<PaL.X.Shared.Models.UserProfile>();
                                    avatarUrl = profile?.ProfilePictureUrl;
                                    if (profile != null && !string.IsNullOrEmpty(profile.FirstName) && !string.IsNullOrEmpty(profile.LastName))
                                    {
                                        fullName = $"{profile.LastName} {profile.FirstName}";
                                    }
                                }
                            }
                            catch {}

                            if (!string.IsNullOrEmpty(avatarUrl) && !avatarUrl.StartsWith("http"))
                            {
                                 avatarUrl = ApiUrl + avatarUrl;
                            }
                            PostMessage(new { type = "init", user = new { id = _currentUser.Id, username = _currentUser.Username, displayName = fullName, isAdmin = _currentUser.IsAdmin, avatarUrl = avatarUrl }, apiUrl = ApiUrl });
                        }
                        break;
                    case "logout":
                        _currentUser = null;
                        webView.CoreWebView2.Navigate("https://app.pal.x/login.html");
                        this.Invoke(() => {
                            this.Size = new Size(400, 550);
                            this.CenterToScreen();
                        });
                        break;
                    case "profileReady":
                        if (_currentUser != null)
                        {
                            string avatarUrl = null;
                            try
                            {
                                var profileResponse = await _httpClient.GetAsync($"/api/users/{_currentUser.Username}/profile");
                                if (profileResponse.IsSuccessStatusCode)
                                {
                                    var profile = await profileResponse.Content.ReadFromJsonAsync<PaL.X.Shared.Models.UserProfile>();
                                    avatarUrl = profile?.ProfilePictureUrl;
                                }
                            }
                            catch {}

                            if (!string.IsNullOrEmpty(avatarUrl) && !avatarUrl.StartsWith("http"))
                            {
                                 avatarUrl = ApiUrl + avatarUrl;
                            }
                            PostMessage(new { type = "initProfile", username = _currentUser.Username, avatarUrl = avatarUrl });
                        }
                        break;
                    case "saveProfile":
                        if (message.TryGetProperty("payload", out var profilePayload))
                        {
                            await HandleSaveProfile(profilePayload);
                        }
                        break;
                    case "cancelProfile":
                        _currentUser = null;
                        webView.CoreWebView2.Navigate("https://app.pal.x/login.html");
                        this.Invoke(() => {
                            this.Size = new Size(400, 550);
                            this.CenterToScreen();
                        });
                        break;
                    case "ready":
                        if (_currentUser != null) SendInitData();
                        break;
                    case "requestSmileys":
                        SendSmileys();
                        break;
                    case "sendMessage":
                        if (message.TryGetProperty("content", out var contentProp))
                        {
                            HandleSendMessage(contentProp.GetString() ?? "");
                        }
                        break;
                    case "sendImage":
                        if (message.TryGetProperty("payload", out var payloadProp))
                        {
                            HandleSendImage(payloadProp);
                        }
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
        }
    }

    private async Task HandleLogin(string username, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", new { Username = username, Password = password });
            if (response.IsSuccessStatusCode)
            {
                _currentUser = await response.Content.ReadFromJsonAsync<PaL.X.Shared.Models.User>();
                
                // Check Profile
                var profileResponse = await _httpClient.GetAsync($"/api/users/{username}/profile");
                if (profileResponse.IsSuccessStatusCode)
                {
                    var profile = await profileResponse.Content.ReadFromJsonAsync<PaL.X.Shared.Models.UserProfile>();
                    if (profile != null && profile.IsComplete())
                    {
                        NavigateToMain();
                        return;
                    }
                }
                NavigateToProfile();
            }
            else
            {
                PostMessage(new { type = "loginError", message = "Identifiants incorrects" });
            }
        }
        catch (Exception ex)
        {
            PostMessage(new { type = "loginError", message = "Erreur de connexion au serveur" });
        }
    }

    private void NavigateToMain()
    {
        this.Invoke(() => {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(380, 750);
            this.CenterToScreen();
        });
        webView.CoreWebView2.Navigate("https://app.pal.x/main.html");
        
        // Wait for navigation to complete then send init data
        webView.NavigationCompleted += (s, e) => {
            if (e.IsSuccess && webView.Source.ToString().EndsWith("main.html"))
            {
                PostMessage(new { 
                    type = "init", 
                    user = _currentUser,
                    apiUrl = ApiUrl
                });
            }
        };
    }

    private void NavigateToProfile()
    {
        this.Invoke(() => {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(1000, 780); // Updated dimensions
            this.CenterToScreen();
        });
        webView.CoreWebView2.Navigate("https://app.pal.x/user_profile.html");
    }

    private async Task HandleSaveProfile(JsonElement payload)
    {
        try
        {
            var profile = new PaL.X.Shared.Models.UserProfile
            {
                UserId = _currentUser.Id,
                Email = payload.GetProperty("email").GetString() ?? "",
                FirstName = payload.GetProperty("firstName").GetString() ?? "",
                LastName = payload.GetProperty("lastName").GetString() ?? "",
                Gender = payload.GetProperty("gender").GetString() ?? "",
                BirthDate = DateTime.SpecifyKind(DateTime.Parse(payload.GetProperty("birthDate").GetString() ?? DateTime.Now.ToString()), DateTimeKind.Utc),
                Country = payload.GetProperty("country").GetString() ?? "",
                PhoneNumber = payload.TryGetProperty("phoneNumber", out var phoneProp) ? phoneProp.GetString() : null,
                
                EmailVisibility = (PaL.X.Shared.Models.VisibilityLevel)payload.GetProperty("emailVisibility").GetInt32(),
                NameVisibility = (PaL.X.Shared.Models.VisibilityLevel)payload.GetProperty("nameVisibility").GetInt32(),
                GenderVisibility = (PaL.X.Shared.Models.VisibilityLevel)payload.GetProperty("genderVisibility").GetInt32(),
                BirthDateVisibility = (PaL.X.Shared.Models.VisibilityLevel)payload.GetProperty("birthDateVisibility").GetInt32(),
                CountryVisibility = (PaL.X.Shared.Models.VisibilityLevel)payload.GetProperty("countryVisibility").GetInt32(),
                PhoneNumberVisibility = payload.TryGetProperty("phoneNumberVisibility", out var phoneVisProp) ? (PaL.X.Shared.Models.VisibilityLevel)phoneVisProp.GetInt32() : PaL.X.Shared.Models.VisibilityLevel.Friends
            };

            var response = await _httpClient.PutAsJsonAsync($"/api/users/{_currentUser.Username}/profile", profile);
            if (response.IsSuccessStatusCode)
            {
                NavigateToMain();
            }
            else
            {
                MessageBox.Show("Erreur lors de la sauvegarde du profil.");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur sauvegarde profil: {ex.Message}");
        }
    }

    private async Task HandleRegister(string username, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/register", new { Username = username, Password = password });
            if (response.IsSuccessStatusCode)
            {
                PostMessage(new { type = "registerSuccess" });
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                PostMessage(new { type = "registerError", message = error });
            }
        }
        catch (Exception ex)
        {
            PostMessage(new { type = "registerError", message = "Erreur de connexion au serveur" });
        }
    }

    private async void SendInitData()
    {
        // Fetch friends from API
        var recipientName = "Personne";
        int recipientId = 0;

        try 
        {
            var friendsResponse = await _httpClient.GetAsync($"/api/users/{_currentUser.Username}/friends");
            if (friendsResponse.IsSuccessStatusCode)
            {
                var friends = await friendsResponse.Content.ReadFromJsonAsync<List<dynamic>>();
                if (friends != null && friends.Count > 0)
                {
                    // Dynamic parsing is tricky, let's assume structure
                    var first = friends[0];
                    // System.Text.Json dynamic deserialization results in JsonElement usually
                    if (first is JsonElement el)
                    {
                        recipientId = el.GetProperty("id").GetInt32();
                        recipientName = el.GetProperty("username").GetString();
                    }
                }
            }
        }
        catch {}

        var payload = new
        {
            type = "setUserInfo",
            payload = new 
            {
                user = new { id = _currentUser.Id, name = _currentUser.Username },
                recipient = new { id = recipientId, name = recipientName }
            }
        };
        PostMessage(payload);
    }

    private void SendSmileys()
    {
        string smileysRoot = Path.Combine(AssetsPath, "Smiley");
        var smileyData = new Dictionary<string, List<string>>();

        if (Directory.Exists(smileysRoot))
        {
            // Get subdirectories as categories
            var directories = Directory.GetDirectories(smileysRoot);
            foreach (var dir in directories)
            {
                string category = Path.GetFileName(dir);
                var files = Directory.GetFiles(dir)
                    .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || 
                                f.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) || 
                                f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                    .Select(Path.GetFileName)
                    .ToList();
                
                if (files.Count > 0)
                {
                    smileyData[category] = files!;
                }
            }

            // Also check root for "Défaut"
            var rootFiles = Directory.GetFiles(smileysRoot)
                .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || 
                            f.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) || 
                            f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                .Select(Path.GetFileName)
                .ToList();
            
            if (rootFiles.Count > 0)
            {
                smileyData["Défaut"] = rootFiles!;
            }
        }

        PostMessage(new { type = "loadSmileys", payload = smileyData });
    }

    private void HandleSendMessage(string content)
    {
        // Echo back for now (Simulation)
        // Note: In a real app, we would save to DB here
        
        var msg = new 
        {
            content = content,
            isMe = true,
            timestamp = DateTime.Now,
            senderName = _currentUser.Username
        };

        PostMessage(new { type = "addMessage", payload = msg });

        // Simulate reply
        Task.Delay(1000).ContinueWith(_ => 
        {
            this.Invoke(() => 
            {
                var reply = new 
                {
                    content = "Ceci est une réponse automatique de PaL.X v2 🤖",
                    isMe = false,
                    timestamp = DateTime.Now,
                    senderName = "Ami Connecté"
                };
                PostMessage(new { type = "addMessage", payload = reply });
            });
        });
    }

    private void HandleSendImage(JsonElement payload)
    {
        // Placeholder for image handling
        // payload contains { name, data } (base64)
    }

    private void PostMessage(object data)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        string json = JsonSerializer.Serialize(data, options);
        webView.CoreWebView2.PostWebMessageAsJson(json);
    }

    // P/Invoke for dragging
    public const int WM_NCLBUTTONDOWN = 0xA1;
    public const int HT_CAPTION = 0x2;

    [DllImport("user32.dll")]
    public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
    [DllImport("user32.dll")]
    public static extern bool ReleaseCapture();

    protected override async void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (_currentUser != null)
        {
            try
            {
                await _httpClient.PostAsync($"api/auth/logout/{_currentUser.Id}", null);
            }
            catch { /* Ignore errors on exit */ }
        }
    }

    private void InitializeHeartbeat()
    {
        _heartbeatTimer = new System.Windows.Forms.Timer();
        _heartbeatTimer.Interval = 3000; // Check every 3 seconds
        _heartbeatTimer.Tick += async (s, e) => await CheckServerStatus();
        _heartbeatTimer.Start();
    }

    private async Task CheckServerStatus()
    {
        try
        {
            // Use a short timeout so the UI doesn't freeze if the server hangs
            using (var cts = new CancellationTokenSource(1000))
            {
                var response = await _httpClient.GetAsync("api/auth/ping", cts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    TriggerMaintenanceMode();
                }
            }
        }
        catch
        {
            TriggerMaintenanceMode();
        }
    }

    private void TriggerMaintenanceMode()
    {
        // Stop the timer to prevent multiple triggers
        _heartbeatTimer.Stop();
        
        // Hide the main form
        this.Hide();
        
        // Show the maintenance dialog
        using (var frm = new FormMaintenance())
        {
            frm.ShowDialog();
        }
        
        // Exit the application
        Application.Exit();
    }
}
