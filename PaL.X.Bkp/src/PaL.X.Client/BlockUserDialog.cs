using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using PaL.X.Shared.DTOs;

namespace PaL.X.Client
{
    public class BlockUserDialog : Form
    {
        private readonly UserProfileDto _target;
        private readonly BlockedUserDto? _existingBlock;
        private bool _isInitializing;
        private Label _lblHeadline = null!;
        private TextBox _txtReason = null!;
        private CheckBox _chkPermanent = null!;
        private RadioButton _rbDuration = null!;
        private RadioButton _rbUntil = null!;
        private NumericUpDown _numDays = null!;
        private DateTimePicker _dtpUntil = null!;
        private Label _lblSummary = null!;
        private Button _btnOk = null!;
        private Button _btnCancel = null!;

        public string Reason { get; private set; } = string.Empty;
        public bool IsPermanent { get; private set; }
        public int? DurationDays { get; private set; }
        public DateTime? BlockedUntil { get; private set; }

        public BlockUserDialog(UserProfileDto target, BlockedUserDto? existingBlock = null)
        {
            _target = target;
            _existingBlock = existingBlock;
            _isInitializing = true;
            InitializeComponent();
            ApplyTargetDetails();
            ApplyExistingSettings();
            _isInitializing = false;
            ToggleDurationInputs();
            UpdateSummary();
        }

        private void InitializeComponent()
        {
            Text = "Bloquer un utilisateur";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            Size = new Size(520, 520);
            MinimumSize = new Size(520, 520);
            Padding = new Padding(12);

            var lblTitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Text = "Confirmer le blocage",
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(lblTitle);

            _lblHeadline = new Label
            {
                Dock = DockStyle.Top,
                Height = 48,
                Margin = new Padding(0, 12, 0, 0),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(_lblHeadline);
            _lblHeadline.BringToFront();

            var grpOptions = new GroupBox
            {
                Text = "Durée du blocage",
                Dock = DockStyle.Top,
                Height = 150,
                Padding = new Padding(12)
            };
            Controls.Add(grpOptions);
            grpOptions.BringToFront();

            _chkPermanent = new CheckBox
            {
                Text = "Blocage permanent",
                AutoSize = true,
                Location = new Point(12, 28)
            };
            _chkPermanent.CheckedChanged += (_, __) => ToggleDurationInputs();
            grpOptions.Controls.Add(_chkPermanent);

            _rbDuration = new RadioButton
            {
                Text = "Durée (en jours)",
                Location = new Point(32, 60),
                AutoSize = true,
                Checked = true
            };
            _rbDuration.CheckedChanged += (_, __) => ToggleDurationInputs();
            grpOptions.Controls.Add(_rbDuration);

            _numDays = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 365,
                Value = 7,
                Location = new Point(200, 58),
                Width = 80
            };
            _numDays.ValueChanged += (_, __) => UpdateSummary();
            grpOptions.Controls.Add(_numDays);

            _rbUntil = new RadioButton
            {
                Text = "Date de fin",
                Location = new Point(32, 92),
                AutoSize = true
            };
            _rbUntil.CheckedChanged += (_, __) => ToggleDurationInputs();
            grpOptions.Controls.Add(_rbUntil);

            _dtpUntil = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy HH:mm",
                Value = DateTime.Now.AddDays(7),
                Location = new Point(200, 90),
                Width = 200,
                ShowUpDown = true
            };
            _dtpUntil.ValueChanged += (_, __) => UpdateSummary();
            grpOptions.Controls.Add(_dtpUntil);

            var grpReason = new GroupBox
            {
                Text = "Raison (facultatif)",
                Dock = DockStyle.Top,
                Height = 120,
                Padding = new Padding(12)
            };
            Controls.Add(grpReason);
            grpReason.BringToFront();

            _txtReason = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Vertical,
                PlaceholderText = "Exemple : comportements inappropriés..."
            };
            _txtReason.TextChanged += (_, __) => UpdateSummary();
            grpReason.Controls.Add(_txtReason);

            _lblSummary = new Label
            {
                Dock = DockStyle.Top,
                Height = 60,
                Margin = new Padding(0, 12, 0, 0),
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.FromArgb(90, 90, 90)
            };
            Controls.Add(_lblSummary);
            _lblSummary.BringToFront();

            var pnlButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 10, 0, 0)
            };
            Controls.Add(pnlButtons);

            _btnOk = new Button
            {
                Text = "Bloquer",
                DialogResult = DialogResult.OK,
                Width = 110,
                Height = 32,
                BackColor = Color.FromArgb(185, 28, 28),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnOk.FlatAppearance.BorderSize = 0;
            _btnOk.Click += (_, __) => OnConfirm();
            pnlButtons.Controls.Add(_btnOk);

            _btnCancel = new Button
            {
                Text = "Annuler",
                DialogResult = DialogResult.Cancel,
                Width = 100,
                Height = 32,
                Margin = new Padding(0, 0, 10, 0)
            };
            pnlButtons.Controls.Add(_btnCancel);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
        }

        private void ApplyTargetDetails()
        {
            var nameBuilder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(_target.FirstName))
            {
                nameBuilder.Append(_target.FirstName.Trim());
            }
            if (!string.IsNullOrWhiteSpace(_target.LastName))
            {
                if (nameBuilder.Length > 0)
                {
                    nameBuilder.Append(' ');
                }
                nameBuilder.Append(_target.LastName.Trim());
            }
            if (nameBuilder.Length == 0 && !string.IsNullOrWhiteSpace(_target.DisplayedName))
            {
                nameBuilder.Append(_target.DisplayedName.Trim());
            }
            if (nameBuilder.Length == 0)
            {
                nameBuilder.Append("cet utilisateur");
            }

            _lblHeadline.Text = $"Vous êtes sur le point de bloquer {nameBuilder}.\nIl ne pourra plus vous écrire tant que le blocage restera actif.";
        }

        private void ApplyExistingSettings()
        {
            if (_existingBlock == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(_existingBlock.Reason))
            {
                _txtReason.Text = _existingBlock.Reason;
            }

            if (_existingBlock.IsPermanent)
            {
                _chkPermanent.Checked = true;
                return;
            }

            _chkPermanent.Checked = false;

            if (_existingBlock.BlockedUntil.HasValue)
            {
                _rbUntil.Checked = true;
                var localValue = _existingBlock.BlockedUntil.Value.ToLocalTime();
                if (localValue < _dtpUntil.MinDate)
                {
                    localValue = _dtpUntil.MinDate;
                }
                else if (localValue > _dtpUntil.MaxDate)
                {
                    localValue = _dtpUntil.MaxDate;
                }
                _dtpUntil.Value = localValue;
                return;
            }

            if (_existingBlock.DurationDays.HasValue)
            {
                _rbDuration.Checked = true;
                var days = Math.Clamp(_existingBlock.DurationDays.Value, (int)_numDays.Minimum, (int)_numDays.Maximum);
                _numDays.Value = days;
            }
        }

        private void ToggleDurationInputs()
        {
            bool enabled = !_chkPermanent.Checked;
            _rbDuration.Enabled = enabled;
            _rbUntil.Enabled = enabled;
            _numDays.Enabled = enabled && _rbDuration.Checked;
            _dtpUntil.Enabled = enabled && _rbUntil.Checked;

            if (_chkPermanent.Checked)
            {
                _rbDuration.Checked = false;
                _rbUntil.Checked = false;
            }
            else if (!_rbDuration.Checked && !_rbUntil.Checked)
            {
                _rbDuration.Checked = true;
            }

            if (!_isInitializing)
            {
                UpdateSummary();
            }
        }

        private void UpdateSummary()
        {
            if (_isInitializing)
            {
                return;
            }

            var builder = new StringBuilder();

            if (_chkPermanent.Checked)
            {
                builder.Append("Blocage permanent : l'utilisateur restera bloqué jusqu'à déblocage manuel.");
            }
            else if (_rbDuration.Checked)
            {
                builder.AppendFormat("Blocage temporaire : {0} jour(s), soit jusqu'au {1:dd/MM/yyyy HH:mm}.",
                    (int)_numDays.Value,
                    DateTime.Now.AddDays((double)_numDays.Value));
            }
            else if (_rbUntil.Checked)
            {
                builder.AppendFormat("Blocage actif jusqu'au {0:dd/MM/yyyy HH:mm}.", _dtpUntil.Value);
            }
            else
            {
                builder.Append("Sélectionnez une durée ou cochez 'permanent'.");
            }

            if (!string.IsNullOrWhiteSpace(_txtReason.Text))
            {
                builder.AppendLine();
                builder.Append("Motif fourni : ");
                builder.Append(_txtReason.Text.Trim());
            }

            _lblSummary.Text = builder.ToString();
        }

        private void OnConfirm()
        {
            if (_chkPermanent.Checked)
            {
                IsPermanent = true;
                DurationDays = null;
                BlockedUntil = null;
            }
            else if (_rbDuration.Checked)
            {
                IsPermanent = false;
                DurationDays = (int)_numDays.Value;
                BlockedUntil = null;
            }
            else if (_rbUntil.Checked)
            {
                var selected = _dtpUntil.Value;
                if (selected <= DateTime.Now.AddMinutes(1))
                {
                    PalMessageBox.Show("La date de fin doit être postérieure à maintenant.", "Blocage", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }

                IsPermanent = false;
                DurationDays = null;
                BlockedUntil = DateTime.SpecifyKind(selected, DateTimeKind.Local);
            }
            else
            {
                PalMessageBox.Show("Veuillez choisir une durée de blocage ou cocher l'option permanente.", "Blocage", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            Reason = _txtReason.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
