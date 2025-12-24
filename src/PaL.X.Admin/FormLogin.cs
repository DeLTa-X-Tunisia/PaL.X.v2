using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Text.Json;
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
    private AuthenticationService _authService;
    private PalContext _context;
    private Process? _serverProcess;

    public User? LoggedInUser { get; private set; }

    public FormLogin()
    {
        InitializeComponent();
        InitializeDatabase();
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

    private void InitializeDatabase()
    {
        var options = new DbContextOptionsBuilder<PalContext>()
            .UseNpgsql("Host=localhost;Database=PaL.X.v2;Username=postgres;Password=2012704")
            .Options;
        _context = new PalContext(options);
        _authService = new AuthenticationService(_context);
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
                            PostMessage(new { type = "initProfile", username = LoggedInUser.Username });
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
                            PostMessage(new { type = "init", user = new { username = LoggedInUser.Username, isAdmin = LoggedInUser.IsAdmin } });
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
        var user = await _authService.LoginAsync(username, password);
        
        if (user != null)
        {
            if (!user.IsAdmin)
            {
                PostMessage(new { type = "loginError", message = "Accès réservé aux administrateurs." });
                return;
            }

            LoggedInUser = user;
            
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
            PostMessage(new { type = "loginError", message = "Identifiants incorrects." });
        }
    }

    private void NavigateToMain()
    {
        this.Invoke(() => {
            this.Size = new Size(380, 750);
            this.CenterToScreen();
        });
        webView.CoreWebView2.Navigate("https://app.pal.x/main.html");
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
            var user = await _authService.RegisterAsync(username, password);
            // Auto-promote to admin for this specific form
            user.IsAdmin = true;
            await _context.SaveChangesAsync();
            
            PostMessage(new { type = "registerSuccess" });
        }
        catch (Exception ex)
        {
            PostMessage(new { type = "registerError", message = ex.Message });
        }
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
