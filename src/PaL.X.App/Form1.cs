using Microsoft.Web.WebView2.Core;
using System.Text.Json;
using PaL.X.Shared;
using PaL.X.Data;
using PaL.X.Core;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;

namespace PaL.X.App;

public partial class Form1 : Form
{
    private Microsoft.Web.WebView2.WinForms.WebView2 webView;
    private const string AssetsPath = @"C:\Users\azizi\OneDrive\Desktop\PaL.X\PaL.X.v2.assets";
    private const string ApiUrl = "http://localhost:5030";
    private AuthenticationService _authService;
    private PalContext _dbContext;
    private PaL.X.Shared.Models.User _currentUser;

    public Form1()
    {
        InitializeComponent();
        InitializeDatabase();
        InitializeWebView();
        
        // Borderless and Compact
        this.FormBorderStyle = FormBorderStyle.None;
        this.Size = new Size(400, 550);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MaximizeBox = false;
    }

    private void InitializeDatabase()
    {
        var options = new DbContextOptionsBuilder<PalContext>()
            .UseNpgsql("Host=localhost;Database=PaL.X.v2;Username=postgres;Password=2012704")
            .Options;
            
        _dbContext = new PalContext(options);
        
        // Ensure DB exists (Quick dev mode)
        try 
        {
            _dbContext.Database.EnsureCreated();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur connexion DB: {ex.Message}\nAssurez-vous que PostgreSQL est lancé et la base créée.");
        }

        _authService = new AuthenticationService(_dbContext);
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
                            var profile = await _authService.GetProfileAsync(_currentUser.Id);
                            string avatarUrl = profile?.ProfilePictureUrl;
                            if (!string.IsNullOrEmpty(avatarUrl) && !avatarUrl.StartsWith("http"))
                            {
                                 avatarUrl = ApiUrl + avatarUrl;
                            }
                            PostMessage(new { type = "init", user = new { username = _currentUser.Username, isAdmin = _currentUser.IsAdmin, avatarUrl = avatarUrl } });
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
                            var profile = await _authService.GetProfileAsync(_currentUser.Id);
                            string avatarUrl = profile?.ProfilePictureUrl;
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
        var user = await _authService.LoginAsync(username, password);
        if (user != null)
        {
            _currentUser = user;
            
            // Check Profile
            var profile = await _authService.GetProfileAsync(user.Id);
            
            if (profile != null && profile.IsComplete())
            {
                NavigateToMain();
            }
            else
            {
                NavigateToProfile();
            }
        }
        else
        {
            PostMessage(new { type = "loginError", message = "Identifiants incorrects" });
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

            await _authService.SaveProfileAsync(profile);
            NavigateToMain();
        }
        catch (Exception ex)
        {
            var message = $"Erreur sauvegarde profil: {ex.Message}";
            if (ex.InnerException != null)
            {
                message += $"\n\nDétails: {ex.InnerException.Message}";
            }
            MessageBox.Show(message);
        }
    }

    private async Task HandleRegister(string username, string password)
    {
        try
        {
            await _authService.RegisterAsync(username, password);
            PostMessage(new { type = "registerSuccess" });
        }
        catch (Exception ex)
        {
            PostMessage(new { type = "registerError", message = ex.Message });
        }
    }

    private async void SendInitData()
    {
        var friends = await _authService.GetFriendsAsync(_currentUser.Id);
        var recipient = friends.FirstOrDefault(); // Just pick first for now

        var payload = new
        {
            type = "setUserInfo",
            payload = new 
            {
                user = new { id = _currentUser.Id, name = _currentUser.Username },
                recipient = recipient != null ? new { id = recipient.Id, name = recipient.Username } : new { id = 0, name = "Personne" }
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
        string json = JsonSerializer.Serialize(data);
        webView.CoreWebView2.PostWebMessageAsJson(json);
    }

    // P/Invoke for dragging
    public const int WM_NCLBUTTONDOWN = 0xA1;
    public const int HT_CAPTION = 0x2;

    [DllImport("user32.dll")]
    public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
    [DllImport("user32.dll")]
    public static extern bool ReleaseCapture();
}
