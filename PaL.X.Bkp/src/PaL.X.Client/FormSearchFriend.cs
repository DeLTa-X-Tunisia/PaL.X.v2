using PaL.X.Client.Services;
using PaL.X.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PaL.X.Client
{
    public partial class FormSearchFriend : Form
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;
        private readonly MainForm _mainForm;
        private readonly int _currentUserId;
        private List<UserProfileDto> _allUsers = new List<UserProfileDto>();
        private ImageList _imageList = null!;
    private ToolStripMenuItem? _blockMenuItem;
    private ToolStripMenuItem? _unblockMenuItem;
        private const string MaleIconKey = "gender_male";
        private const string FemaleIconKey = "gender_female";
        private const string NeutralIconKey = "gender_neutral";

        // Constructeur pour le Designer
        public FormSearchFriend()
        {
            InitializeComponent();
            _httpClient = new HttpClient();
            _apiBaseUrl = string.Empty;
            _mainForm = null!;
            InitializeImageList();
        }

        public FormSearchFriend(HttpClient httpClient, string apiBaseUrl, MainForm mainForm, int currentUserId)
        {
            InitializeComponent();
            _httpClient = httpClient;
            _apiBaseUrl = apiBaseUrl;
            _mainForm = mainForm;
            _currentUserId = currentUserId;

            InitializeContextMenu();
            InitializeImageList();
        }

        private void InitializeImageList()
        {
            _imageList = new ImageList
            {
                ImageSize = new Size(40, 40),
                ColorDepth = ColorDepth.Depth32Bit,
                TransparentColor = Color.Transparent
            };

            LoadGenderIcon(MaleIconKey, "gender/homme.ico", Color.LightSkyBlue, "H");
            LoadGenderIcon(FemaleIconKey, "gender/femme.ico", Color.Pink, "F");
            LoadGenderIcon(NeutralIconKey, "gender/autre.ico", Color.LightGray, "?");

            lstUsers.SmallImageList = _imageList;
        }

        private void LoadGenderIcon(string key, string resourceKey, Color fallbackColor, string fallbackGlyph)
        {
            if (_imageList.Images.ContainsKey(key))
                return;

            var resourceImage = ResourceImageStore.LoadImage(resourceKey);
            if (resourceImage != null)
            {
                _imageList.Images.Add(key, resourceImage);
                return;
            }

            _imageList.Images.Add(key, CreateFallbackGlyph(fallbackColor, fallbackGlyph));
        }

        private Image CreateFallbackGlyph(Color backgroundColor, string glyph)
        {
            var bmp = new Bitmap(40, 40);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(backgroundColor);
                using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using var font = new Font("Segoe UI", 14, FontStyle.Bold);
                g.DrawString(glyph, font, Brushes.White, new RectangleF(0, 0, 40, 40), format);
            }
            return bmp;
        }

        private void InitializeContextMenu()
        {
            var contextMenu = new ContextMenuStrip();
            
            // Chargement des icônes depuis les ressources
            Image? imgAdd = ResourceImageStore.LoadImage("icon/context/ajouter.ico");
            Image? imgBlock = ResourceImageStore.LoadImage("icon/context/bloquer.ico");
            Image? imgUnblock = ResourceImageStore.LoadImage("icon/context/debloquer.ico");
            Image? imgProfile = ResourceImageStore.LoadImage("icon/context/profile.png");

            var viewProfileItem = new ToolStripMenuItem("Voir profil");
            if (imgProfile != null) viewProfileItem.Image = imgProfile;
            viewProfileItem.Click += (s, e) => 
            {
                if (lstUsers.SelectedItems.Count > 0)
                {
                    var item = lstUsers.SelectedItems[0];
                    if (item.Tag is UserProfileDto user)
                    {
                        using var profileForm = new UserProfileViewForm(_httpClient, user.Id);
                        profileForm.ShowDialog(this);
                    }
                }
            };

            var addItem = new ToolStripMenuItem("Ajouter comme ami");
            if (imgAdd != null) addItem.Image = imgAdd;
            addItem.Click += async (s, e) => 
            {
                if (lstUsers.SelectedItems.Count > 0)
                {
                    var item = lstUsers.SelectedItems[0];
                    if (item.Tag is UserProfileDto user)
                    {
                        await _mainForm.AddFriend(user);
                    }
                }
            };
            
            _blockMenuItem = new ToolStripMenuItem("Bloquer");
            if (imgBlock != null) _blockMenuItem.Image = imgBlock;
            _blockMenuItem.Click += (s, e) => PalMessageBox.Show("Fonctionnalité Bloquer à venir.");

            _unblockMenuItem = new ToolStripMenuItem("Débloquer");
            if (imgUnblock != null) _unblockMenuItem.Image = imgUnblock;
            _unblockMenuItem.Click += (s, e) => PalMessageBox.Show("Fonctionnalité Débloquer à venir.");
            
            contextMenu.Items.Add(viewProfileItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(addItem);
            contextMenu.Items.Add(_blockMenuItem);
            contextMenu.Items.Add(_unblockMenuItem);
            contextMenu.Opening += (s, e) => UpdateContextMenuState();
            
            lstUsers.ContextMenuStrip = contextMenu;
            lstUsers.SelectedIndexChanged += (s, e) => UpdateContextMenuState();
        }

        private void UpdateContextMenuState()
        {
            var user = lstUsers.SelectedItems.Count > 0 ? lstUsers.SelectedItems[0].Tag as UserProfileDto : null;

            if (_blockMenuItem != null)
            {
                _blockMenuItem.Enabled = user != null && !user.IsBlocked;
            }

            if (_unblockMenuItem != null)
            {
                _unblockMenuItem.Enabled = user != null && user.IsBlocked;
            }
        }

        private async void FormSearchFriend_Load(object sender, EventArgs e)
        {
            await LoadUsers();
        }

        private async Task LoadUsers()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/profile/all");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var allUsers = JsonSerializer.Deserialize<List<UserProfileDto>>(json, options) ?? new List<UserProfileDto>();
                    
                    // Filtrer l'utilisateur courant
                    _allUsers = allUsers.Where(u => u.Id != _currentUserId).ToList();
                    
                    DisplayUsers(_allUsers);
                }
                else
                {
                    PalMessageBox.Show("Impossible de charger la liste des utilisateurs.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                PalMessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DisplayUsers(List<UserProfileDto> users)
        {
            lstUsers.BeginUpdate();
            lstUsers.Items.Clear();
            foreach (var user in users)
            {
                var item = new ListViewItem();
                item.Tag = user;
                item.ImageKey = ResolveGenderKey(user.Gender);

                // Add SubItems
                item.SubItems.Add(user.FirstName);
                item.SubItems.Add(user.LastName);
                item.SubItems.Add(CalculateAge(user.DateOfBirth));
                item.SubItems.Add(user.Gender);
                item.SubItems.Add(user.Country);

                lstUsers.Items.Add(item);
            }

            lstUsers.EndUpdate();
        }

        private string ResolveGenderKey(string gender)
        {
            if (string.IsNullOrWhiteSpace(gender))
                return NeutralIconKey;

            string normalized = gender.Trim().ToLowerInvariant();
            return normalized switch
            {
                "homme" => MaleIconKey,
                "male" => MaleIconKey,
                "femme" => FemaleIconKey,
                "female" => FemaleIconKey,
                _ => NeutralIconKey
            };
        }

        private string CalculateAge(DateTime dateOfBirth)
        {
            var today = DateTime.Today;
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > today.AddYears(-age)) age--;
            return $"{age} Ans";
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            var query = txtSearch.Text.ToLower();
            var filtered = _allUsers.Where(u => 
                u.FirstName.ToLower().Contains(query) || 
                u.LastName.ToLower().Contains(query) || 
                u.Country.ToLower().Contains(query)
            ).ToList();

            DisplayUsers(filtered);
        }
    }
}
