using Microsoft.EntityFrameworkCore;
using PaL.X.Data;
using PaL.X.Shared.Models;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PaL.X.Admin;

public partial class Form1 : Form
{
    private PalContext _context;
    private DataGridView dgvUsers;
    private Button btnRefresh;
    private Button btnAddUser;

    public Form1()
    {
        InitializeComponent();
        InitializeCustomComponents();
        InitializeDatabase();
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
            var users = _context.Users.ToList();
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
}
