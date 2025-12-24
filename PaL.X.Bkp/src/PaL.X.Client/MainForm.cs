using PaL.X.Client.Services;
using PaL.X.Client.Voice;
using PaL.X.Client.Video;
using PaL.X.Shared.DTOs;
using PaL.X.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.AspNetCore.SignalR.Client;

namespace PaL.X.Client
{
    public partial class MainForm : Form
    {
        private readonly string _authToken;
        private readonly UserData _currentUser;
        private readonly HttpClient _httpClient;
        private HubConnection _hubConnection = null!;
        private const string ApiBaseUrl = "https://localhost:5001/api";
        private const string HubUrl = "https://localhost:5001/hubs/pal";
        private bool _serviceAvailable = false;
        private string _connectionId = string.Empty;
        
        private ImageList imgListGender = null!;
        private ImageList imgListStatus = null!;
        
    private Dictionary<int, FormChat> _openChats = new Dictionary<int, FormChat>();
    private UserProfileForm? _openProfileMe = null;
    private Dictionary<int, UserProfileViewForm> _openProfilesFriends = new Dictionary<int, UserProfileViewForm>();
        private FormSearchFriend? _formSearchFriend;
        private FormBlockedUsers? _blockedUsersForm;
        private readonly Dictionary<int, BlockedUserDto> _blockedUsers = new();
        private readonly Dictionary<int, RemoteBlockState> _blockedByOthers = new();
    private readonly HashSet<int> _acknowledgedBlockNotifications = new();
        private ToolStripMenuItem? _blockMenuItem;
        private ToolStripMenuItem? _unblockMenuItem;
        private ToolStripMenuItem? _deleteMenuItem;
        private ToolStripMenuItem? _callMenuItem;
        private ToolStripMenuItem? _videoCallMenuItem;
        private int _blockedIconIndex = -1;
        private Image? _blockNoticeIcon;
        private Image? _unblockNoticeIcon;
        
        private UserActivityMonitor? _activityMonitor;
    private UserStatus _currentUserStatus = UserStatus.Online;
    public UserStatus CurrentUserStatus => _currentUserStatus;
        private ToolStripDropDownButton? _statusButton;

        // Voice call
        private VoiceCallService? _voiceCallService;
        private VoiceCallForm? _activeCallForm;
        private string? _activeCallId;
        private int _activeCallPeerId;
        private DateTime? _activeCallStartedAt;
    private UserStatus? _statusBeforeCall;

        // Video call
        private VideoCallService? _videoCallService;
        private VideoCallForm? _activeVideoCallForm;
        private string? _activeVideoCallId;
        private int _activeVideoCallPeerId;
        private DateTime? _activeVideoCallStartedAt;

        private static Image? LoadAbsoluteIcon(string path)
        {
            try
            {
                if (!System.IO.File.Exists(path)) return null;
                using var img = Image.FromFile(path);
                return new Bitmap(img);
            }
            catch
            {
                return null;
            }
        }

        // Constructeur pour le Designer
        public MainForm()
        {
            InitializeComponent();
            if (DesignMode) return;

            _authToken = string.Empty;
            _currentUser = new UserData();
            _httpClient = new HttpClient();
            LoadBlockAcknowledgements();
        }

        public MainForm(string authToken, UserData currentUser)
        {
            InitializeComponent();
            SetupRuntimeUI();
            LoadBlockAcknowledgements();
            _authToken = authToken;
            _currentUser = currentUser;
            
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
            _httpClient = new HttpClient(handler);
            
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _authToken);
        }

        private void BtnMyProfile_Click(object? sender, EventArgs e)
        {
            var profileForm = new UserProfileForm(_httpClient);
            _openProfileMe = profileForm;
            profileForm.FormClosed += (s, e2) =>
            {
                if (_openProfileMe == profileForm)
                {
                    _openProfileMe = null;
                }
            };
            profileForm.Show(this);
        }

        private async void BtnBlockedUsers_Click(object? sender, EventArgs e)
        {
            if (_blockedUsersForm == null || _blockedUsersForm.IsDisposed)
            {
                var form = new FormBlockedUsers(this);
                form.FormClosed += (_, __) =>
                {
                    if (_blockedUsersForm == form)
                    {
                        _blockedUsersForm = null;
                    }
                };

                _blockedUsersForm = form;
                form.UpdateList(_blockedUsers.Values);
                form.Show(this);
            }
            else
            {
                if (_blockedUsersForm.WindowState == FormWindowState.Minimized)
                {
                    _blockedUsersForm.WindowState = FormWindowState.Normal;
                }

                _blockedUsersForm.BringToFront();
                _blockedUsersForm.Focus();
            }

            await LoadBlockedUsersSafeAsync();
        }

        private void SetupRuntimeUI()
        {
            // ImageList pour Genre
            imgListGender = new ImageList();
            imgListGender.ImageSize = new System.Drawing.Size(16, 16);
            imgListGender.ColorDepth = ColorDepth.Depth32Bit;
            
            try 
            {
                TryAddGenderIcon("Homme", "gender/homme.ico");
                TryAddGenderIcon("Femme", "gender/femme.ico");
                TryAddGenderIcon("Autre", "gender/autre.ico");
                TryAddGenderIcon("Pending", "icon/status/pending.ico");
                if (TryAddGenderIcon("Blocked", "message/devil.ico"))
                {
                    _blockedIconIndex = imgListGender.Images.IndexOfKey("Blocked");
                }
            }
            catch (Exception ex)
            {
                PalMessageBox.Show("Erreur chargement icônes: " + ex.Message);
            }

            // ImageList pour Statut
            imgListStatus = new ImageList();
            imgListStatus.ImageSize = new System.Drawing.Size(16, 16);
            imgListStatus.ColorDepth = ColorDepth.Depth32Bit;
            
            try 
            {
                TryAddStatusIcon("icon/status/en_ligne.ico", "Online");
                TryAddStatusIcon("icon/status/hors_ligne.ico", "Offline");
                TryAddStatusIcon("icon/status/absent.ico", "Away");
                TryAddStatusIcon("icon/status/brb.ico", "BRB");
                TryAddStatusIcon("icon/status/dnd.ico", "DoNotDisturb");
                TryAddStatusIcon("icon/status/occupé.ico", "Busy");
                TryAddStatusIcon(@"Voice/en_appel.png", "InCall");
            }
            catch (Exception ex)
            {
                PalMessageBox.Show("Erreur chargement icônes statut: " + ex.Message);
            }
            
            // Setup Status Menu Button
            SetupStatusMenu();

            // ListView Amis Setup
            lstFriends.SmallImageList = imgListGender;
            lstFriends.DrawColumnHeader += (sender, e) => e.DrawDefault = true;
            lstFriends.DrawItem += (sender, e) => e.DrawDefault = false;
            lstFriends.DrawSubItem += LstFriends_DrawSubItem;
            lstFriends.SelectedIndexChanged += LstFriends_SelectedIndexChanged;
            lstFriends.MouseClick += (s, e) => 
            {
                if (e.Button == MouseButtons.Right)
                {
                    var item = lstFriends.GetItemAt(e.X, e.Y);
                    if (item != null)
                    {
                        item.Selected = true;
                        lstFriends.ContextMenuStrip?.Show(lstFriends, e.Location);
                    }
                }
            };

            // Context Menu for Friends List
            var contextMenu = new ContextMenuStrip();
            var itemProfile = new ToolStripMenuItem("Voir profil");
            
            // Load icon for menu item
            var profileIcon = LoadImageSafe("icon/context/profile.png");
            if (profileIcon != null)
            {
                itemProfile.Image = profileIcon;
            }

            _callMenuItem = new ToolStripMenuItem("Appel vocal");
            var callIcon = LoadAbsoluteIcon(@"C:\Users\azizi\OneDrive\Desktop\PaL.X\Voice\appel_vocal.png") ?? VoiceIcons.VolumeIcon ?? VoiceIcons.IncomingIcon;
            if (callIcon != null)
            {
                _callMenuItem.Image = callIcon;
            }
            _callMenuItem.Click += async (s, e) => await StartCallWithSelectedFriend();

            _videoCallMenuItem = new ToolStripMenuItem("Appel vidéo");
            var videoCallIcon = LoadAbsoluteIcon(@"C:\Users\azizi\OneDrive\Desktop\PaL.X\PaL.X.Assets\Context\Video_Call.png");
            if (videoCallIcon != null)
            {
                _videoCallMenuItem.Image = videoCallIcon;
            }
            _videoCallMenuItem.Click += async (s, e) => await StartVideoCallWithSelectedFriend();

            itemProfile.Click += (s, e) =>
            {
                if (lstFriends.SelectedItems.Count == 0)
                {
                    return;
                }

                var selectedItem = lstFriends.SelectedItems[0];
                if (selectedItem?.Tag is not UserProfileDto user)
                {
                    return;
                }

                if (_openProfilesFriends.TryGetValue(user.Id, out var existing) && existing != null && !existing.IsDisposed)
                {
                    existing.BringToFront();
                    existing.Focus();
                    return;
                }

                var profileForm = new UserProfileViewForm(_httpClient, user.Id);
                _openProfilesFriends[user.Id] = profileForm;
                profileForm.FormClosed += (s2, e2) => { _openProfilesFriends.Remove(user.Id); };
                profileForm.Show(this);
            };
            contextMenu.Items.Add(itemProfile);
            contextMenu.Items.Add(_callMenuItem);
            contextMenu.Items.Add(_videoCallMenuItem);
            lstFriends.ContextMenuStrip = contextMenu;
            lstFriends.DoubleClick += LstFriends_DoubleClick;
            lstFriends.ListViewItemSorter = new FriendListComparer();
            
            // Context Menu pour Amis (Merge with existing logic if needed, but for now adding items to the one created above)
            // Note: I created 'contextMenu' above. I should use that instead of creating a new one or merge them.
            // The code below creates 'contextMenuFriends' but doesn't assign it yet.
            // Let's reuse 'contextMenu' which is already assigned to lstFriends.ContextMenuStrip
            
            // Chargement des icônes du menu contextuel
            // Use "icon/context/..." prefix as registered in Resources.resx
            Image? imgBlock = LoadImageSafe("icon/context/bloquer.ico");
            Image? imgUnblock = LoadImageSafe("icon/context/debloquer.ico");
            Image? imgDelete = LoadImageSafe("icon/context/suprimer.ico");

            _blockNoticeIcon = LoadImageSafe("icon/message/msgblock.png");
            _unblockNoticeIcon = LoadImageSafe("icon/message/msgunblock.png");

            _blockMenuItem = new ToolStripMenuItem("Bloquer");
            if (imgBlock != null) _blockMenuItem.Image = imgBlock;
            _blockMenuItem.Click += async (s, e) => await BlockSelectedFriend();
            _blockMenuItem.Enabled = false;

            _unblockMenuItem = new ToolStripMenuItem("Débloquer");
            if (imgUnblock != null) _unblockMenuItem.Image = imgUnblock;
            _unblockMenuItem.Click += async (s, e) => await UnblockSelectedFriend();
            _unblockMenuItem.Enabled = false;

            _deleteMenuItem = new ToolStripMenuItem("Supprimer");
            if (imgDelete != null) _deleteMenuItem.Image = imgDelete;
            _deleteMenuItem.Click += async (s, e) => await RemoveSelectedFriend();
            _deleteMenuItem.Enabled = false;
            
            // Add items to the existing contextMenu (which already has "Voir profil")
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(_blockMenuItem);
            contextMenu.Items.Add(_unblockMenuItem);
            contextMenu.Items.Add(_deleteMenuItem);
        }

        private static Image? LoadImageSafe(string resourcePath)
        {
            try
            {
                var image = ResourceImageStore.LoadImage(resourcePath);
                return image;
            }
            catch
            {
                return null;
            }
        }

        private bool TryAddGenderIcon(string key, string resourcePath)
        {
            var icon = LoadImageSafe(resourcePath);
            if (icon == null)
            {
                return false;
            }

            imgListGender.Images.Add(key, icon);
            return true;
        }

        private void TryAddStatusIcon(string resourcePath, string key)
        {
            var icon = LoadImageSafe(resourcePath);
            if (icon != null)
            {
                imgListStatus.Images.Add(key, icon);
            }
        }

        private void SetupStatusMenu()
        {
            // Create status dropdown button
            _statusButton = new ToolStripDropDownButton();
            _statusButton.Text = _currentUserStatus.GetDisplayName();
            _statusButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            
            // Set initial icon
            if (imgListStatus.Images.ContainsKey(_currentUserStatus.ToString()))
            {
                _statusButton.Image = imgListStatus.Images[_currentUserStatus.ToString()];
            }
            
            // Create menu items for each status
            var statuses = new[] 
            { 
                UserStatus.Online, 
                UserStatus.Away, 
                UserStatus.BRB, 
                UserStatus.DoNotDisturb, 
                UserStatus.Busy, 
                UserStatus.Offline 
            };
            
            foreach (var status in statuses)
            {
                var menuItem = new ToolStripMenuItem();
                menuItem.Text = status.GetDisplayName();
                menuItem.Tag = status;
                
                // Set icon from ImageList
                if (imgListStatus.Images.ContainsKey(status.ToString()))
                {
                    menuItem.Image = imgListStatus.Images[status.ToString()];
                }
                
                // Handle click event
                menuItem.Click += async (s, e) =>
                {
                    var selectedStatus = (UserStatus)((ToolStripMenuItem)s!).Tag!;
                    await ChangeStatusAsync(selectedStatus);
                    
                    // Update button appearance
                    UpdateStatusButtonDisplay(selectedStatus);
                };
                
                _statusButton.DropDownItems.Add(menuItem);
            }
            
            // Add button to toolbar/menu strip
            var toolStrip = new ToolStrip();
            toolStrip.Dock = DockStyle.Top;
            toolStrip.Items.Add(_statusButton);
            this.Controls.Add(toolStrip);
            toolStrip.BringToFront();
        }
        
        private void UpdateStatusButtonDisplay(UserStatus status)
        {
            if (_statusButton != null)
            {
                _statusButton.Text = status.GetDisplayName();
                if (imgListStatus.Images.ContainsKey(status.ToString()))
                {
                    _statusButton.Image = imgListStatus.Images[status.ToString()];
                }
            }
        }
        
        private async Task ChangeStatusAsync(UserStatus newStatus)
        {
            try
            {
                if (_activityMonitor != null)
                {
                    await _activityMonitor.UpdateStatusAsync(newStatus);
                    _currentUserStatus = newStatus;
                }
                else
                {
                    // Fallback: direct API call if monitor not initialized yet
                    var request = new { Status = newStatus.ToString() };
                    var json = JsonSerializer.Serialize(request);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    
                    var response = await _httpClient.PutAsync($"{ApiBaseUrl}/session/status", content);
                    if (response.IsSuccessStatusCode)
                    {
                        _currentUserStatus = newStatus;
                    }
                }

                // Always reflect change in UI status button
                UpdateStatusButtonDisplay(newStatus);
                // Propagate my status change to open chat windows
                foreach (var chat in _openChats.Values)
                {
                    try { chat.UpdateMyStatus(_currentUserStatus); } catch { }
                }
            }
            catch (Exception ex)
            {
                PalMessageBox.Show($"Erreur lors du changement de statut: {ex.Message}");
            }
        }

        // Public wrapper so other windows can change my status directly
        public async Task ChangeMyStatusAsync(UserStatus newStatus)
        {
            await ChangeStatusAsync(newStatus);
        }

        public async Task HangupActiveCallAsync()
        {
            if (_voiceCallService != null && _activeCallId != null)
            {
                await _voiceCallService.HangupAsync(_activeCallId);
            }
        }

        public async Task AcceptActiveCallAsync()
        {
            if (_voiceCallService != null && _activeCallId != null)
            {
                await _voiceCallService.AcceptAsync(_activeCallId);
            }
        }

        public Task SetCallMutedAsync(bool muted)
        {
            _voiceCallService?.SetMuted(muted);
            return Task.CompletedTask;
        }

        public void SetCallVolume(int percent)
        {
            _voiceCallService?.SetVolumePercent(percent);
        }

        private async Task SetInCallStatusAsync()
        {
            if (_currentUserStatus == UserStatus.InCall)
            {
                return;
            }

            _statusBeforeCall ??= _currentUserStatus;
            await ChangeStatusAsync(UserStatus.InCall);
        }

        private async Task RestoreStatusAfterCallAsync()
        {
            if (_statusBeforeCall.HasValue)
            {
                var target = _statusBeforeCall.Value;
                _statusBeforeCall = null;
                await ChangeStatusAsync(target);
            }
        }

        private async Task RemoveSelectedFriend()
        {
            if (lstFriends.SelectedItems.Count > 0)
            {
                var item = lstFriends.SelectedItems[0];
                if (item.Tag is UserProfileDto user)
                {
                    var confirm = PalMessageBox.Show($"Voulez-vous vraiment supprimer {user.FirstName} {user.LastName} de vos amis ?", 
                        "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        
                    if (confirm == DialogResult.Yes)
                    {
                        try
                        {
                            var response = await _httpClient.DeleteAsync($"{ApiBaseUrl}/friend/remove/{user.Id}");
                            if (response.IsSuccessStatusCode)
                            {
                                lstFriends.Items.Remove(item);
                            }
                            else
                            {
                                PalMessageBox.Show("Erreur lors de la suppression.");
                            }
                        }
                        catch (Exception ex)
                        {
                            PalMessageBox.Show($"Erreur: {ex.Message}");
                        }
                    }
                }
            }
        }

        private async Task BlockSelectedFriend()
        {
            var friend = GetSelectedFriend();
            if (friend == null || friend.IsPending || friend.IsBlocked)
            {
                return;
            }

            using var dialog = new BlockUserDialog(friend);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }
            var (success, error) = await TryApplyBlockAsync(friend.Id, dialog.Reason, dialog.IsPermanent, dialog.DurationDays, dialog.BlockedUntil);
            var displayName = GetUserDisplayName(friend);

            if (success)
            {
                PalMessageBox.Show($"{displayName} ne pourra plus vous écrire.",
                    "Blocage activé",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else if (!string.IsNullOrWhiteSpace(error))
            {
                PalMessageBox.Show($"Blocage impossible : {error}", "Blocage", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task UnblockSelectedFriend()
        {
            var friend = GetSelectedFriend();
            if (friend == null || !friend.IsBlocked)
            {
                return;
            }

            var displayName = $"{friend.FirstName} {friend.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = GetUserDisplayName(friend);
            }

            var confirm = PalMessageBox.Show($"Voulez-vous débloquer {displayName} ?", "Déblocage", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
            {
                return;
            }

            var (success, error) = await TryUnblockFriendAsync(friend.Id);
            if (success)
            {
                PalMessageBox.Show($"{displayName} peut à nouveau vous envoyer des messages.",
                    "Déblocage confirmé",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else if (!string.IsNullOrWhiteSpace(error))
            {
                PalMessageBox.Show($"Déblocage impossible : {error}", "Déblocage", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        internal async Task<(bool Success, string? Error)> TryUnblockFriendAsync(int userId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{ApiBaseUrl}/blocked/{userId}");
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return (false, error);
                }

                _blockedUsers.Remove(userId);
                await LoadBlockedUsers();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
            finally
            {
                RefreshChatBlockState(userId);
                UpdateContextMenuState();
            }
        }

        internal async Task<(bool Success, string? Error)> TryApplyBlockAsync(int userId, string? reason, bool isPermanent, int? durationDays, DateTime? blockedUntil)
        {
            try
            {
                var sanitizedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
                int? effectiveDuration = isPermanent ? null : durationDays;

                DateTime? effectiveUntil = null;
                if (!isPermanent && blockedUntil.HasValue)
                {
                    var value = blockedUntil.Value;
                    effectiveUntil = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
                }

                var payload = new
                {
                    Reason = sanitizedReason,
                    IsPermanent = isPermanent,
                    DurationDays = effectiveDuration,
                    BlockedUntil = effectiveUntil
                };

                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{ApiBaseUrl}/blocked/{userId}", content);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return (false, error);
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var dto = await response.Content.ReadFromJsonAsync<BlockedUserDto>(options);
                if (dto != null)
                {
                    _blockedUsers[dto.UserId] = dto;
                }

                await LoadBlockedUsers();
                RefreshChatBlockState(userId);
                UpdateContextMenuState();

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private void HandleSanctionNotification(SanctionNotificationPayload payload)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<SanctionNotificationPayload>(HandleSanctionNotification), payload);
                return;
            }

            if (payload == null)
            {
                return;
            }

            var notificationType = payload.NotificationType ?? string.Empty;
            var shouldDisplay = false;
            var blockerDisplayName = GetBlockerDisplayName(payload.BlockedByUserId);

            if (string.Equals(notificationType, "Block", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(notificationType, "BlockReminder", StringComparison.OrdinalIgnoreCase))
            {
                if (!_blockedByOthers.TryGetValue(payload.BlockedByUserId, out var state))
                {
                    state = new RemoteBlockState();
                    _blockedByOthers[payload.BlockedByUserId] = state;
                }

                state.IsBlocked = true;
                state.Message = payload.Message;
                state.IsPermanent = payload.IsPermanent;
                state.BlockedUntil = payload.BlockedUntil;
                state.Reason = payload.Reason;

                // Do not re-display if the user already acknowledged this block
                var alreadyAcknowledged = _acknowledgedBlockNotifications.Contains(payload.BlockedByUserId);
                state.NotificationShown |= alreadyAcknowledged;
                shouldDisplay = !state.NotificationShown;
            }
            else if (string.Equals(notificationType, "Unblock", StringComparison.OrdinalIgnoreCase))
            {
                _blockedByOthers.Remove(payload.BlockedByUserId);

                if (_acknowledgedBlockNotifications.Remove(payload.BlockedByUserId))
                {
                    PersistBlockAcknowledgements();
                }

                shouldDisplay = true;
            }

            RefreshChatBlockState(payload.BlockedByUserId);

            if (shouldDisplay)
            {
                var shown = DisplaySanctionToast(payload, blockerDisplayName);
                if (shown)
                {
                    if (_blockedByOthers.TryGetValue(payload.BlockedByUserId, out var state))
                    {
                        state.NotificationShown = true;
                    }

                    _acknowledgedBlockNotifications.Add(payload.BlockedByUserId);
                    PersistBlockAcknowledgements();
                }
            }
        }

        private void HandleBlockedStateChanged(int userId, bool isBlocked)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int, bool>(HandleBlockedStateChanged), userId, isBlocked);
                return;
            }

            if (isBlocked)
            {
                if (!_blockedByOthers.TryGetValue(userId, out var state))
                {
                    state = new RemoteBlockState();
                    _blockedByOthers[userId] = state;
                }

                state.IsBlocked = true;
            }
            else
            {
                _blockedByOthers.Remove(userId);
            }

            RefreshChatBlockState(userId);
        }

        private bool DisplaySanctionToast(SanctionNotificationPayload payload, string blockerDisplayName)
        {
            var notificationType = payload.NotificationType ?? string.Empty;

            if (string.Equals(notificationType, "Block", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(notificationType, "BlockReminder", StringComparison.OrdinalIgnoreCase))
            {
                var lines = new List<string>();
                lines.Add($"<b>{blockerDisplayName}</b>");
                lines.Add("Vous a bloqué.");

                if (!string.IsNullOrWhiteSpace(payload.Reason))
                {
                    lines.Add($"Motif : {payload.Reason.Trim()}");
                }

                if (payload.IsPermanent)
                {
                    lines.Add("Blocage permanent.");
                }
                else if (payload.BlockedUntil.HasValue)
                {
                    lines.Add($"Blocage temporaire jusqu'au {payload.BlockedUntil:dd/MM/yyyy HH:mm}.");
                }

                var message = string.Join(Environment.NewLine, lines);
                PalMessageBox.Show(message, "Blocage reçu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return true;
            }

            if (string.Equals(notificationType, "Unblock", StringComparison.OrdinalIgnoreCase))
            {
                PalMessageBox.Show($"<b>{blockerDisplayName}</b> vous a débloqué.", "Déblocage", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }

            return false;
        }

        private string GetBlockerDisplayName(int userId)
        {
            var item = FindFriendListViewItem(userId);
            if (item?.Tag is UserProfileDto friend)
            {
                return GetUserDisplayName(friend);
            }

            return $"Utilisateur #{userId}";
        }

        private string GetBlockAckFilePath()
        {
            return Path.Combine(Application.UserAppDataPath, "block_acknowledged.json");
        }

        private void LstFriends_DrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
        {
            if (e.Item == null)
            {
                return;
            }

            // Dessiner l'arrière-plan
            if (e.Item.Selected)
            {
                e.Graphics.FillRectangle(SystemBrushes.Highlight, e.Bounds);
            }
            else
            {
                e.Graphics.FillRectangle(SystemBrushes.Window, e.Bounds);
            }

            Font itemFont = lstFriends.Font;
            Color textColor = e.Item.Selected ? SystemColors.HighlightText : SystemColors.WindowText;
            bool isPending = false;
            bool isBlocked = false;
            bool isOnline = false;

            if (e.Item.Tag is UserProfileDto user)
            {
                isPending = user.IsPending;
                isBlocked = user.IsBlocked;
                isOnline = user.CurrentStatus != UserStatus.Offline;

                if (isPending)
                {
                    textColor = e.Item.Selected ? SystemColors.HighlightText : Color.Gray;
                }
                else if (isBlocked)
                {
                    itemFont = new Font(lstFriends.Font, FontStyle.Bold);
                    if (!e.Item.Selected)
                    {
                        textColor = Color.FromArgb(148, 32, 26);
                    }
                }
                else if (isOnline)
                {
                    itemFont = new Font(lstFriends.Font, FontStyle.Bold);
                }
            }

            if (e.ColumnIndex == 0)
            {
                if (e.Item.ImageIndex >= 0 && e.Item.ImageIndex < imgListGender.Images.Count)
                {
                    var image = imgListGender.Images[e.Item.ImageIndex];
                    e.Graphics.DrawImage(image, e.Bounds.Left + 2, e.Bounds.Top + (e.Bounds.Height - image.Height) / 2);
                }

                var textRect = new Rectangle(e.Bounds.Left + 20, e.Bounds.Top, e.Bounds.Width - 20, e.Bounds.Height);
                var itemText = e.Item.Text ?? string.Empty;
                TextRenderer.DrawText(e.Graphics, itemText, itemFont, textRect, textColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            }
            else if (e.ColumnIndex == 1 && e.SubItem != null)
            {
                var statusText = e.Item.Tag is UserProfileDto friend
                    ? GetStatusDisplay(friend)
                    : e.SubItem.Text;

                var textSize = TextRenderer.MeasureText(statusText ?? string.Empty, itemFont);
                var textRect = new Rectangle(e.Bounds.Left + 2, e.Bounds.Top, textSize.Width, e.Bounds.Height);

                TextRenderer.DrawText(e.Graphics, statusText, itemFont, textRect, textColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);

                if (!isPending && !isBlocked && imgListStatus != null && e.Item.Tag is UserProfileDto statusFriend)
                {
                    var statusKey = statusFriend.CurrentStatus.ToString();
                    if (imgListStatus.Images.ContainsKey(statusKey))
                    {
                        var image = imgListStatus.Images[statusKey];
                        if (image != null)
                        {
                            e.Graphics.DrawImage(image, e.Bounds.Left + textSize.Width + 5, e.Bounds.Top + (e.Bounds.Height - image.Height) / 2);
                        }
                    }
                }
            }

            if (!ReferenceEquals(itemFont, lstFriends.Font))
            {
                itemFont.Dispose();
            }
        }

        private void LstFriends_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (lstFriends.SelectedItems.Count > 0)
            {
                var item = lstFriends.SelectedItems[0];
                if (item.Tag is UserProfileDto user && user.IsPending)
                {
                    item.Selected = false;
                }
            }

            UpdateContextMenuState();
        }

        private void LstFriends_DoubleClick(object? sender, EventArgs e)
        {
            if (lstFriends.SelectedItems.Count > 0)
            {
                var item = lstFriends.SelectedItems[0];
                if (item.Tag is UserProfileDto user)
                {
                    if (user.IsPending) return;
                    OpenChatWindow(user);
                }
            }
        }

        public void OpenChatWindow(UserProfileDto user, bool ignoreDoNotDisturb = false)
        {
            if (!ignoreDoNotDisturb && user.CurrentStatus == UserStatus.DoNotDisturb)
            {
                var nameParts = new[] { user.FirstName, user.LastName }
                    .Where(part => !string.IsNullOrWhiteSpace(part))
                    .Select(part => part!.Trim());
                var friendlyName = string.Join(" ", nameParts);

                if (string.IsNullOrWhiteSpace(friendlyName) && !string.IsNullOrWhiteSpace(user.DisplayedName))
                {
                    friendlyName = user.DisplayedName.Trim();
                }

                if (string.IsNullOrWhiteSpace(friendlyName))
                {
                    friendlyName = "Cet ami";
                }

                PalMessageBox.Show(
                    $"<b>{friendlyName}</b> a activé le mode \"Ne pas déranger\" et préfère rester au calme pour l'instant. Merci de respecter sa tranquillité et de réessayer un peu plus tard.",
                    "Ne pas déranger",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (_openChats.ContainsKey(user.Id))
            {
                var chat = _openChats[user.Id];
                if (chat.IsDisposed)
                {
                    _openChats.Remove(user.Id);
                }
                else
                {
                    chat.BringToFront();
                    chat.Focus();
                    return;
                }
            }

            var form = new FormChat(user, _currentUser, this);
            form.FormClosed += (s, args) => _openChats.Remove(user.Id);
            _openChats.Add(user.Id, form);
            form.Show();
            RefreshChatBlockState(user.Id);
        }

        private bool IsChatOpen(int userId)
        {
            return _openChats.TryGetValue(userId, out var chat) && chat != null && !chat.IsDisposed;
        }

        private FormChat? EnsureChatWindow(int userId, string displayName)
        {
            if (_openChats.TryGetValue(userId, out var existing) && existing != null && !existing.IsDisposed)
            {
                existing.BringToFront();
                existing.Focus();
                return existing;
            }

            var item = FindFriendListViewItem(userId);
            var profile = item?.Tag as UserProfileDto ?? new UserProfileDto
            {
                Id = userId,
                DisplayedName = displayName,
                Username = displayName
            };

            OpenChatWindow(profile, ignoreDoNotDisturb: true);
            _openChats.TryGetValue(userId, out var opened);
            return opened;
        }

        public async Task SendPrivateMessage(ChatMessageDto msg)
        {
            if (_hubConnection != null)
            {
                await _hubConnection.InvokeAsync("SendPrivateMessage", msg);
            }
        }

        public async Task NotifyTyping(int recipientUserId, bool isTyping)
        {
            if (_hubConnection != null)
            {
                await _hubConnection.InvokeAsync("NotifyTyping", recipientUserId, isTyping);
            }
        }

        public async Task NotifyConversationDeletion(int recipientUserId)
        {
            if (_hubConnection != null)
            {
                await _hubConnection.InvokeAsync("NotifyConversationDeletion", recipientUserId);
            }
        }

        public async Task RespondConversationDeletion(int requesterId, bool accepted)
        {
            if (_hubConnection != null)
            {
                await _hubConnection.InvokeAsync("RespondConversationDeletion", requesterId, accepted);
            }
        }

        private void BtnSearchFriend_Click(object? sender, EventArgs e)
        {
            if (_formSearchFriend == null || _formSearchFriend.IsDisposed)
            {
                _formSearchFriend = new FormSearchFriend(_httpClient, ApiBaseUrl, this, _currentUser.Id);
                _formSearchFriend.Show();
            }
            else
            {
                if (_formSearchFriend.WindowState == FormWindowState.Minimized)
                    _formSearchFriend.WindowState = FormWindowState.Normal;
                _formSearchFriend.BringToFront();
                _formSearchFriend.Focus();
            }
        }

        private async Task CheckPendingRequests()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{ApiBaseUrl}/friendrequest/pending");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var requests = JsonSerializer.Deserialize<List<PendingRequestDto>>(json, options);

                    if (requests != null)
                    {
                        bool anyProcessed = false;
                        foreach (var req in requests)
                        {
                            var form = new FormFriendRequest(req.SenderName);
                            if (form.ShowDialog() == DialogResult.OK)
                            {
                                var payload = new { ResponseType = form.ResponseType, Reason = form.Reason };
                                var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
                                await _httpClient.PostAsync($"{ApiBaseUrl}/friendrequest/respond/{req.Id}", content);
                                anyProcessed = true;
                            }
                        }

                        if (anyProcessed)
                        {
                            await LoadFriends();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la vérification des demandes: {ex.Message}");
            }
        }

        public class PendingRequestDto
        {
            public int Id { get; set; }
            public int SenderId { get; set; }
            public string SenderName { get; set; } = "";
            public DateTime CreatedAt { get; set; }
        }

        public async Task AddFriend(UserProfileDto user)
        {
            // Vérifier si déjà présent
            foreach(ListViewItem existing in lstFriends.Items)
            {
                if (existing.Tag is UserProfileDto existingUser && existingUser.Id == user.Id)
                {
                    PalMessageBox.Show($"{user.FirstName} {user.LastName} est déjà dans votre liste d'amis.");
                    return;
                }
            }

            // Nouvelle logique : Envoyer une demande via API (Persistance)
            try
            {
                var response = await _httpClient.PostAsync($"{ApiBaseUrl}/friendrequest/send/{user.Id}", null);
                if (response.IsSuccessStatusCode)
                {
                    // Ajouter provisoirement à la liste
                    user.IsPending = true;
                    AddFriendToListView(user);
                    lstFriends.Sort();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    PalMessageBox.Show($"Erreur : {error}");
                }
            }
            catch (Exception ex)
            {
                PalMessageBox.Show($"Erreur lors de l'envoi de la demande : {ex.Message}");
            }
        }

        private void AddFriendToListView(UserProfileDto user)
        {
            // Prevent duplicates
            foreach (ListViewItem existingItem in lstFriends.Items)
            {
                if (existingItem.Tag is UserProfileDto existingUser && existingUser.Id == user.Id)
                {
                    // Update existing item
                    existingUser.IsPending = user.IsPending;
                    existingUser.IsBlocked = user.IsBlocked;
                    existingUser.IsOnline = user.IsOnline;

                    // Normaliser le statut si rien n'est fourni : si en ligne mais statut par défaut (Offline), le passer à Online
                    if (existingUser.CurrentStatus == UserStatus.Offline && user.IsOnline)
                    {
                        existingUser.CurrentStatus = UserStatus.Online;
                    }
                    else
                    {
                        existingUser.CurrentStatus = user.CurrentStatus;
                    }
                    
                    existingItem.Text = GetUserDisplayName(user);
                    ApplyFriendVisualState(existingItem);
                    return;
                }
            }

            var displayName = $"{user.FirstName} {user.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(displayName) && !string.IsNullOrWhiteSpace(user.DisplayedName))
            {
                displayName = user.DisplayedName;
            }

            var item = new ListViewItem(displayName);
            item.Tag = user;
            item.SubItems.Add(string.Empty);

            // Normaliser le statut si l'API renvoie IsOnline=true mais CurrentStatus par défaut (Offline)
            if (user.CurrentStatus == UserStatus.Offline && user.IsOnline)
            {
                user.CurrentStatus = UserStatus.Online;
            }

            ApplyFriendVisualState(item);
            lstFriends.Items.Add(item);
            lstFriends.Sort();
        }

        private void ApplyFriendVisualState(ListViewItem item)
        {
            if (item.Tag is not UserProfileDto user)
            {
                return;
            }

            if (user.IsPending)
            {
                var pendingIndex = imgListGender.Images.IndexOfKey("Pending");
                if (pendingIndex >= 0)
                {
                    item.ImageIndex = pendingIndex;
                }
            }
            else if (user.IsBlocked && _blockedIconIndex >= 0)
            {
                item.ImageIndex = _blockedIconIndex;
            }
            else
            {
                string genderKey = "Autre";
                if (string.Equals(user.Gender, "Homme", StringComparison.OrdinalIgnoreCase))
                {
                    genderKey = "Homme";
                }
                else if (string.Equals(user.Gender, "Femme", StringComparison.OrdinalIgnoreCase))
                {
                    genderKey = "Femme";
                }

                int imageIndex = imgListGender.Images.IndexOfKey(genderKey);
                if (imageIndex >= 0)
                {
                    item.ImageIndex = imageIndex;
                }
            }

            UpdateFriendStatusColumn(item);
        }

        private static string GetStatusDisplay(UserProfileDto user)
        {
            if (user.IsBlocked)
            {
                return "Bloqué";
            }

            if (user.IsPending)
            {
                return "En attente";
            }

            return user.CurrentStatus.GetDisplayName();
        }

        private void UpdateFriendStatusColumn(ListViewItem item)
        {
            if (item.Tag is not UserProfileDto user)
            {
                return;
            }

            if (item.SubItems.Count > 1)
            {
                item.SubItems[1].Text = GetStatusDisplay(user);
            }
            else
            {
                item.SubItems.Add(GetStatusDisplay(user));
            }
        }

        private UserProfileDto? GetSelectedFriend()
        {
            if (lstFriends.SelectedItems.Count == 0)
            {
                return null;
            }

            return lstFriends.SelectedItems[0].Tag as UserProfileDto;
        }

        private ListViewItem? FindFriendListViewItem(int userId)
        {
            foreach (ListViewItem item in lstFriends.Items)
            {
                if (item.Tag is UserProfileDto friend && friend.Id == userId)
                {
                    return item;
                }
            }

            return null;
        }

        private void RefreshChatBlockState(int userId)
        {
            if (!_openChats.TryGetValue(userId, out var chat) || chat == null || chat.IsDisposed)
            {
                return;
            }

            if (_blockedUsers.ContainsKey(userId))
            {
                chat.SetBlockedByMe(true);
            }
            else
            {
                chat.SetBlockedByMe(false);
            }

            if (_blockedByOthers.TryGetValue(userId, out var remoteState) && remoteState.IsBlocked)
            {
                chat.SetBlockedByRemote(true, remoteState.Message, remoteState.IsPermanent, remoteState.BlockedUntil, remoteState.Reason);
            }
            else
            {
                chat.SetBlockedByRemote(false, null, false, null, null);
            }
        }

        private static string GetUserDisplayName(UserProfileDto profile)
        {
            var parts = new[] { profile.FirstName?.Trim(), profile.LastName?.Trim() }
                .Where(p => !string.IsNullOrWhiteSpace(p));

            var joined = string.Join(" ", parts);
            if (!string.IsNullOrWhiteSpace(joined))
            {
                return joined;
            }

            if (!string.IsNullOrWhiteSpace(profile.DisplayedName))
            {
                return profile.DisplayedName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(profile.Username))
            {
                return profile.Username.Trim();
            }

            return $"Utilisateur #{profile.Id}";
        }

        internal Task LoadBlockedUsersSafeAsync()
        {
            return LoadBlockedUsers();
        }

        private void LoadBlockAcknowledgements()
        {
            try
            {
                var path = GetBlockAckFilePath();
                if (!File.Exists(path))
                {
                    return;
                }

                var json = File.ReadAllText(path);
                var ids = JsonSerializer.Deserialize<HashSet<int>>(json) ?? new HashSet<int>();

                _acknowledgedBlockNotifications.Clear();
                foreach (var id in ids)
                {
                    _acknowledgedBlockNotifications.Add(id);
                }
            }
            catch
            {
                // If anything goes wrong, fall back to in-memory only
            }
        }

        private void PersistBlockAcknowledgements()
        {
            try
            {
                var path = GetBlockAckFilePath();
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(_acknowledgedBlockNotifications);
                File.WriteAllText(path, json);
            }
            catch
            {
                // Ignore persistence errors; notifications will simply reappear on next session
            }
        }

        private void UpdateContextMenuState()
        {
            if (_blockMenuItem == null || _unblockMenuItem == null)
            {
                return;
            }

            var selectedFriend = GetSelectedFriend();
            if (selectedFriend == null)
            {
                _blockMenuItem.Enabled = false;
                _unblockMenuItem.Enabled = false;
                if (_deleteMenuItem != null)
                {
                    _deleteMenuItem.Enabled = false;
                }
                return;
            }

            var isPending = selectedFriend.IsPending;
            _blockMenuItem.Enabled = !isPending && !selectedFriend.IsBlocked;
            _unblockMenuItem.Enabled = selectedFriend.IsBlocked;

            if (_deleteMenuItem != null)
            {
                _deleteMenuItem.Enabled = !isPending;
            }
        }

        // Ancienne méthode (gardée pour référence ou suppression future)
        /*
        public async Task AddFriendDirect(UserProfileDto user)
        {
            try
            {
                var response = await _httpClient.PostAsync($"{ApiBaseUrl}/friend/add/{user.Id}", null);
                // ...
            }
            catch (Exception ex)
            {
                PalMessageBox.Show($"Erreur : {ex.Message}");
            }
        }
        */

        private async Task StartCallWithSelectedFriend()
        {
            if (_voiceCallService == null)
            {
                PalMessageBox.Show("Le service d'appel n'est pas prêt.");
                return;
            }

            if (lstFriends.SelectedItems.Count == 0)
            {
                return;
            }

            if (lstFriends.SelectedItems[0].Tag is not UserProfileDto user)
            {
                return;
            }

            _activeCallPeerId = user.Id;

            try

            {
                await _voiceCallService.InviteAsync(user.Id);
            }
            catch (Exception ex)
            {
                PalMessageBox.Show($"Impossible de lancer l'appel : {ex.Message}");
            }
        }

        private async Task StartVideoCallWithSelectedFriend()
        {
            if (lstFriends.SelectedItems.Count == 0) return;
            if (lstFriends.SelectedItems[0].Tag is not UserProfileDto user) return;
            
            await StartVideoCallInternalAsync(user.Id);
        }
        private async Task LoadFriends()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{ApiBaseUrl}/friend");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var friends = JsonSerializer.Deserialize<List<UserProfileDto>>(json, options);
                    
                    lstFriends.Items.Clear();
                    UpdateContextMenuState();
                    if (friends != null)
                    {
                        // Charger les statuts actifs de tous les amis
                        await LoadFriendsStatuses(friends);
                        
                        foreach (var friend in friends)
                        {
                            friend.IsBlocked = _blockedUsers.ContainsKey(friend.Id);
                            AddFriendToListView(friend);
                        }

                        UpdateContextMenuState();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur chargement amis: {ex.Message}");
            }
        }

        private async Task LoadBlockedUsers()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{ApiBaseUrl}/blocked");
                if (!response.IsSuccessStatusCode)
                {
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var blocked = JsonSerializer.Deserialize<List<BlockedUserDto>>(json, options) ?? new List<BlockedUserDto>();

                void Apply()
                {
                    _blockedUsers.Clear();
                    foreach (var entry in blocked)
                    {
                        _blockedUsers[entry.UserId] = entry;
                    }

                    foreach (ListViewItem item in lstFriends.Items)
                    {
                        if (item.Tag is UserProfileDto friend)
                        {
                            friend.IsBlocked = _blockedUsers.ContainsKey(friend.Id);
                            ApplyFriendVisualState(item);
                        }
                    }

                    lstFriends.Invalidate();
                    UpdateContextMenuState();
                    _blockedUsersForm?.UpdateList(_blockedUsers.Values);
                }

                if (InvokeRequired)
                {
                    Invoke((MethodInvoker)Apply);
                }
                else
                {
                    Apply();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur chargement utilisateurs bloqués: {ex.Message}");
            }
        }
        
        private async Task LoadFriendsStatuses(List<UserProfileDto> friends)
        {
            try
            {
                // Récupérer tous les utilisateurs en ligne avec leurs statuts depuis l'API
                // Add timestamp to prevent caching
                var response = await _httpClient.GetAsync($"{ApiBaseUrl}/session/online-users?t={DateTime.UtcNow.Ticks}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    
                    // Use Dictionary<string, string> to avoid key conversion issues with System.Text.Json
                    var onlineUsersRaw = JsonSerializer.Deserialize<Dictionary<string, string>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (onlineUsersRaw != null)
                    {
                        // Convert keys to int
                        var onlineUsers = new Dictionary<int, string>();
                        foreach(var kvp in onlineUsersRaw)
                        {
                            if (int.TryParse(kvp.Key, out int userId))
                            {
                                onlineUsers[userId] = kvp.Value;
                            }
                        }

                        foreach (var friend in friends)
                        {
                            if (onlineUsers.TryGetValue(friend.Id, out var statusStr))
                            {
                                if (Enum.TryParse<UserStatus>(statusStr, true, out var status))
                                {
                                    friend.CurrentStatus = status;
                                    friend.IsOnline = status != UserStatus.Offline;
                                }
                                else
                                {
                                    // Si présent dans la liste en ligne mais parsing impossible, considérer l'utilisateur comme en ligne
                                    friend.CurrentStatus = UserStatus.Online;
                                    friend.IsOnline = true;
                                }
                            }
                            else
                            {
                                // Si l'API friends a déjà indiqué IsOnline=true mais n'est pas encore dans la liste online, favoriser Online
                                if (friend.IsOnline)
                                {
                                    friend.CurrentStatus = UserStatus.Online;
                                }
                                else
                                {
                                    friend.CurrentStatus = UserStatus.Offline;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur chargement statuts amis: {ex.Message}");
            }
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            // Afficher les informations de l'utilisateur (Custom Header)
            SetupCustomHeader();
            LoadBlockAcknowledgements();
            
            await LoadBlockedUsers();
            
            // Charger la liste d'amis
            await LoadFriends();
            
            // Initialiser SignalR
            await InitializeSignalR();

            // Vérifier l'état du service
            await CheckServiceStatus();
            
            // Se connecter au service si disponible
            if (_serviceAvailable)
            {
                await RegisterConnection();
            }
            
            // Vérifier les demandes d'amis en attente (Persistance)
            await CheckPendingRequests();
            
            // Initialiser le moniteur d'activité
            InitializeActivityMonitor();
            
            // Broadcaster le statut initial (Online ou Offline selon mode invisible)
            await BroadcastInitialStatus();
            
            // Démarrer le timer pour vérifier périodiquement l'état du service (fallback)
            timerConnectionCheck.Start();
        }
        
        private void InitializeActivityMonitor()
        {
            _activityMonitor = new UserActivityMonitor(_httpClient);
            _activityMonitor.StatusChanged += OnUserStatusChanged;
        }
        
        private void OnUserStatusChanged(object? sender, UserStatus newStatus)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnUserStatusChanged(sender, newStatus)));
                return;
            }
            
            _currentUserStatus = newStatus;
            System.Diagnostics.Debug.WriteLine($"User status changed to: {newStatus.GetDisplayName()}");
        }
        
        private async Task BroadcastInitialStatus()
        {
            try
            {
                // Récupérer le statut initial depuis la session active
                var response = await _httpClient.GetAsync($"{ApiBaseUrl}/session/current");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var session = JsonSerializer.Deserialize<SessionDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (session != null)
                    {
                        _currentUserStatus = session.DisplayedStatus;
                        
                        // Mettre à jour le menu de statut avec le statut réel
                        UpdateStatusButtonDisplay(_currentUserStatus);
                    }
                }
                
                // Le broadcast est automatiquement géré par le Hub lors de la connexion SignalR
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur chargement statut initial: {ex.Message}");
            }
        }

        private async Task InitializeSignalR()
        {
            try
            {
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(HubUrl, options =>
                    {
                        options.AccessTokenProvider = () => Task.FromResult<string?>(_authToken);
                        // Ignorer les erreurs de certificat SSL en développement
                        options.HttpMessageHandlerFactory = (handler) =>
                        {
                            if (handler is HttpClientHandler clientHandler)
                            {
                                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                            }
                            return handler;
                        };
                    })
                    .WithAutomaticReconnect()
                    .Build();

                _hubConnection.On<bool>("ServiceStatusChanged", (isRunning) =>
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        if (IsLogout) return;

                        _serviceAvailable = isRunning;
                        if (isRunning)
                        {
                            lblConnectionStatus.Text = $"Statut: Service actif ✓ (SignalR)";
                            lblConnectionStatus.ForeColor = System.Drawing.Color.Green;
                        }
                        else
                        {
                            IsLogout = true; // Empêcher d'autres déclenchements
                            timerConnectionCheck.Stop(); // Arrêter le timer immédiatement
                            
                            lblConnectionStatus.Text = $"Statut: Service arrêté par l'admin ✗ (SignalR)";
                            lblConnectionStatus.ForeColor = System.Drawing.Color.Red;
                            
                            PalMessageBox.Show("Le service a été arrêté par l'administrateur.", "Service arrêté", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            Logout().ConfigureAwait(false);
                        }
                    });
                });

                _hubConnection.On("ForceDisconnect", async () =>
                {
                    // 1. Déconnexion effective (arrêt SignalR, timer, etc.)
                    if (IsLogout) return;
                    IsLogout = true;
                    timerConnectionCheck.Stop();

                    // Arrêter SignalR proprement
                    if (_hubConnection != null)
                    {
                        await _hubConnection.StopAsync();
                    }

                    this.Invoke((MethodInvoker)delegate
                    {
                        lblConnectionStatus.Text = $"Statut: Déconnecté par l'admin ✗ (SignalR)";
                        lblConnectionStatus.ForeColor = System.Drawing.Color.Red;
                        
                        // 2. Affichage du message
                        PalMessageBox.Show("Vous avez été déconnecté par l'administrateur.\nVeuillez vous reconnecter.", 
                            "Déconnexion forcée", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        
                        // 3. Fermeture / Redirection
                        this.Close();
                    });
                });

                // Handle status change notifications from other users
                _hubConnection.On<int, string, string>("UserStatusChanged", (userId, username, status) =>
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        // Parse status string to enum
                        if (!Enum.TryParse<UserStatus>(status, true, out var newStatus))
                        {
                            // Si on reçoit une valeur inattendue mais que l'événement provient de l'utilisateur, considérer En ligne
                            newStatus = UserStatus.Online;
                        }
                        
                        // DEBUG: Log received status
                        System.Diagnostics.Debug.WriteLine($"[UserStatusChanged] UserId={userId}, Username={username}, Status={status}, Parsed={newStatus}");
                        
                        // Update friend status in ListView
                        foreach (ListViewItem item in lstFriends.Items)
                        {
                            if (item.Tag is UserProfileDto friend && friend.Id == userId)
                            {
                                // Update friend's status
                                friend.CurrentStatus = newStatus;
                                friend.IsOnline = newStatus != UserStatus.Offline;
                                
                                // DEBUG: Log update
                                System.Diagnostics.Debug.WriteLine($"[UserStatusChanged] Updated friend: CurrentStatus={friend.CurrentStatus}, IsOnline={friend.IsOnline}");
                                
                                // Update ListView subitem text
                                if (item.SubItems.Count > 1)
                                {
                                    item.SubItems[1].Text = newStatus.GetDisplayName();
                                }
                                
                                // Refresh ListView to show updated icon
                                lstFriends.Invalidate();
                                break;
                            }
                        }
                        
                        // Update open chat windows
                        if (_openChats.ContainsKey(userId))
                        {
                            var chat = _openChats[userId];
                            if (!chat.IsDisposed)
                            {
                                chat.UpdateUserStatus(newStatus);
                            }
                        }

                        // Update my profile window if it's me
                        if (_openProfileMe != null && !_openProfileMe.IsDisposed && _currentUser.Id == userId)
                        {
                            _openProfileMe.UpdateStatus(newStatus);
                        }

                        // Update open friend profile window
                        if (_openProfilesFriends.TryGetValue(userId, out var friendProfileWin) && friendProfileWin != null && !friendProfileWin.IsDisposed)
                        {
                            friendProfileWin.UpdateStatus(newStatus);
                        }
                    });
                });

                _hubConnection.On<ChatMessageDto>("ReceivePrivateMessage", (msg) =>
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        // If the message is from me, route it to the chat with the receiver and mark as outgoing
                        if (msg.SenderId == _currentUser.Id)
                        {
                            if (_openChats.TryGetValue(msg.ReceiverId, out var myChat) && myChat != null && !myChat.IsDisposed)
                            {
                                myChat.AppendMessage(msg, true);
                                myChat.BringToFront();
                                return;
                            }
                        }

                        // Standard incoming flow (from another user)
                        if (_openChats.ContainsKey(msg.SenderId))
                        {
                            var chat = _openChats[msg.SenderId];
                            if (!chat.IsDisposed)
                            {
                                chat.AppendMessage(msg, false);
                                chat.BringToFront();
                                return;
                            }
                            else
                            {
                                _openChats.Remove(msg.SenderId);
                            }
                        }

                        // If not open, open it
                        var senderProfile = new UserProfileDto
                        {
                            Id = msg.SenderId,
                            FirstName = msg.SenderName,
                            LastName = "",
                            DisplayedName = msg.SenderName,
                            Username = msg.SenderName,
                            IsOnline = true
                        };
                        
                        // Try to find in friend list to get full details
                        foreach(ListViewItem item in lstFriends.Items)
                        {
                            if (item.Tag is UserProfileDto friend && friend.Id == msg.SenderId)
                            {
                                senderProfile = friend;
                                break;
                            }
                        }

                        OpenChatWindow(senderProfile, ignoreDoNotDisturb: true);
                        // Don't append message here - it's already loaded in the history when the window opens
                        // _openChats[msg.SenderId].AppendMessage(msg, false);
                    });
                });

                // Handle message saved confirmation with correct MessageId
                _hubConnection.On<ChatMessageDto>("MessageSaved", (msg) =>
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        // Update the local message with the correct ID from database
                        if (_openChats.ContainsKey(msg.ReceiverId))
                        {
                            var chat = _openChats[msg.ReceiverId];
                            if (!chat.IsDisposed)
                            {
                                chat.UpdateMessageId(msg);
                            }
                        }
                    });
                });

                _hubConnection.On<int, string, bool>("UserTyping", (userId, username, isTyping) =>
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        // Find if chat window is open for this user
                        if (_openChats.ContainsKey(userId))
                        {
                            var chat = _openChats[userId];
                            if (!chat.IsDisposed)
                            {
                                chat.ShowTypingIndicator(username, isTyping);
                            }
                        }
                    });
                });

                _hubConnection.On<SanctionNotificationPayload>("SanctionNotification", payload =>
                {
                    HandleSanctionNotification(payload);
                });

                _hubConnection.On<int, bool>("BlockedStateChanged", (blockerUserId, isBlocked) =>
                {
                    HandleBlockedStateChanged(blockerUserId, isBlocked);
                });

                _hubConnection.On("BlockedUsersUpdated", async () =>
                {
                    await LoadBlockedUsers();
                });

                _hubConnection.On<int, bool>("BlockedUserStateChanged", (blockedUserId, isBlocked) =>
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        if (_blockedUsers.TryGetValue(blockedUserId, out var entry))
                        {
                            if (!isBlocked)
                            {
                                _blockedUsers.Remove(blockedUserId);
                            }
                        }

                        var item = FindFriendListViewItem(blockedUserId);
                        if (item?.Tag is UserProfileDto friend)
                        {
                            friend.IsBlocked = isBlocked;
                            ApplyFriendVisualState(item);
                        }

                        UpdateContextMenuState();
                        RefreshChatBlockState(blockedUserId);
                    });
                });

                _hubConnection.On<int, string>("ConversationDeletionRequest", (userId, username) =>
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        var result = MessageBox.Show(
                            $"{username} a supprimé votre conversation de son côté.\n\n" +
                            "Voulez-vous également la supprimer de votre côté ?",
                            "Suppression de conversation",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);

                        bool accepted = (result == DialogResult.Yes);
                        
                        if (accepted)
                        {
                            // Clear chat window if open
                            if (_openChats.ContainsKey(userId))
                            {
                                var chat = _openChats[userId];
                                if (!chat.IsDisposed)
                                {
                                    chat.ClearConversation();
                                }
                            }
                            
                            // Delete from database
                            _ = _httpClient.DeleteAsync($"{ApiBaseUrl}/chat/conversation/{userId}?localOnly=true");
                        }

                        // Send response
                        _ = RespondConversationDeletion(userId, accepted);
                    });
                });

                _hubConnection.On<bool>("ConversationDeletionResponse", (accepted) =>
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        if (accepted)
                        {
                            PalMessageBox.Show("Le destinataire a également supprimé la conversation de son côté.");
                        }
                        else
                        {
                            PalMessageBox.Show("Le destinataire a choisi de conserver la conversation de son côté.");
                        }
                    });
                });

                _hubConnection.On<int>("ProfileUpdated", async (userId) =>
                {
                    bool isFriend = false;
                    foreach (ListViewItem item in lstFriends.Items)
                    {
                        if (item.Tag is UserProfileDto user && user.Id == userId)
                        {
                            isFriend = true;
                            break;
                        }
                    }

                    bool isChatOpen = _openChats.ContainsKey(userId);

                    if (isFriend || isChatOpen)
                    {
                        try 
                        {
                            var response = await _httpClient.GetAsync($"{ApiBaseUrl}/profile/{userId}");
                            if (response.IsSuccessStatusCode)
                            {
                                var json = await response.Content.ReadAsStringAsync();
                                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                                var updatedProfile = JsonSerializer.Deserialize<UserProfileDto>(json, options);

                                if (updatedProfile == null)
                                {
                                    return;
                                }

                                var profile = updatedProfile;

                                this.Invoke((MethodInvoker)delegate
                                {
                                    foreach (ListViewItem item in lstFriends.Items)
                                    {
                                        if (item.Tag is UserProfileDto user && user.Id == userId)
                                        {
                                            user.FirstName = profile.FirstName;
                                            user.LastName = profile.LastName;
                                            user.ProfilePicture = profile.ProfilePicture;
                                            user.Gender = profile.Gender;
                                            item.Text = $"{user.FirstName} {user.LastName}".Trim();
                                            break;
                                        }
                                    }

                                    if (_openChats.TryGetValue(userId, out var chat) && chat != null && !chat.IsDisposed)
                                    {
                                        chat.UpdateProfile(profile);
                                    }
                                });
                            }
                        }
                        catch { }
                    }
                });

                _hubConnection.On<int, bool>("FriendStatusChanged", (friendId, isOnline) =>
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        foreach (ListViewItem item in lstFriends.Items)
                        {
                            if (item.Tag is UserProfileDto user && user.Id == friendId)
                            {
                                user.IsOnline = isOnline;
                                item.SubItems[1].Text = isOnline ? "En ligne" : "Hors ligne";
                                break;
                            }
                        }
                        lstFriends.Sort();
                    });
                });

                _hubConnection.On<int, string>("ReceiveFriendRequest", (requesterId, requesterName) =>
                {
                    this.Invoke((MethodInvoker)async delegate
                    {
                        // Refresh pending requests from DB to get the ID and details properly
                        await CheckPendingRequests();
                    });
                });

                _hubConnection.On<string, string, string>("ReceiveFriendResponse", (responderName, responseType, reason) =>
                {
                    this.Invoke((MethodInvoker)async delegate
                    {
                        // Update local list based on response
                        foreach (ListViewItem item in lstFriends.Items)
                        {
                            if (item.Tag is UserProfileDto user && (user.FirstName + " " + user.LastName) == responderName) // Ideally use ID
                            {
                                if (responseType == "Accept" || responseType == "AcceptAdd")
                                {
                                    user.IsPending = false;
                                    // Update Icon
                                    string genderKey = "Autre";
                                    if (string.Equals(user.Gender, "Homme", StringComparison.OrdinalIgnoreCase)) genderKey = "Homme";
                                    else if (string.Equals(user.Gender, "Femme", StringComparison.OrdinalIgnoreCase)) genderKey = "Femme";
                                    int imageIndex = imgListGender.Images.IndexOfKey(genderKey);
                                    if (imageIndex >= 0) item.ImageIndex = imageIndex;
                                    
                                    // Don't set text here, let LoadFriends handle it
                                    // item.SubItems[1].Text = user.IsOnline ? "En ligne" : "Hors ligne";
                                }
                                else if (responseType == "Refuse")
                                {
                                    lstFriends.Items.Remove(item);
                                }
                                break;
                            }
                        }
                        lstFriends.Sort();

                        var form = new FormFriendResponse(responderName, responseType, reason);
                        form.ShowDialog();

                        // Rafraîchir immédiatement la liste/les statuts au cas où le signal "RefreshFriendList" arriverait en retard
                        await LoadFriends();
                    });
                });

                _hubConnection.On("RefreshFriendList", () =>
                {
                    this.Invoke((MethodInvoker)async delegate
                    {
                        await LoadFriends();
                    });
                });

                InitializeVoiceLayer();

                await _hubConnection.StartAsync();
                Console.WriteLine($"SignalR connecté. ID: {_hubConnection.ConnectionId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur de connexion SignalR: {ex.Message}");
            }
        }

        private void InitializeVoiceLayer()
        {
            _voiceCallService = new VoiceCallService(_hubConnection, _httpClient, ApiBaseUrl, _currentUser.Id);

            _voiceCallService.IncomingCall += dto => SafeInvoke(() => HandleIncomingCall(dto));
            _voiceCallService.OutgoingCall += dto => SafeInvoke(() => HandleOutgoingCall(dto));
            _voiceCallService.CallAccepted += dto => SafeInvoke(() => HandleCallAccepted(dto));
            _voiceCallService.CallRejected += dto => SafeInvoke(() => HandleCallRejected(dto));
            _voiceCallService.CallEnded += dto => SafeInvoke(() => HandleCallEnded(dto));

            _videoCallService = new VideoCallService(_hubConnection, _httpClient, ApiBaseUrl, _currentUser.Id);
            _videoCallService.IncomingCall += dto => SafeInvoke(() => HandleIncomingVideoCall(dto));
            _videoCallService.OutgoingCall += dto => SafeInvoke(() => HandleOutgoingVideoCall(dto));
            _videoCallService.CallAccepted += dto => SafeInvoke(() => HandleVideoCallAccepted(dto));
            _videoCallService.CallRejected += dto => SafeInvoke(() => HandleVideoCallRejected(dto));
            _videoCallService.CallEnded += dto => SafeInvoke(() => HandleVideoCallEnded(dto));
            _videoCallService.RtcSignalReceived += dto => SafeInvoke(() => HandleVideoRtcSignal(dto));
        }

        private void HandleIncomingVideoCall(CallInviteDto dto)
        {
            _activeVideoCallId = dto.CallId;
            _activeVideoCallPeerId = dto.FromUserId;
            _activeVideoCallStartedAt = null;

            _activeVideoCallForm?.Close();

            var displayName = ResolveDisplayName(dto.FromUserId, dto.FromName);
            _activeVideoCallForm = new VideoCallForm(displayName, incoming: true);
            WireVideoCallForm(_activeVideoCallForm, dto.CallId, dto.FromUserId, displayName);
            _activeVideoCallForm.SetStatus("Appel vidéo entrant");
            _activeVideoCallForm.Show();
        }

        private void HandleOutgoingVideoCall(CallInviteDto dto)
        {
            _activeVideoCallId = dto.CallId;
            _activeVideoCallPeerId = dto.ToUserId;
            _activeVideoCallStartedAt = null;

            _activeVideoCallForm?.Close();

            var displayName = ResolveDisplayName(dto.ToUserId, $"Utilisateur {dto.ToUserId}");
            _activeVideoCallForm = new VideoCallForm(displayName, incoming: false);
            WireVideoCallForm(_activeVideoCallForm, dto.CallId, dto.ToUserId, displayName);
            _activeVideoCallForm.SetStatus("Appel vidéo en cours…");
            _activeVideoCallForm.Show();

            _ = SetInCallStatusAsync();
        }

        private void HandleVideoCallAccepted(CallAcceptDto dto)
        {
            if (_activeVideoCallForm != null && _activeVideoCallId == dto.CallId)
            {
                _activeVideoCallForm.SwitchToInCallMode();
                _activeVideoCallForm.SetStatus("Connecté");

                // Caller is initiator (ToUserId == callerId in our Accept payload)
                var isInitiator = dto.ToUserId == _currentUser.Id;
                _ = _activeVideoCallForm.StartWebRtcAsync(isInitiator);
            }

            _activeVideoCallStartedAt = DateTime.UtcNow;
            _ = SetInCallStatusAsync();

            // Message auto: seulement l'appelant (ToUserId) l'envoie pour éviter les doublons
            if (dto.ToUserId == _currentUser.Id)
            {
                _ = SendVideoCallEventMessageAsync(_activeVideoCallPeerId, "started");
            }
        }

        private void HandleVideoRtcSignal(VideoRtcSignalDto dto)
        {
            if (_activeVideoCallForm == null)
            {
                return;
            }

            if (_activeVideoCallId == null || !string.Equals(_activeVideoCallId, dto.CallId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _ = _activeVideoCallForm.ApplyRemoteSignalAsync(dto.SignalType, dto.Payload);
        }

        private void HandleVideoCallRejected(CallRejectDto dto)
        {
            var peer = dto.FromUserId == _currentUser.Id ? dto.ToUserId : dto.FromUserId;
            var reason = dto.Reason?.ToLowerInvariant() ?? string.Empty;
            var displayName = ResolveDisplayName(peer, $"Utilisateur {peer}");

            if (reason == "blocked")
            {
                PalMessageBox.Show("Vous ne pouvez pas appeler ce contact car il vous a bloqué.", "Appel vidéo bloqué", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _activeVideoCallForm?.Close();
                _activeVideoCallForm = null;
                _activeVideoCallId = null;
                _activeVideoCallStartedAt = null;
                _ = RestoreStatusAfterCallAsync();
                return;
            }

            if (reason == "videoincall")
            {
                PalMessageBox.Show($"{displayName} est occupé avec un autre appel vidéo.", "Appel vidéo occupé", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _activeVideoCallForm?.SetStatus("Destinataire en appel vidéo");
                _activeVideoCallStartedAt = null;
                _ = RestoreStatusAfterCallAsync();

                if (_activeVideoCallId == null || _activeVideoCallId == dto.CallId)
                {
                    _activeVideoCallForm?.Close();
                    _activeVideoCallForm = null;
                    _activeVideoCallId = null;
                }
                return;
            }

            if (reason == "incall" || reason == "in_call")
            {
                PalMessageBox.Show("Le destinataire est en appel, veuillez réessayer plus tard.", "Appel occupé", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _activeVideoCallForm?.SetStatus("Destinataire en appel");
                _activeVideoCallStartedAt = null;
                _ = RestoreStatusAfterCallAsync();

                if (_activeVideoCallId == null || _activeVideoCallId == dto.CallId)
                {
                    _activeVideoCallForm?.Close();
                    _activeVideoCallForm = null;
                    _activeVideoCallId = null;
                }
                return;
            }

            if (_activeVideoCallId == null || _activeVideoCallId == dto.CallId)
            {
                _activeVideoCallForm?.Close();
                _activeVideoCallForm = null;
                _activeVideoCallId = null;
                PalMessageBox.Show("Appel vidéo refusé ou indisponible");
            }

            _activeVideoCallStartedAt = null;
            _ = RestoreStatusAfterCallAsync();
        }

        private void HandleVideoCallEnded(CallEndDto dto)
        {
            if (_activeVideoCallId == dto.CallId)
            {
                _activeVideoCallForm?.Close();
                _activeVideoCallForm = null;
                _activeVideoCallId = null;
            }

            var peer = dto.FromUserId == _currentUser.Id ? dto.ToUserId : dto.FromUserId;

            // Message auto: seulement l'utilisateur qui a raccroché l'envoie
            if (dto.FromUserId == _currentUser.Id)
            {
                _ = SendVideoCallEventMessageAsync(peer, "ended");
            }

            _activeVideoCallStartedAt = null;
            _ = RestoreStatusAfterCallAsync();
        }

        private void WireVideoCallForm(VideoCallForm form, string callId, int peerUserId, string peerDisplayName)
        {
            async void SendRtc(object? _, VideoCallForm.RtcSignalToSendEventArgs e)
            {
                try
                {
                    if (_videoCallService != null)
                    {
                        await _videoCallService.SendRtcSignalAsync(callId, e.SignalType, e.Payload);
                    }
                }
                catch
                {
                    // ignore transient send errors
                }
            }

            form.RtcSignalToSend += SendRtc;

            form.HangupRequested += async (_, __) =>
            {
                if (_videoCallService != null)
                {
                    await _videoCallService.HangupAsync(callId);
                }
                form.Close();
                _activeVideoCallStartedAt = null;
                _ = RestoreStatusAfterCallAsync();
            };

            form.AcceptRequested += async (_, __) =>
            {
                if (_videoCallService != null)
                {
                    await _videoCallService.AcceptAsync(callId);
                }
                form.SetStatus("Connexion…");
            };

            form.RejectRequested += async (_, __) =>
            {
                if (_videoCallService != null)
                {
                    await _videoCallService.RejectAsync(callId, "refused");
                }
                form.Close();
                _activeVideoCallStartedAt = null;
                _ = RestoreStatusAfterCallAsync();
            };

            form.FormClosed += (_, __) =>
            {
                form.RtcSignalToSend -= SendRtc;

                if (_activeVideoCallForm == form)
                {
                    _activeVideoCallForm = null;
                }
            };
        }

        private async Task SendVideoCallEventMessageAsync(int peerUserId, string eventType)
        {
            try
            {
                if (_hubConnection == null)
                {
                    return;
                }

                var payload = new VideoCallEventDto
                {
                    EventType = eventType,
                    AtUtc = DateTime.UtcNow
                };

                var msg = new ChatMessageDto
                {
                    MessageId = 0,
                    SenderId = _currentUser.Id,
                    SenderName = _currentUser.Username ?? "Moi",
                    ReceiverId = peerUserId,
                    Content = JsonSerializer.Serialize(payload),
                    ContentType = "video_call_event",
                    Timestamp = DateTime.UtcNow,
                    IsEdited = false
                };

                await _hubConnection.InvokeAsync("SendPrivateMessage", msg);
            }
            catch
            {
                // ignore
            }
        }

        private void SafeInvoke(Action action)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(action));
            }
            else
            {
                action();
            }
        }

        private void HandleIncomingCall(CallInviteDto dto)
        {
            _activeCallId = dto.CallId;
            _activeCallPeerId = dto.FromUserId;
            _activeCallStartedAt = null;

            _activeCallForm?.Close();

            var displayName = ResolveDisplayName(dto.FromUserId, dto.FromName);
            if (IsChatOpen(dto.FromUserId))
            {
                UpdateChatCallState(dto.FromUserId, VoiceCallUiState.InCall, "Appel entrant");
                _activeCallForm = null;
            }
            else
            {
                _activeCallForm = new VoiceCallForm(displayName, VoiceIcons.IncomingIcon, incoming: true);
                WireCallForm(_activeCallForm, dto.CallId, dto.FromUserId, displayName);
                _activeCallForm.SetStatus("Appel entrant");
                _activeCallForm.Show();
            }

            UpdateChatCallState(dto.FromUserId, VoiceCallUiState.InCall, "Appel entrant");
        }

        private void HandleOutgoingCall(CallInviteDto dto)
        {
            _activeCallId = dto.CallId;
            _activeCallPeerId = dto.ToUserId;
            _activeCallStartedAt = null;

            _activeCallForm?.Close();
            var displayName = ResolveDisplayName(dto.ToUserId, $"Utilisateur {dto.ToUserId}");
            if (IsChatOpen(dto.ToUserId))
            {
                UpdateChatCallState(dto.ToUserId, VoiceCallUiState.InCall, "Appel en cours…");
                _activeCallForm = null;
            }
            else
            {
                _activeCallForm = new VoiceCallForm(displayName, VoiceIcons.MicOffIcon, incoming: false);
                WireCallForm(_activeCallForm, dto.CallId, dto.ToUserId, displayName);
                _activeCallForm.SetStatus("Appel en cours…");
                _activeCallForm.Show();
            }

            UpdateChatCallState(dto.ToUserId, VoiceCallUiState.InCall, "Appel en cours…");
            _ = SetInCallStatusAsync();
        }

        private void HandleCallAccepted(CallAcceptDto dto)
        {
            if (_activeCallForm != null && _activeCallId == dto.CallId)
            {
                _activeCallForm.SwitchToInCallMode();
                _activeCallForm.SetStatus("Connecté");
            }

            _activeCallStartedAt = DateTime.UtcNow;
            UpdateChatCallState(_activeCallPeerId, VoiceCallUiState.InCall, "En appel");
            _ = SetInCallStatusAsync();
        }

        private async Task<CallLogDto> BuildCallLogAsync(CallEndDto dto, int peerUserId)
        {
            CallLogDto? log = null;
            if (_voiceCallService != null)
            {
                try
                {
                    var history = await _voiceCallService.GetHistoryAsync(peerUserId);
                    if (history != null)
                    {
                        log = history.Find(h => h.CallId == dto.CallId);
                    }
                }
                catch
                {
                    // Ignore history failures, fallback below
                }
            }

            var endedAt = dto.EndedAt == default ? DateTime.UtcNow : dto.EndedAt;
            if (log != null)
            {
                // Ensure EndedAt is set even if history returned default
                log.EndedAt = log.EndedAt == default ? endedAt : log.EndedAt;
                return log;
            }

            var startedAt = _activeCallStartedAt ?? endedAt;
            var durationSeconds = Math.Max(0, (int)Math.Round((endedAt - startedAt).TotalSeconds));

            return new CallLogDto
            {
                CallId = dto.CallId,
                CallerId = dto.FromUserId,
                CalleeId = dto.ToUserId,
                StartedAt = startedAt,
                EndedAt = endedAt,
                Result = dto.Reason?.ToLowerInvariant() switch
                {
                    "refused" => "rejected",
                    "cancelled" => "cancelled",
                    "missed" => "missed",
                    "incall" => "busy",
                    "in_call" => "busy",
                    _ => "completed"
                },
                EndReason = dto.Reason,
                // DurationSeconds is computed property; we set timestamps above
            };
        }

        private async Task AppendCallEndedMessageAsync(CallEndDto dto, int peerUserId)
        {
            var log = await BuildCallLogAsync(dto, peerUserId);
            var content = JsonSerializer.Serialize(log);
            var msg = new ChatMessageDto
            {
                MessageId = 0,
                SenderId = _currentUser.Id,
                SenderName = _currentUser.Username ?? "Moi",
                ReceiverId = peerUserId,
                Content = content,
                ContentType = "call",
                Timestamp = log.EndedAt.ToLocalTime(),
                IsEdited = false
            };

            if (_openChats.TryGetValue(peerUserId, out var chat) && chat != null && !chat.IsDisposed)
            {
                chat.AppendMessage(msg, isMe: true);
            }
        }

        private async Task AppendCallBusyMessageAsync(int peerUserId, string reason)
        {
            var now = DateTime.UtcNow;
            var log = new CallLogDto
            {
                CallId = Guid.NewGuid().ToString(),
                CallerId = _currentUser.Id,
                CalleeId = peerUserId,
                StartedAt = now,
                EndedAt = now,
                Result = "busy",
                EndReason = reason
            };

            var msg = new ChatMessageDto
            {
                MessageId = 0,
                SenderId = _currentUser.Id,
                SenderName = _currentUser.Username ?? "Moi",
                ReceiverId = peerUserId,
                Content = JsonSerializer.Serialize(log),
                ContentType = "call",
                Timestamp = now.ToLocalTime(),
                IsEdited = false
            };

            if (_openChats.TryGetValue(peerUserId, out var chat) && chat != null && !chat.IsDisposed)
            {
                chat.AppendMessage(msg, isMe: true);
            }
        }

        private void HandleCallRejected(CallRejectDto dto)
        {
            var peer = dto.FromUserId == _currentUser.Id ? dto.ToUserId : dto.FromUserId;
            var reason = dto.Reason?.ToLowerInvariant() ?? string.Empty;

            if (reason == "blocked")
            {
                PalMessageBox.Show("Vous ne pouvez pas appeler ce contact car il vous a bloqué.", "Appel bloqué", MessageBoxButtons.OK, MessageBoxIcon.Information);
                UpdateChatCallState(peer, VoiceCallUiState.Idle, "Appel impossible — contact bloqué.");
                _activeCallForm?.Close();
                _activeCallForm = null;
                _activeCallId = null;
                _activeCallStartedAt = null;
                _ = RestoreStatusAfterCallAsync();
                return;
            }

            if (reason == "incall" || reason == "in_call" || reason == "videoincall")
            {
                _activeCallForm?.SetStatus("Destinataire en appel");
                PalMessageBox.Show("Le destinataire est en appel, veuillez réessayer plus tard.", "Appel occupé", MessageBoxButtons.OK, MessageBoxIcon.Information);
                UpdateChatCallState(peer, VoiceCallUiState.Idle, "Destinataire en appel");
                _ = AppendCallBusyMessageAsync(peer, dto.Reason ?? "InCall");
                _activeCallStartedAt = null;
                _ = RestoreStatusAfterCallAsync();

                if (_activeCallId == null || _activeCallId == dto.CallId)
                {
                    _activeCallForm?.Close();
                    _activeCallForm = null;
                    _activeCallId = null;
                }
                return;
            }

            if (_activeCallId == null || _activeCallId == dto.CallId)
            {
                _activeCallForm?.Close();
                _activeCallForm = null;
                _activeCallId = null;
                PalMessageBox.Show("Appel refusé ou indisponible");
            }

            UpdateChatCallState(peer, VoiceCallUiState.Idle, "Appel vocal");
            _activeCallStartedAt = null;
            _ = RestoreStatusAfterCallAsync();
        }

        private void HandleCallEnded(CallEndDto dto)
        {
            if (_activeCallId == dto.CallId)
            {
                _activeCallForm?.Close();
                _activeCallForm = null;
                _activeCallId = null;
            }

            var peer = dto.FromUserId == _currentUser.Id ? dto.ToUserId : dto.FromUserId;
            UpdateChatCallState(peer, VoiceCallUiState.Idle, "Appel vocal");
            _ = AppendCallEndedMessageAsync(dto, peer);
            _activeCallStartedAt = null;
            _ = RestoreStatusAfterCallAsync();
        }

        private void WireCallForm(VoiceCallForm form, string callId, int peerUserId, string peerDisplayName)
        {
            var preserveCallOnClose = false;

            form.HangupRequested += async (_, __) =>
            {
                if (_voiceCallService != null)
                {
                    await _voiceCallService.HangupAsync(callId);
                }
                form.Close();
                UpdateChatCallState(_activeCallPeerId, VoiceCallUiState.Idle, "Appel vocal");
                _activeCallStartedAt = null;
                _ = RestoreStatusAfterCallAsync();
            };

            form.MuteToggled += (_, muted) =>
            {
                _voiceCallService?.SetMuted(muted);
                UpdateChatCallState(_activeCallPeerId, muted ? VoiceCallUiState.Muted : VoiceCallUiState.InCall, muted ? "Micro coupé" : "En appel");
            };
            form.VolumeChanged += (_, vol) => _voiceCallService?.SetVolumePercent(vol);
            form.AcceptRequested += async (_, __) =>
            {
                if (_voiceCallService != null)
                {
                    await _voiceCallService.AcceptAsync(callId);
                }
                form.SwitchToInCallMode();
                UpdateChatCallState(_activeCallPeerId, VoiceCallUiState.InCall, "En appel");
            };
            form.RejectRequested += async (_, __) =>
            {
                if (_voiceCallService != null)
                {
                    await _voiceCallService.RejectAsync(callId, "refused");
                }
                form.Close();
                UpdateChatCallState(_activeCallPeerId, VoiceCallUiState.Idle, "Appel vocal");
            };

            form.OpenChatRequested += (_, __) =>
            {
                var chat = EnsureChatWindow(peerUserId, peerDisplayName);
                var state = form.IsInCallMode
                    ? (form.IsMuted ? VoiceCallUiState.Muted : VoiceCallUiState.InCall)
                    : VoiceCallUiState.InCall;
                var status = string.IsNullOrWhiteSpace(form.CurrentStatus) ? "Appel vocal" : form.CurrentStatus;
                UpdateChatCallState(peerUserId, state, status);

                // Close mini-window once the chat is ready and synced
                if (chat != null && !chat.IsDisposed)
                {
                    preserveCallOnClose = true;
                    chat.BeginInvoke(new Action(() =>
                    {
                        chat.UpdateVoiceCallButton(state, status);
                        form.Close();
                    }));
                }
                else
                {
                    preserveCallOnClose = true;
                    form.Close();
                }
            };

            form.FormClosed += (_, __) =>
            {
                if (_activeCallForm == form)
                {
                    _activeCallForm = null;
                    if (!preserveCallOnClose)
                    {
                        _activeCallId = null;
                    }
                }
            };
        }

        private string ResolveDisplayName(int userId, string fallback)
        {
            var item = FindFriendListViewItem(userId);
            if (item?.Tag is UserProfileDto user)
            {
                if (!string.IsNullOrWhiteSpace(user.DisplayedName)) return user.DisplayedName;
                if (!string.IsNullOrWhiteSpace(user.FirstName) || !string.IsNullOrWhiteSpace(user.LastName))
                {
                    return ($"{user.FirstName} {user.LastName}").Trim();
                }
                return string.IsNullOrWhiteSpace(user.Username) ? fallback : user.Username;
            }
            return fallback;
        }

        public async Task StartVoiceCall(int peerUserId)
        {
            if (_voiceCallService == null)
            {
                PalMessageBox.Show("Le service d'appel n'est pas prêt.");
                return;
            }

            if (_activeVideoCallId != null)
            {
                PalMessageBox.Show("Vous êtes déjà en appel vidéo.", "Appel", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_blockedByOthers.TryGetValue(peerUserId, out var remoteBlock) && remoteBlock.IsBlocked)
            {
                PalMessageBox.Show("Vous ne pouvez pas appeler ce contact car il vous a bloqué.", "Appel bloqué", MessageBoxButtons.OK, MessageBoxIcon.Information);
                UpdateChatCallState(peerUserId, VoiceCallUiState.Idle, "Appel impossible — contact bloqué.");
                return;
            }

            _activeCallPeerId = peerUserId;
            try
            {
                await _voiceCallService.InviteAsync(peerUserId);
                UpdateChatCallState(peerUserId, VoiceCallUiState.InCall, "Appel en cours…");
            }
            catch (Exception ex)
            {
                PalMessageBox.Show($"Impossible de lancer l'appel : {ex.Message}");
            }
        }

        public Task StartVideoCall(int peerUserId)
        {
            return StartVideoCallInternalAsync(peerUserId);
        }

        private async Task StartVideoCallInternalAsync(int peerUserId)
        {
            if (_videoCallService == null)
            {
                PalMessageBox.Show("Le service d'appel vidéo n'est pas prêt.");
                return;
            }

            // Empêcher tout autre appel pendant une session vidéo
            if (_activeCallId != null)
            {
                PalMessageBox.Show("Terminez l'appel vocal en cours avant de lancer un appel vidéo.", "Appel", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_activeVideoCallId != null)
            {
                PalMessageBox.Show("Vous êtes déjà en appel vidéo.", "Appel vidéo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_blockedByOthers.TryGetValue(peerUserId, out var remoteBlock) && remoteBlock.IsBlocked)
            {
                PalMessageBox.Show("Vous ne pouvez pas appeler ce contact car il vous a bloqué.", "Appel vidéo bloqué", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _activeVideoCallPeerId = peerUserId;
            try
            {
                await _videoCallService.InviteAsync(peerUserId);
            }
            catch (Exception ex)
            {
                PalMessageBox.Show($"Impossible de lancer l'appel vidéo : {ex.Message}");
            }
        }

        private void UpdateChatCallState(int peerUserId, VoiceCallUiState state, string tooltip)
        {
            if (_openChats.TryGetValue(peerUserId, out var chat) && chat != null && !chat.IsDisposed)
            {
                chat.UpdateVoiceCallButton(state, tooltip);
            }
        }

        private async Task CheckServiceStatus()
        {
            if (IsLogout) return;

            try
            {
                var url = $"{ApiBaseUrl}/service/check";
                if (!string.IsNullOrEmpty(_connectionId))
                {
                    url += $"?connectionId={_connectionId}";
                }
                else
                {
                    // Debug: ID manquant
                    Console.WriteLine("CheckServiceStatus: _connectionId est vide, impossible de vérifier la validité du client.");
                }

                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var serviceResponse = JsonSerializer.Deserialize<ServiceCheckResponse>(responseJson, options);
                    
                    _serviceAvailable = serviceResponse?.ServiceAvailable ?? false;
                    bool isClientValid = serviceResponse?.ClientValid ?? true;
                    
                    if (_serviceAvailable)
                    {
                        if (!isClientValid)
                        {
                            Console.WriteLine($"Client invalide détecté par le service. ID: {_connectionId}");
                        }

                        if (!isClientValid && !string.IsNullOrEmpty(_connectionId))
                        {
                            if (IsLogout) return;
                            IsLogout = true;
                            timerConnectionCheck.Stop();

                            // Le client a été déconnecté par l'admin
                            lblConnectionStatus.Text = $"Statut: Déconnecté par l'admin ✗ ({DateTime.Now:HH:mm:ss})";
                            lblConnectionStatus.ForeColor = System.Drawing.Color.Red;
                            
                            PalMessageBox.Show("Vous avez été déconnecté par l'administrateur.",
                                "Déconnexion forcée", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            await Logout();
                        }
                        else
                        {
                            lblConnectionStatus.Text = $"Statut: Service actif ✓ ({DateTime.Now:HH:mm:ss})";
                            lblConnectionStatus.ForeColor = System.Drawing.Color.Green;
                        }
                    }
                    else
                    {
                        if (IsLogout) return;
                        IsLogout = true;
                        timerConnectionCheck.Stop();

                        lblConnectionStatus.Text = $"Statut: Service arrêté par l'admin ✗ ({DateTime.Now:HH:mm:ss})";
                        lblConnectionStatus.ForeColor = System.Drawing.Color.Red;
                        
                        // Si le service n'est pas disponible, déconnecter l'utilisateur
                        PalMessageBox.Show("Le service a été arrêté par l'administrateur. Vous allez être déconnecté.",
                            "Service arrêté", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        await Logout();
                    }
                }
                else
                {
                    lblConnectionStatus.Text = $"Statut: Erreur de connexion ({DateTime.Now:HH:mm:ss})";
                    lblConnectionStatus.ForeColor = System.Drawing.Color.Orange;
                }
            }
            catch (Exception ex)
            {
                lblConnectionStatus.Text = $"Statut: Erreur ({DateTime.Now:HH:mm:ss})";
                lblConnectionStatus.ForeColor = System.Drawing.Color.Orange;
                Console.WriteLine($"Erreur lors de la vérification du service: {ex.Message}");
            }
        }

        private async Task RegisterConnection()
        {
            try
            {
                // Utiliser l'ID SignalR s'il est disponible
                var signalRId = _hubConnection?.ConnectionId;
                var connectRequest = new { SignalRConnectionId = signalRId };
                var json = JsonSerializer.Serialize(connectRequest);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{ApiBaseUrl}/service/connect", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseJson);
                    if (doc.RootElement.TryGetProperty("connectionId", out var connectionIdElement) || 
                        doc.RootElement.TryGetProperty("ConnectionId", out connectionIdElement))
                    {
                        _connectionId = connectionIdElement.GetString() ?? string.Empty;
                        Console.WriteLine($"ID de connexion reçu: {_connectionId}");
                    }
                    else 
                    {
                        Console.WriteLine("Impossible de récupérer l'ID de connexion dans la réponse.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'enregistrement de la connexion: {ex.Message}");
            }
        }

        private async Task UnregisterConnection()
        {
            if (!string.IsNullOrEmpty(_connectionId))
            {
                try
                {
                    var disconnectRequest = new { ConnectionId = _connectionId };
                    var json = JsonSerializer.Serialize(disconnectRequest);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    
                    await _httpClient.PostAsync($"{ApiBaseUrl}/service/disconnect", content);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de la déconnexion du service: {ex.Message}");
                }
            }
        }

        private async void timerConnectionCheck_Tick(object sender, EventArgs e)
        {
            // Vérifier périodiquement l'état du service
            await CheckServiceStatus();
        }

        public bool IsLogout { get; private set; } = false;

        private async void btnLogout_Click(object sender, EventArgs e)
        {
            IsLogout = true;
            await Logout();
        }

        private async Task Logout()
        {
            IsLogout = true;
            
            // Se déconnecter du service
            await UnregisterConnection();

            // Arrêter SignalR
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
            }
            
            // Arrêter le timer
            timerConnectionCheck.Stop();
            
            // Fermer la fenêtre
            this.Close();
        }

        private async void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Disposer le moniteur d'activité
            _activityMonitor?.Dispose();
            _voiceCallService?.Dispose();
            
            // End session via API
            try
            {
                await _httpClient.PostAsync($"{ApiBaseUrl}/session/end", null);
            }
            catch { /* Ignore errors on closing */ }
            
            // Se déconnecter du service lors de la fermeture
            await UnregisterConnection();
            
            // Arrêter le timer
            timerConnectionCheck.Stop();
        }

        public HttpClient GetHttpClient() => _httpClient;
        public string GetApiBaseUrl() => ApiBaseUrl;

        // Méthode pour forcer la déconnexion (appelée par l'admin)
        public async Task<List<ChatMessageDto>> GetChatHistory(int otherUserId, int? take = null)
        {
            try
            {
                var url = take.HasValue
                    ? $"{ApiBaseUrl}/chat/history/{otherUserId}?take={take.Value}"
                    : $"{ApiBaseUrl}/chat/history/{otherUserId}";

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    return JsonSerializer.Deserialize<List<ChatMessageDto>>(json, options) ?? new List<ChatMessageDto>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error fetching history: " + ex.Message);
            }
            return new List<ChatMessageDto>();
        }

        public void ForceLogout()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(ForceLogout));
                return;
            }

            PalMessageBox.Show("Vous avez été déconnecté par l'administrateur.",
                "Déconnexion forcée", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            // Appeler Logout de manière asynchrone
            _ = Logout();
        }

        public class FriendListComparer : System.Collections.IComparer
        {
            public int Compare(object? x, object? y)
            {
                var itemX = x as ListViewItem;
                var itemY = y as ListViewItem;

                if (itemX == null || itemY == null) return 0;

                var userX = itemX.Tag as UserProfileDto;
                var userY = itemY.Tag as UserProfileDto;

                if (userX == null || userY == null) return 0;

                // 1. Sort by Status (Online > Pending > Offline)
                if (userX.IsOnline && !userY.IsOnline) return -1;
                if (!userX.IsOnline && userY.IsOnline) return 1;
                
                if (userX.IsPending && !userY.IsPending) return -1; // Pending comes before Offline (assuming Offline is false/false)
                if (!userX.IsPending && userY.IsPending) return 1;
                
                // Wait, logic:
                // Online (IsOnline=true)
                // Pending (IsPending=true, IsOnline=false usually)
                // Offline (IsOnline=false, IsPending=false)
                
                int scoreX = (userX.IsOnline ? 2 : 0) + (userX.IsPending ? 1 : 0);
                int scoreY = (userY.IsOnline ? 2 : 0) + (userY.IsPending ? 1 : 0);
                
                if (scoreX > scoreY) return -1;
                if (scoreX < scoreY) return 1;

                // 2. Sort by Name (Alphabetical)
                return string.Compare(userX.FirstName + userX.LastName, userY.FirstName + userY.LastName, StringComparison.OrdinalIgnoreCase);
            }
        }

        private sealed class RemoteBlockState
        {
            public bool IsBlocked { get; set; }
            public string? Message { get; set; }
            public bool IsPermanent { get; set; }
            public DateTime? BlockedUntil { get; set; }
            public string? Reason { get; set; }
            public bool NotificationShown { get; set; }
        }

        private sealed class SanctionNotificationPayload
        {
            public string NotificationType { get; set; } = string.Empty;
            public string Severity { get; set; } = string.Empty;
            public int BlockedByUserId { get; set; }
            public string Message { get; set; } = string.Empty;
            public string Reason { get; set; } = string.Empty;
            public bool IsPermanent { get; set; }
            public DateTime? BlockedUntil { get; set; }
        }

        private PictureBox? _pbProfileHeader;
        private Label? _lblUserNameHeader;

        private void SetupCustomHeader()
        {
            // 1. Move Welcome Label
            lblWelcome.Location = new Point(130, 40);

            // 2. Setup Profile Picture
            _pbProfileHeader = new PictureBox
            {
                Size = new Size(64, 64),
                Location = new Point(50, 45),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };
            this.Controls.Add(_pbProfileHeader);

            // 3. Setup "Connecté en tant que :" Label
            lblUserInfo.Text = "Connecté en tant que :";
            lblUserInfo.Location = new Point(130, 80);
            lblUserInfo.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            
            // 4. Setup Name Label (Bold)
            string displayName = !string.IsNullOrWhiteSpace(_currentUser.LastName) 
                ? $"{_currentUser.FirstName} {_currentUser.LastName.ToUpper()}" 
                : _currentUser.Username;

            _lblUserNameHeader = new Label
            {
                Text = displayName,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.Black,
                AutoSize = true
            };
            this.Controls.Add(_lblUserNameHeader);
            
            // Adjust position
            _lblUserNameHeader.Location = new Point(lblUserInfo.Location.X + lblUserInfo.PreferredWidth + 5, lblUserInfo.Location.Y);

            // 5. Move Connection Status
            lblConnectionStatus.Location = new Point(130, 105);

            // 6. Load Profile Image
            _ = LoadHeaderProfileImage();
        }

        private async Task LoadHeaderProfileImage()
        {
            try
            {
                // Use the correct endpoint: /api/profile (not /api/profile/me)
                var response = await _httpClient.GetAsync($"{ApiBaseUrl}/profile");
                if (response.IsSuccessStatusCode)
                {
                    var profile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
                    if (profile != null && _lblUserNameHeader != null && _pbProfileHeader != null)
                    {
                        // Update Name
                        if (!string.IsNullOrWhiteSpace(profile.LastName))
                        {
                            string displayName = $"{profile.FirstName} {profile.LastName.ToUpper()}";
                            _lblUserNameHeader.Text = displayName;
                            _lblUserNameHeader.Location = new Point(lblUserInfo.Location.X + lblUserInfo.PreferredWidth + 5, lblUserInfo.Location.Y);
                        }

                        // Load Image (Profile Picture or Gender Fallback)
                        Image? img = null;
                        if (profile.ProfilePicture != null && profile.ProfilePicture.Length > 0)
                        {
                            try
                            {
                                using var ms = new MemoryStream(profile.ProfilePicture);
                                img = Image.FromStream(ms);
                            }
                            catch
                            {
                                // Fallback if image data is corrupted
                            }
                        }

                        if (img == null)
                        {
                            string genderIcon = "icon/gender/autre.ico";
                            if (string.Equals(profile.Gender, "Homme", StringComparison.OrdinalIgnoreCase)) genderIcon = "icon/gender/homme.ico";
                            else if (string.Equals(profile.Gender, "Femme", StringComparison.OrdinalIgnoreCase)) genderIcon = "icon/gender/femme.ico";
                            
                            img = ResourceImageStore.LoadImage(genderIcon);
                        }

                        if (img != null)
                        {
                            _pbProfileHeader.Image = img;
                        }
                        else
                        {
                            // Last resort fallback if even gender icon fails
                            System.Diagnostics.Debug.WriteLine("Failed to load any profile image for header.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading profile header: " + ex.Message);
            }
        }
    }
}