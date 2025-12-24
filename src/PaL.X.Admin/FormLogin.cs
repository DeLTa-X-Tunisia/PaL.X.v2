using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Json;
using PaL.X.Data;
using PaL.X.Core;
using PaL.X.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace PaL.X.Admin;

public partial class FormLogin : Form
{
    private WebView2 webView;
    private HttpClient _httpClient;
    private Process? _serverProcess;
    private const string ApiUrl = "http://localhost:5030";

    public User? LoggedInUser { get; private set; }

    public FormLogin()
    {
        InitializeComponent();
        _httpClient = new HttpClient { BaseAddress = new Uri(ApiUrl) };
        InitializeWebView();
        CheckExistingServer();
    }

    private void CheckExistingServer()
    {
        // Check if already running
        var processes = Process.GetProcessesByName("PaL.X.Server");
        if (processes.Length > 0)
        {
            _serverProcess = processes[0];
        }
    }

    private void InitializeComponent()
    {
        this.FormBorderStyle = FormBorderStyle.None;
        this.Size = new Size(400, 550);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MaximizeBox = false;
    }

    private async void InitializeWebView()
    {
        webView = new WebView2();
        webView.Dock = DockStyle.Fill;
        this.Controls.Add(webView);

        await webView.EnsureCoreWebView2Async();

        // Map wwwroot from App project (assuming relative path)
        // We need to find the App's wwwroot. Since we are in Admin/bin/Debug..., we go up.
        // Or we can just copy the file. For now, let's try to map the same folder if possible.
        // A robust way is to embed it or copy it. Let's assume the build copies it or we point to source.
        
        // HACK: Pointing to source for dev
        string appProjectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\..\src\PaL.X.App\wwwroot"));
        
        if (Directory.Exists(appProjectRoot))
        {
             webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "app.pal.x", 
                appProjectRoot, 
                CoreWebView2HostResourceAccessKind.Allow
            );
            webView.Source = new Uri("https://app.pal.x/login.html");
        }
        else
        {
            MessageBox.Show("Impossible de trouver les fichiers UI (wwwroot).");
        }

        webView.CoreWebView2.WebMessageReceived += WebView_WebMessageReceived;
        webView.NavigationCompleted += WebView_NavigationCompleted;
    }

    private async void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
             await webView.ExecuteScriptAsync("if(typeof setTitle === 'function') setTitle('PaL.X Admin');");
             
             // Enable Admin Mode in UI
             bool isRunning = _serverProcess != null && !_serverProcess.HasExited;
             PostMessage(new { type = "setAdminMode", isRunning = isRunning });
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
                    case "toggleServer":
                        ToggleServer();
                        break;
                    case "cancel":
                        // Ensure we kill the server if we started it (optional, but good for cleanup)
                        if (_serverProcess != null && !_serverProcess.HasExited)
                        {
                            try { _serverProcess.Kill(); } catch {}
                        }
                        Environment.Exit(0);
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
                    case "profileReady":
                        if (LoggedInUser != null)
                        {
                            string avatarUrl = null;
                            try
                            {
                                var profileResponse = await _httpClient.GetAsync($"/api/users/{LoggedInUser.Username}/profile");
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
                            PostMessage(new { type = "initProfile", username = LoggedInUser.Username, avatarUrl = avatarUrl });
                        }
                        break;
                    case "saveProfile":
                        if (message.TryGetProperty("payload", out var profilePayload))
                        {
                            await HandleSaveProfile(profilePayload);
                        }
                        break;
                    case "cancelProfile":
                        LoggedInUser = null;
                        webView.CoreWebView2.Navigate("https://app.pal.x/login.html");
                        this.Invoke(() => {
                            this.Size = new Size(400, 550);
                            this.CenterToScreen();
                        });
                        break;
                    case "mainReady":
                        if (LoggedInUser != null)
                        {
                            string avatarUrl = null;
                            string fullName = LoggedInUser.Username;
                            try
                            {
                                var profileResponse = await _httpClient.GetAsync($"/api/users/{LoggedInUser.Username}/profile");
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
                            PostMessage(new { type = "init", user = new { username = LoggedInUser.Username, displayName = fullName, isAdmin = LoggedInUser.IsAdmin, avatarUrl = avatarUrl }, apiUrl = ApiUrl });
                        }
                        break;
                    case "logout":
                        LoggedInUser = null;
                        webView.CoreWebView2.Navigate("https://app.pal.x/login.html");
                        this.Invoke(() => {
                            this.Size = new Size(400, 550);
                            this.CenterToScreen();
                        });
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
        }
    }

    private void ToggleServer()
    {
        bool isRunning = _serverProcess != null && !_serverProcess.HasExited;

        if (isRunning)
        {
            // Stop Server
            try 
            {
                // Kill all instances to be sure
                foreach (var proc in Process.GetProcessesByName("PaL.X.Server"))
                {
                    proc.Kill();
                }
                _serverProcess = null;
                PostMessage(new { type = "serverStatus", isRunning = false });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur arrêt serveur: {ex.Message}");
            }
        }
        else
        {
            // Start Server
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "run --project ../../../../../src/PaL.X.Server/PaL.X.Server.csproj", // Relative path from bin/Debug/net10.0-windows
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                
                _serverProcess = Process.Start(startInfo);
                PostMessage(new { type = "serverStatus", isRunning = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur démarrage serveur: {ex.Message}");
            }
        }
    }

    private async Task HandleLogin(string username, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", new { Username = username, Password = password });
            if (response.IsSuccessStatusCode)
            {
                var user = await response.Content.ReadFromJsonAsync<PaL.X.Shared.Models.User>();
                if (!user.IsAdmin)
                {
                    PostMessage(new { type = "loginError", message = "Accès réservé aux administrateurs." });
                    return;
                }

                LoggedInUser = user;
                
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
                PostMessage(new { type = "loginError", message = "Identifiants incorrects." });
            }
        }
        catch (Exception ex)
        {
            PostMessage(new { type = "loginError", message = "Erreur de connexion au serveur." });
        }
    }

    private void NavigateToMain()
    {
        this.Invoke(() => {
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
                    user = LoggedInUser,
                    apiUrl = ApiUrl
                });
            }
        };
    }

    private void NavigateToProfile()
    {
        this.Invoke(() => {
            this.Size = new Size(1000, 780);
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
                UserId = LoggedInUser.Id,
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

            var response = await _httpClient.PutAsJsonAsync($"/api/users/{LoggedInUser.Username}/profile", profile);
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
            var response = await _httpClient.PostAsJsonAsync("/api/auth/register", new { Username = username, Password = password });
            if (response.IsSuccessStatusCode)
            {
                // Auto-promote to admin for this specific form (Logic moved to server or handled here? 
                // Server doesn't know context. We might need a special endpoint or just accept user is created)
                // For now, just register. Admin promotion should be separate or via DB seed.
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
            PostMessage(new { type = "registerError", message = ex.Message });
        }
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
        if (LoggedInUser != null)
        {
            try
            {
                await _httpClient.PostAsync($"api/auth/logout/{LoggedInUser.Id}", null);
            }
            catch { /* Ignore errors on exit */ }
        }

        if (_serverProcess != null && !_serverProcess.HasExited)
        {
             try { _serverProcess.Kill(); } catch {}
        }
    }
}
