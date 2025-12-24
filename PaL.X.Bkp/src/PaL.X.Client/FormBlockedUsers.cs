using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using PaL.X.Client.Services;
using PaL.X.Shared.DTOs;

namespace PaL.X.Client
{
    public class FormBlockedUsers : Form
    {
        private readonly MainForm _mainForm;

        private readonly ListView _lvBlocked = new();
        private readonly Label _lblCount = new();
        private readonly Button _btnRefresh = new();
        private readonly Button _btnClose = new();
        private readonly ColumnHeader _colName = new();
        private readonly ColumnHeader _colSince = new();
        private readonly ColumnHeader _colUntil = new();
        private readonly ColumnHeader _colReason = new();
        private readonly ImageList _genderImages = new();
        private ContextMenuStrip _ctxMenu = null!;
        private ToolStripMenuItem _ctxUnblock = null!;
        private ToolStripMenuItem _ctxModify = null!;

        public FormBlockedUsers(MainForm mainForm)
        {
            _mainForm = mainForm;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Utilisateurs bloqués";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Size = new Size(720, 420);
            Padding = new Padding(12);

            _lblCount.Dock = DockStyle.Top;
            _lblCount.Height = 24;
            _lblCount.TextAlign = ContentAlignment.MiddleLeft;
            _lblCount.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            Controls.Add(_lblCount);

            _lvBlocked.Dock = DockStyle.Fill;
            _lvBlocked.View = View.Details;
            _lvBlocked.FullRowSelect = true;
            _lvBlocked.HideSelection = false;
            _lvBlocked.MultiSelect = false;
            _lvBlocked.Columns.AddRange(new[] { _colName, _colSince, _colUntil, _colReason });
            _lvBlocked.SelectedIndexChanged += (_, __) => UpdateActionAvailability();
            _lvBlocked.DoubleClick += async (_, __) => await TriggerModify();
            _lvBlocked.MouseDown += OnListMouseDown;
            Controls.Add(_lvBlocked);
            _lvBlocked.BringToFront();

            _genderImages.ColorDepth = ColorDepth.Depth32Bit;
            _genderImages.ImageSize = new Size(24, 24);
            LoadGenderIcons();
            _lvBlocked.SmallImageList = _genderImages;

            _ctxMenu = new ContextMenuStrip();
            _ctxMenu.Opening += ContextMenuOpening;

            _ctxUnblock = new ToolStripMenuItem("Débloquer");
            _ctxUnblock.Click += async (_, __) => await TriggerUnblock();

            _ctxModify = new ToolStripMenuItem("Modifier");
            _ctxModify.Click += async (_, __) => await TriggerModify();

            LoadContextMenuIcons();

            _ctxMenu.Items.AddRange(new ToolStripItem[] { _ctxUnblock, _ctxModify });
            _lvBlocked.ContextMenuStrip = _ctxMenu;

            _colName.Text = "Utilisateur";
            _colName.Width = 200;
            _colSince.Text = "Depuis";
            _colSince.Width = 130;
            _colUntil.Text = "Fin";
            _colUntil.Width = 160;
            _colReason.Text = "Raison";
            _colReason.Width = 200;

            var panelButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 10, 0, 0)
            };
            Controls.Add(panelButtons);

            _btnClose.Text = "Fermer";
            _btnClose.Width = 100;
            _btnClose.Click += (_, __) => Close();
            panelButtons.Controls.Add(_btnClose);

            _btnRefresh.Text = "Actualiser";
            _btnRefresh.Width = 110;
            _btnRefresh.Margin = new Padding(0, 0, 8, 0);
            _btnRefresh.Click += async (_, __) => await RefreshList();
            panelButtons.Controls.Add(_btnRefresh);
        }

        public void UpdateList(IEnumerable<BlockedUserDto> blocked)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<IEnumerable<BlockedUserDto>>(UpdateList), blocked);
                return;
            }

            var entries = blocked
                .OrderByDescending(b => b.BlockedOn)
                .ToList();

            _lvBlocked.BeginUpdate();
            _lvBlocked.Items.Clear();

            foreach (var entry in entries)
            {
                var item = new ListViewItem(FormatDisplayName(entry))
                {
                    Tag = entry
                };

                var genderKey = ResolveGenderKey(entry.Gender);
                if (_genderImages.Images.ContainsKey(genderKey))
                {
                    item.ImageKey = genderKey;
                }

                item.SubItems.Add(entry.BlockedOn.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
                item.SubItems.Add(FormatUntil(entry));
                item.SubItems.Add(string.IsNullOrWhiteSpace(entry.Reason) ? "—" : entry.Reason);

                _lvBlocked.Items.Add(item);
            }

            _lvBlocked.EndUpdate();
            _lblCount.Text = entries.Count == 0
                ? "Aucun utilisateur bloqué"
                : entries.Count == 1
                    ? "1 utilisateur bloqué"
                    : $"{entries.Count} utilisateurs bloqués";

            UpdateActionAvailability();
        }

        private static string FormatDisplayName(BlockedUserDto dto)
        {
            var parts = new[] { dto.FirstName?.Trim(), dto.LastName?.Trim() }
                .Where(p => !string.IsNullOrWhiteSpace(p));

            var name = string.Join(" ", parts);
            return string.IsNullOrWhiteSpace(name) ? $"Utilisateur #{dto.UserId}" : name;
        }

        private static string FormatUntil(BlockedUserDto dto)
        {
            if (dto.IsPermanent)
            {
                return "Permanent";
            }

            if (dto.BlockedUntil.HasValue)
            {
                return dto.BlockedUntil.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            }

            if (dto.DurationDays.HasValue)
            {
                return $"{dto.DurationDays.Value} jour(s)";
            }

            return "—";
        }

        private void UpdateActionAvailability()
        {
            bool hasSelection = _lvBlocked.SelectedItems.Count > 0;

            if (_ctxUnblock != null)
            {
                _ctxUnblock.Enabled = hasSelection;
            }

            if (_ctxModify != null)
            {
                _ctxModify.Enabled = hasSelection;
            }
        }

        private async Task RefreshList()
        {
            await _mainForm.LoadBlockedUsersSafeAsync();
        }

        private async Task TriggerUnblock()
        {
            if (_lvBlocked.SelectedItems.Count == 0)
            {
                return;
            }

            if (_lvBlocked.SelectedItems[0].Tag is not BlockedUserDto entry)
            {
                return;
            }

            var displayName = FormatDisplayName(entry);
            var confirm = PalMessageBox.Show($"Voulez-vous débloquer {displayName} ?", "Déblocage", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
            {
                return;
            }

            var (success, error) = await _mainForm.TryUnblockFriendAsync(entry.UserId);
            if (success)
            {
                PalMessageBox.Show($"{displayName} a été débloqué.", "Déblocage", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (!string.IsNullOrWhiteSpace(error))
            {
                PalMessageBox.Show($"Déblocage impossible : {error}", "Déblocage", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            UpdateActionAvailability();
        }

        private async Task TriggerModify()
        {
            if (_lvBlocked.SelectedItems.Count == 0)
            {
                return;
            }

            if (_lvBlocked.SelectedItems[0].Tag is not BlockedUserDto entry)
            {
                return;
            }

            var profile = new UserProfileDto
            {
                Id = entry.UserId,
                FirstName = entry.FirstName,
                LastName = entry.LastName,
                DisplayedName = FormatDisplayName(entry),
                Gender = entry.Gender,
                Username = entry.UserId.ToString()
            };

            using var dialog = new BlockUserDialog(profile, entry);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            var (success, error) = await _mainForm.TryApplyBlockAsync(entry.UserId, dialog.Reason, dialog.IsPermanent, dialog.DurationDays, dialog.BlockedUntil);
            if (success)
            {
                PalMessageBox.Show($"Blocage mis à jour pour {profile.DisplayedName}.", "Blocage", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (!string.IsNullOrWhiteSpace(error))
            {
                PalMessageBox.Show($"Modification impossible : {error}", "Blocage", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            UpdateActionAvailability();
        }

        private void LoadGenderIcons()
        {
            TryAddGenderIcon("Male", "icon/gender/homme.ico");
            TryAddGenderIcon("Female", "icon/gender/femme.ico");
            TryAddGenderIcon("Other", "icon/gender/autre.ico");
        }

        private void LoadContextMenuIcons()
        {
            var unblockIcon = ResourceImageStore.LoadImage("icon/message/debloque.ico");
            if (unblockIcon != null)
            {
                _ctxUnblock.Image = unblockIcon;
            }

            var editIcon = ResourceImageStore.LoadImage("icon/message/modifier.ico");
            if (editIcon != null)
            {
                _ctxModify.Image = editIcon;
            }
        }

        private void OnListMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            var hit = _lvBlocked.HitTest(e.Location);
            if (hit.Item != null)
            {
                hit.Item.Selected = true;
            }
            else
            {
                _lvBlocked.SelectedItems.Clear();
            }
        }

        private void ContextMenuOpening(object? sender, CancelEventArgs e)
        {
            UpdateActionAvailability();
            if (_lvBlocked.SelectedItems.Count == 0)
            {
                e.Cancel = true;
            }
        }

        private void TryAddGenderIcon(string key, string resourceKey)
        {
            var image = ResourceImageStore.LoadImage(resourceKey);
            if (image == null || _genderImages.Images.ContainsKey(key))
            {
                return;
            }

            _genderImages.Images.Add(key, image);
        }

        private static string ResolveGenderKey(string? gender)
        {
            if (string.Equals(gender, "Homme", StringComparison.OrdinalIgnoreCase))
            {
                return "Male";
            }

            if (string.Equals(gender, "Femme", StringComparison.OrdinalIgnoreCase))
            {
                return "Female";
            }

            return "Other";
        }
    }
}
