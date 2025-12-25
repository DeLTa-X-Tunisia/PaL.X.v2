using Microsoft.EntityFrameworkCore;
using PaL.X.Data;
using PaL.X.Shared.Models;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PaL.X.Admin;

public partial class Form1 : Form
{
    private PalContext _context;
    private DataGridView dgvUsers;
    private Button btnRefresh;
    private Button btnAddUser;
    
    private const string ApiUrl = "http://localhost:5030";
    private readonly HttpClient _httpClient;
    private System.Windows.Forms.Timer _heartbeatTimer;

    public Form1()
    {
        InitializeComponent();
        _httpClient = new HttpClient { BaseAddress = new Uri(ApiUrl) };
        InitializeCustomComponents();
        InitializeDatabase();
        InitializeHeartbeat();
    }

    private void InitializeDatabase()
    {
        var options = new DbContextOptionsBuilder<PalContext>()
            .UseNpgsql("Host=localhost;Database=PaL.X.v2;Username=postgres;Password=2012704")
            .Options;
        _context = new PalContext(options);
        
        // Ensure created just in case Admin is run first
        try { _context.Database.EnsureCreated(); } catch {}
        
        LoadUsers();
    }

    private void InitializeCustomComponents()
    {
        this.Text = "PaL.X Admin v2";
        this.Size = new Size(800, 600);

        dgvUsers = new DataGridView();
        dgvUsers.Dock = DockStyle.Top;
        dgvUsers.Height = 400;
        dgvUsers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        this.Controls.Add(dgvUsers);

        btnRefresh = new Button();
        btnRefresh.Text = "Rafraîchir";
        btnRefresh.Location = new Point(10, 410);
        btnRefresh.Click += (s, e) => LoadUsers();
        this.Controls.Add(btnRefresh);
        
        btnAddUser = new Button();
        btnAddUser.Text = "Ajouter Test User";
        btnAddUser.Location = new Point(100, 410);
        btnAddUser.Width = 120;
        btnAddUser.Click += BtnAddUser_Click;
        this.Controls.Add(btnAddUser);
    }

    private void LoadUsers()
    {
        try
        {
            // Refresh context to get latest data
            _context.ChangeTracker.Clear();
            
            var users = _context.Users
                .Select(u => new 
                {
                    u.Id,
                    u.Username,
                    u.IsAdmin,
                    u.Status,
                    u.CreatedAt,
                    // Manual join since navigation property might not be set up both ways
                    Profile = _context.UserProfiles.FirstOrDefault(p => p.UserId == u.Id)
                })
                .ToList()
                .Select(u => new 
                {
                    u.Id,
                    Username = u.Username,
                    NomComplet = (u.Profile != null && (!string.IsNullOrEmpty(u.Profile.FirstName) || !string.IsNullOrEmpty(u.Profile.LastName)))
                        ? $"{u.Profile.LastName} {u.Profile.FirstName}".Trim()
                        : u.Username,
                    u.IsAdmin,
                    u.Status,
                    Avatar = !string.IsNullOrEmpty(u.Profile?.ProfilePictureUrl) ? "Oui" : "Non",
                    u.CreatedAt
                })
                .ToList();

            dgvUsers.DataSource = users;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Erreur DB: " + ex.Message);
        }
    }
    
    private void BtnAddUser_Click(object? sender, EventArgs e)
    {
        var user = new User
        {
            Username = "User_" + new Random().Next(1000),
            PasswordHash = "1234", 
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Users.Add(user);
        _context.SaveChanges();
        LoadUsers();
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
        _heartbeatTimer.Stop();
        this.Hide();
        using (var frm = new FormMaintenance())
        {
            frm.ShowDialog();
        }
        Application.Exit();
    }
}
