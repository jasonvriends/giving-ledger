using Envelope_Steward.Forms;
using Envelope_Steward.Models;
using Envelope_Steward.Services;
using Microsoft.Data.Sqlite;

namespace Envelope_Steward
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            DataAccess.LoadLastChurch();
            InitializeComponent();
            BuildUI();
            Load += (_, _) =>
            {
                DataAccess.EnsureDatabase();
                RefreshAll();
                UpdateTitle();
            };
        }

        private void RefreshAll()
        {
            RefreshMembers();
            RefreshOfferingTypes();
            RefreshDonationCombos();
            RefreshDonations();
            LoadSettings();
            RefreshReportYears();
            RefreshStats();
            SetStatus("Ready");
        }

        private void UpdateTitle() =>
            Text = $"Giving Ledger — {DataAccess.CurrentChurch}";

        private void RefreshStats()
        {
            int year = DateTime.Today.Year;
            try
            {
                var (ytd, donors, nextRec) = DataAccess.GetDashboardStats(year);
                lblStatYtd.Text     = $"YTD {year}: {ytd:C0}";
                lblStatDonors.Text  = $"Active donors: {donors}";
                lblStatReceipt.Text = $"Next receipt #: {nextRec}";
            }
            catch { /* DB may not be ready on first paint */ }
        }

        // ── UI Construction ──────────────────────────────────────────────────

        private void BuildUI()
        {
            // Menu
            var menu = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("File");
            var miImportMembers   = new ToolStripMenuItem("Import Members CSV...");
            var miImportOfferings = new ToolStripMenuItem("Import Offering Types CSV...");
            var miOpenDb    = new ToolStripMenuItem("Open Database Location");
            var miBackup    = new ToolStripMenuItem("Backup Database to OneDrive…");
            var miSwitch    = new ToolStripMenuItem("Switch Congregation…");
            var miNewChurch = new ToolStripMenuItem("Add New Congregation…");
            var miExit      = new ToolStripMenuItem("Exit");
            miImportMembers.Click   += ImportMembers_Click;
            miImportOfferings.Click += ImportOfferings_Click;
            miOpenDb.Click    += (_, _) => System.Diagnostics.Process.Start("explorer.exe",
                $"/select,\"{DataAccess.DbPath}\"");
            miBackup.Click    += BackupToOneDrive_Click;
            miSwitch.Click    += SwitchChurch_Click;
            miNewChurch.Click += AddChurch_Click;
            miExit.Click      += (_, _) => Close();
            fileMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                miImportMembers, miImportOfferings,
                new ToolStripSeparator(),
                miOpenDb, miBackup,
                new ToolStripSeparator(),
                miSwitch, miNewChurch,
                new ToolStripSeparator(),
                miExit
            });
            menu.Items.Add(fileMenu);

            // Status bar — left: operation message, right: live stats
            statusStrip1 = new StatusStrip();
            toolStripStatusLabel1 = new ToolStripStatusLabel("Ready") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
            lblStatYtd     = new ToolStripStatusLabel("") { BorderSides = ToolStripStatusLabelBorderSides.Left, Padding = new Padding(8, 0, 4, 0) };
            lblStatDonors  = new ToolStripStatusLabel("") { BorderSides = ToolStripStatusLabelBorderSides.Left, Padding = new Padding(8, 0, 4, 0) };
            lblStatReceipt = new ToolStripStatusLabel("") { BorderSides = ToolStripStatusLabelBorderSides.Left, Padding = new Padding(8, 0, 8, 0) };
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel1, lblStatYtd, lblStatDonors, lblStatReceipt });

            // Tab control
            tabMain = new TabControl { Dock = DockStyle.Fill };
            tabMembers = new TabPage("Members");
            tabOfferingTypes = new TabPage("Offering Types");
            tabDonations = new TabPage("Donations");
            tabReports = new TabPage("Reports");
            tabSettings = new TabPage("Settings");

            BuildMembersTab();
            BuildOfferingTypesTab();
            BuildDonationsTab();
            BuildReportsTab();
            BuildSettingsTab();

            tabMain.TabPages.AddRange(new[] { tabMembers, tabOfferingTypes, tabDonations, tabReports, tabSettings });

            Controls.Add(tabMain);
            Controls.Add(menu);
            Controls.Add(statusStrip1);
            MainMenuStrip = menu;
        }

        // ── Members Tab ──────────────────────────────────────────────────────

        private void BuildMembersTab()
        {
            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
                Padding = new Padding(4), BackColor = SystemColors.Control
            };

            toolbar.Controls.Add(new Label { Text = "Search:", AutoSize = true, Anchor = AnchorStyles.None, Margin = new Padding(2, 6, 2, 0) });
            txtMemberSearch = new TextBox { Width = 220, Margin = new Padding(2, 4, 2, 4) };
            toolbar.Controls.Add(txtMemberSearch);

            Button TB(string t) { var b = new Button { Text = t, AutoSize = true, Margin = new Padding(2, 4, 2, 4) }; toolbar.Controls.Add(b); return b; }
            var btnSearch = TB("Search");
            var btnClear  = TB("Clear");
            toolbar.Controls.Add(new Label { Text = "|", AutoSize = true, Margin = new Padding(6, 6, 6, 0), ForeColor = Color.Silver });
            var btnAdd    = TB("Add");
            var btnEdit   = TB("Edit");
            var btnDelete = TB("Delete");
            toolbar.Controls.Add(new Label { Text = "|", AutoSize = true, Margin = new Padding(6, 6, 6, 0), ForeColor = Color.Silver });
            var btnLabels = TB("Print Mailing Labels (PDF)");

            btnSearch.Click += (_, _) => RefreshMembers(txtMemberSearch.Text);
            btnClear.Click  += (_, _) => { txtMemberSearch.Clear(); RefreshMembers(); };
            txtMemberSearch.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) RefreshMembers(txtMemberSearch.Text); };
            btnAdd.Click    += MemberAdd_Click;
            btnEdit.Click   += MemberEdit_Click;
            btnDelete.Click += MemberDelete_Click;
            btnLabels.Click += PrintMailingLabels_Click;

            dgvMembers = MakeGrid();
            dgvMembers.CellDoubleClick += (_, _) => MemberEdit_Click(null, EventArgs.Empty);
            dgvMembers.MouseDown += GridSelectRowOnRightClick;
            var ctxMembers = new ContextMenuStrip();
            ctxMembers.Items.Add("Edit Member",   null, MemberEdit_Click);
            ctxMembers.Items.Add(new ToolStripSeparator());
            ctxMembers.Items.Add("Delete Member", null, MemberDelete_Click);
            dgvMembers.ContextMenuStrip = ctxMembers;

            tabMembers.Controls.Add(dgvMembers);
            tabMembers.Controls.Add(toolbar);
        }

        private void RefreshMembers(string search = "")
        {
            dgvMembers.DataSource = null;
            dgvMembers.DataSource = DataAccess.GetMembersDataTable(search);
            if (dgvMembers.Columns.Contains("Id")) dgvMembers.Columns["Id"]!.Visible = false;
            SetStatus($"{dgvMembers.Rows.Count} member(s)");
        }

        private void MemberAdd_Click(object? s, EventArgs e)
        {
            using var dlg = new MemberEditForm();
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                DataAccess.AddMember(dlg.Member);
                RefreshMembers(txtMemberSearch.Text);
                RefreshStats();
                SetStatus("Member added.");
            }
            catch (Exception ex) { ShowError("Could not add member: " + ex.Message); }
        }

        private void MemberEdit_Click(object? s, EventArgs e)
        {
            if (!TryGetSelectedId(dgvMembers, out int id)) return;
            var existing = DataAccess.GetMember(id);
            if (existing == null) return;
            using var dlg = new MemberEditForm(existing);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                DataAccess.UpdateMember(dlg.Member);
                RefreshMembers(txtMemberSearch.Text);
                RefreshStats();
                SetStatus("Member updated.");
            }
            catch (Exception ex) { ShowError("Could not update member: " + ex.Message); }
        }

        private void MemberDelete_Click(object? s, EventArgs e)
        {
            if (!TryGetSelectedId(dgvMembers, out int id)) return;
            if (MessageBox.Show("Delete this member? This will also delete their donations.", "Confirm",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                DataAccess.DeleteMember(id);
                RefreshMembers(txtMemberSearch.Text);
                RefreshStats();
                SetStatus("Member deleted.");
            }
            catch (Exception ex) { ShowError("Could not delete: " + ex.Message); }
        }

        // ── Offering Types Tab ───────────────────────────────────────────────

        private void BuildOfferingTypesTab()
        {
            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
                Padding = new Padding(4), BackColor = SystemColors.Control
            };

            Button TB(string t) { var b = new Button { Text = t, AutoSize = true, Margin = new Padding(2, 4, 2, 4) }; toolbar.Controls.Add(b); return b; }
            var btnAdd    = TB("Add");
            var btnEdit   = TB("Edit");
            var btnDelete = TB("Delete");

            btnAdd.Click    += OfferingTypeAdd_Click;
            btnEdit.Click   += OfferingTypeEdit_Click;
            btnDelete.Click += OfferingTypeDelete_Click;

            dgvOfferingTypes = MakeGrid();
            dgvOfferingTypes.CellDoubleClick += (_, _) => OfferingTypeEdit_Click(null, EventArgs.Empty);
            dgvOfferingTypes.MouseDown += GridSelectRowOnRightClick;
            var ctxTypes = new ContextMenuStrip();
            ctxTypes.Items.Add("Edit Offering Type",   null, OfferingTypeEdit_Click);
            ctxTypes.Items.Add(new ToolStripSeparator());
            ctxTypes.Items.Add("Delete Offering Type", null, OfferingTypeDelete_Click);
            dgvOfferingTypes.ContextMenuStrip = ctxTypes;

            tabOfferingTypes.Controls.Add(dgvOfferingTypes);
            tabOfferingTypes.Controls.Add(toolbar);
        }

        private void RefreshOfferingTypes()
        {
            dgvOfferingTypes.DataSource = null;
            dgvOfferingTypes.DataSource = DataAccess.GetOfferingTypesDataTable();
            if (dgvOfferingTypes.Columns.Contains("Id")) dgvOfferingTypes.Columns["Id"]!.Visible = false;
            SetStatus($"{dgvOfferingTypes.Rows.Count} offering type(s)");
        }

        private void OfferingTypeAdd_Click(object? s, EventArgs e)
        {
            using var dlg = new OfferingTypeEditForm();
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            try { DataAccess.AddOfferingType(dlg.Record); RefreshOfferingTypes(); RefreshDonationCombos(); SetStatus("Offering type added."); }
            catch (Exception ex) { ShowError("Could not add: " + ex.Message); }
        }

        private void OfferingTypeEdit_Click(object? s, EventArgs e)
        {
            if (!TryGetSelectedId(dgvOfferingTypes, out int id)) return;
            var existing = DataAccess.GetOfferingType(id);
            if (existing == null) return;
            using var dlg = new OfferingTypeEditForm(existing);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            try { DataAccess.UpdateOfferingType(dlg.Record); RefreshOfferingTypes(); RefreshDonationCombos(); SetStatus("Offering type updated."); }
            catch (Exception ex) { ShowError("Could not update: " + ex.Message); }
        }

        private void OfferingTypeDelete_Click(object? s, EventArgs e)
        {
            if (!TryGetSelectedId(dgvOfferingTypes, out int id)) return;
            if (MessageBox.Show("Delete this offering type?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try { DataAccess.DeleteOfferingType(id); RefreshOfferingTypes(); RefreshDonationCombos(); SetStatus("Offering type deleted."); }
            catch (Exception ex) { ShowError("Could not delete: " + ex.Message); }
        }

        // ── Donations Tab ────────────────────────────────────────────────────

        private void BuildDonationsTab()
        {
            // ── Entry panel (two rows via TableLayoutPanel) ──────────────────
            var entryPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 10, RowCount = 2,
                Padding = new Padding(6, 4, 6, 4),
                BackColor = Color.FromArgb(245, 245, 250)
            };

            Label EL(string t) => new Label { Text = t, AutoSize = true, Anchor = AnchorStyles.Right | AnchorStyles.Left, Margin = new Padding(4, 6, 2, 0) };
            Padding CM() => new Padding(0, 4, 6, 4);

            cmbDonationMember = new ComboBox { Width = 250, DropDownStyle = ComboBoxStyle.DropDownList, Margin = CM() };
            dtpDonationDate = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short, Margin = CM() };
            cmbDonationOfferingType = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, Margin = CM() };
            txtDonationAmount = new TextBox { Width = 90, Margin = CM() };
            var btnAddDon = new Button { Text = "Add Donation", AutoSize = true, Margin = CM() };
            txtDonationNotes = new TextBox { Width = 460, Margin = CM() };

            // Row 0: Member | Date | Type | $ | Add
            entryPanel.Controls.Add(EL("Member:"),  0, 0); entryPanel.Controls.Add(cmbDonationMember,      1, 0);
            entryPanel.Controls.Add(EL("Date:"),    2, 0); entryPanel.Controls.Add(dtpDonationDate,        3, 0);
            entryPanel.Controls.Add(EL("Type:"),    4, 0); entryPanel.Controls.Add(cmbDonationOfferingType,5, 0);
            entryPanel.Controls.Add(EL("Amount $"), 6, 0); entryPanel.Controls.Add(txtDonationAmount,      7, 0);
            entryPanel.SetColumnSpan(btnAddDon, 2);
            entryPanel.Controls.Add(btnAddDon, 8, 0);

            // Row 1: Notes (span full width)
            entryPanel.Controls.Add(EL("Notes:"), 0, 1);
            entryPanel.SetColumnSpan(txtDonationNotes, 9);
            entryPanel.Controls.Add(txtDonationNotes, 1, 1);

            btnAddDon.Click += DonationAdd_Click;

            // ── Filter / action toolbar ──────────────────────────────────────
            var filterPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
                Padding = new Padding(4), BackColor = SystemColors.Control
            };

            Padding FM() => new Padding(2, 4, 2, 4);
            Label FL(string t) { var l = new Label { Text = t, AutoSize = true, Margin = new Padding(4, 7, 2, 0) }; filterPanel.Controls.Add(l); return l; }
            ComboBox FC(int w) { var c = new ComboBox { Width = w, DropDownStyle = ComboBoxStyle.DropDownList, Margin = FM() }; filterPanel.Controls.Add(c); return c; }
            Button FB(string t) { var b = new Button { Text = t, AutoSize = true, Margin = FM() }; filterPanel.Controls.Add(b); return b; }

            FL("Filter — Year:"); cmbDonationYear = FC(80);
            FL("Member:");        cmbDonationMemberFilter = FC(240);
            var btnFilter      = FB("Apply");
            var btnClearFilter = FB("Clear");
            filterPanel.Controls.Add(new Label { Text = "|", AutoSize = true, Margin = new Padding(6, 7, 6, 0), ForeColor = Color.Silver });
            var btnEditDon   = FB("Edit Selected");
            var btnDeleteDon = FB("Delete Selected");

            btnFilter.Click      += (_, _) => RefreshDonations();
            btnClearFilter.Click += (_, _) => { cmbDonationYear.SelectedIndex = 0; cmbDonationMemberFilter.SelectedIndex = 0; RefreshDonations(); };
            btnEditDon.Click     += DonationEdit_Click;
            btnDeleteDon.Click   += DonationDelete_Click;

            dgvDonations = MakeGrid();
            dgvDonations.CellDoubleClick += (_, _) => DonationEdit_Click(null, EventArgs.Empty);
            dgvDonations.MouseDown += GridSelectRowOnRightClick;
            var ctxDonations = new ContextMenuStrip();
            ctxDonations.Items.Add("Edit Donation",   null, DonationEdit_Click);
            ctxDonations.Items.Add(new ToolStripSeparator());
            ctxDonations.Items.Add("Delete Donation", null, DonationDelete_Click);
            dgvDonations.ContextMenuStrip = ctxDonations;

            tabDonations.Controls.Add(dgvDonations);
            tabDonations.Controls.Add(filterPanel);
            tabDonations.Controls.Add(entryPanel);
        }

        private void RefreshDonationCombos()
        {
            var members = DataAccess.GetAllMembers();
            var types = DataAccess.GetAllOfferingTypes();

            void PopulateMemberCombo(ComboBox cmb, bool addAll)
            {
                cmb.Items.Clear();
                if (addAll) cmb.Items.Add(new MemberRecord { FirstName = "(All Members)", EnvelopeNumber = "" });
                foreach (var m in members) cmb.Items.Add(m);
                cmb.DisplayMember = "ComboLabel";
                if (addAll) cmb.SelectedIndex = 0;
            }

            PopulateMemberCombo(cmbDonationMember, false);
            PopulateMemberCombo(cmbDonationMemberFilter, true);

            cmbDonationOfferingType.Items.Clear();
            foreach (var t in types) cmbDonationOfferingType.Items.Add(t);
            cmbDonationOfferingType.DisplayMember = "DisplayName";
            if (cmbDonationOfferingType.Items.Count > 0) cmbDonationOfferingType.SelectedIndex = 0;

            var years = DataAccess.GetAvailableYears();
            cmbDonationYear.Items.Clear();
            cmbDonationYear.Items.Add("(All Years)");
            foreach (var yr in years) cmbDonationYear.Items.Add(yr);
            cmbDonationYear.SelectedIndex = 0;
        }

        private void RefreshDonations()
        {
            int? memberId = null;
            int? year = null;

            if (cmbDonationMemberFilter.SelectedItem is MemberRecord fm && fm.Id > 0)
                memberId = fm.Id;
            if (cmbDonationYear.SelectedItem is int yr)
                year = yr;

            dgvDonations.DataSource = null;
            dgvDonations.DataSource = DataAccess.GetDonationsDataTable(memberId, year);
            if (dgvDonations.Columns.Contains("Id")) dgvDonations.Columns["Id"]!.Visible = false;
            if (dgvDonations.Columns.Contains("Amount")) dgvDonations.Columns["Amount"]!.DefaultCellStyle.Format = "C2";
            SetStatus($"{dgvDonations.Rows.Count} donation(s)");
        }

        private void DonationAdd_Click(object? s, EventArgs e)
        {
            if (cmbDonationMember.SelectedItem is not MemberRecord member)
            { ShowError("Please select a member."); return; }
            if (cmbDonationOfferingType.SelectedItem is not OfferingTypeRecord offeringType)
            { ShowError("Please select an offering type."); return; }
            if (!decimal.TryParse(txtDonationAmount.Text, out decimal amount) || amount <= 0)
            { ShowError("Please enter a valid amount."); return; }

            var d = new DonationRecord
            {
                MemberId = member.Id,
                OfferingTypeId = offeringType.Id,
                Amount = amount,
                Date = dtpDonationDate.Value,
                Notes = txtDonationNotes.Text.Trim()
            };

            if (DataAccess.IsDuplicateDonation(d.MemberId, d.Amount, d.Date))
            {
                if (MessageBox.Show(
                    $"A donation of {amount:C2} for {member.DisplayName} on {d.Date:d} already exists.\n\nAdd it anyway?",
                    "Possible Duplicate", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            }

            try
            {
                DataAccess.AddDonation(d);
                txtDonationAmount.Clear();
                txtDonationNotes.Clear();
                RefreshDonations();
                RefreshStats();
                // Update year combo if needed
                var years = DataAccess.GetAvailableYears();
                cmbDonationYear.Items.Clear();
                cmbDonationYear.Items.Add("(All Years)");
                foreach (var yr in years) cmbDonationYear.Items.Add(yr);
                cmbDonationYear.SelectedIndex = 0;
                SetStatus($"Donation added: ${amount:F2} for {member.DisplayName}");
            }
            catch (Exception ex) { ShowError("Could not add donation: " + ex.Message); }
        }

        private void DonationEdit_Click(object? s, EventArgs e)
        {
            if (!TryGetSelectedId(dgvDonations, out int id)) return;
            var existing = DataAccess.GetDonation(id);
            if (existing == null) return;
            var members = DataAccess.GetAllMembers();
            var types = DataAccess.GetAllOfferingTypes();
            using var dlg = new DonationEditForm(existing, members, types);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            try { DataAccess.UpdateDonation(dlg.Record); RefreshDonations(); RefreshStats(); SetStatus("Donation updated."); }
            catch (Exception ex) { ShowError("Could not update: " + ex.Message); }
        }

        private void DonationDelete_Click(object? s, EventArgs e)
        {
            if (!TryGetSelectedId(dgvDonations, out int id)) return;
            if (MessageBox.Show("Delete this donation?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try { DataAccess.DeleteDonation(id); RefreshDonations(); RefreshStats(); SetStatus("Donation deleted."); }
            catch (Exception ex) { ShowError("Could not delete: " + ex.Message); }
        }

        // ── Reports Tab ──────────────────────────────────────────────────────

        private void BuildReportsTab()
        {
            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
                Padding = new Padding(4), BackColor = SystemColors.Control
            };

            Padding M() => new Padding(2, 4, 2, 4);
            Label RL(string t) { var l = new Label { Text = t, AutoSize = true, Margin = new Padding(4, 7, 2, 0) }; toolbar.Controls.Add(l); return l; }
            Button RB(string t) { var b = new Button { Text = t, AutoSize = true, Margin = M() }; toolbar.Controls.Add(b); return b; }

            RL("Year:");
            cmbReportYear = new ComboBox { Width = 80, DropDownStyle = ComboBoxStyle.DropDownList, Margin = M() };
            toolbar.Controls.Add(cmbReportYear);

            RL("Report:");
            cmbReportType = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, Margin = M() };
            cmbReportType.Items.AddRange(new object[] { "By Member", "By Offering Type", "Tax Receipt Summary", "Year-over-Year by Member", "Year-over-Year by Offering Type" });
            cmbReportType.SelectedIndex = 0;
            toolbar.Controls.Add(cmbReportType);

            RL("Period:");
            cmbReportPeriod = new ComboBox { Width = 110, DropDownStyle = ComboBoxStyle.DropDownList, Margin = M() };
            cmbReportPeriod.Items.AddRange(new object[] { "Annual", "Quarterly", "Monthly" });
            cmbReportPeriod.SelectedIndex = 0;
            toolbar.Controls.Add(cmbReportPeriod);

            // Compare Year — visible only for YoY reports
            var lblCompare = new Label { Text = "vs Year:", AutoSize = true, Margin = new Padding(4, 7, 2, 0), Visible = false };
            toolbar.Controls.Add(lblCompare);
            cmbCompareYear = new ComboBox { Width = 80, DropDownStyle = ComboBoxStyle.DropDownList, Margin = M(), Visible = false };
            toolbar.Controls.Add(cmbCompareYear);

            // Active Only — visible only for Tax Receipt Summary
            chkActiveOnly = new CheckBox { Text = "Active members only", AutoSize = true, Checked = true, Margin = new Padding(8, 7, 4, 0), Visible = false };
            toolbar.Controls.Add(chkActiveOnly);

            var btnRun        = RB("Run Report");
            var btnExport     = RB("Export CSV");
            var btnExportPdf  = RB("Export PDF");
            toolbar.Controls.Add(new Label { Text = "|", AutoSize = true, Margin = new Padding(6, 7, 6, 0), ForeColor = Color.Silver });
            var btnReceipts = RB("Generate Tax Receipts (PDF)");

            cmbReportType.SelectedIndexChanged += (_, _) =>
            {
                bool isYoY     = cmbReportType.Text.StartsWith("Year-over-Year");
                bool isTaxSummary = cmbReportType.Text == "Tax Receipt Summary";
                cmbReportPeriod.Enabled = !isYoY && !isTaxSummary;
                lblCompare.Visible   = isYoY;
                cmbCompareYear.Visible = isYoY;
                chkActiveOnly.Visible  = isTaxSummary;
            };

            btnRun.Click       += ReportRun_Click;
            btnExport.Click    += ReportExport_Click;
            btnExportPdf.Click += ReportExportPdf_Click;
            btnReceipts.Click  += GenerateReceipts_Click;

            dgvReport = MakeGrid();

            tabReports.Controls.Add(dgvReport);
            tabReports.Controls.Add(toolbar);
        }

        private void RefreshReportYears()
        {
            var years = DataAccess.GetAvailableYears();
            cmbReportYear.Items.Clear();
            cmbCompareYear.Items.Clear();
            foreach (var yr in years) { cmbReportYear.Items.Add(yr); cmbCompareYear.Items.Add(yr); }
            if (cmbReportYear.Items.Count > 0) cmbReportYear.SelectedIndex = 0;
            // Default compare year = previous year
            if (cmbCompareYear.Items.Count > 1) cmbCompareYear.SelectedIndex = 1;
            else if (cmbCompareYear.Items.Count == 1) cmbCompareYear.SelectedIndex = 0;
        }

        private void ReportRun_Click(object? s, EventArgs e)
        {
            if (cmbReportYear.SelectedItem is not int year) { ShowError("Select a year."); return; }
            string period = cmbReportPeriod.Text;
            bool isYoY = cmbReportType.Text.StartsWith("Year-over-Year");
            int compareYear = cmbCompareYear.SelectedItem is int cy ? cy : year - 1;
            try
            {
                System.Data.DataTable dt = cmbReportType.Text switch
                {
                    "By Offering Type"              => DataAccess.GetReportByOfferingType(year, period),
                    "Tax Receipt Summary"            => DataAccess.GetReportTaxReceiptSummary(year, chkActiveOnly.Checked),
                    "Year-over-Year by Member"       => DataAccess.GetReportYearOverYear(compareYear, year, byMember: true),
                    "Year-over-Year by Offering Type"=> DataAccess.GetReportYearOverYear(compareYear, year, byMember: false),
                    _                               => DataAccess.GetReportByMember(year, period)
                };
                dgvReport.DataSource = null;
                dgvReport.DataSource = dt;
                if (dgvReport.Columns.Contains("MemberId")) dgvReport.Columns["MemberId"]!.Visible = false;
                foreach (DataGridViewColumn col in dgvReport.Columns)
                    if (col.ValueType == typeof(double) || col.ValueType == typeof(decimal))
                        col.DefaultCellStyle.Format = "C2";
                if (dgvReport.Columns.Contains("Total")) dgvReport.Columns["Total"]!.DefaultCellStyle.Format = "C2";
                if (dgvReport.Columns.Contains("Receipt Total")) dgvReport.Columns["Receipt Total"]!.DefaultCellStyle.Format = "C2";
                string label = isYoY ? $"{compareYear} vs {year}" : $"{period} {year}";
                SetStatus($"{dgvReport.Rows.Count} row(s) — {cmbReportType.Text} {label}");
            }
            catch (Exception ex) { ShowError("Report failed: " + ex.Message); }
        }

        private void ReportExport_Click(object? s, EventArgs e)
        {
            if (dgvReport.Rows.Count == 0) { ShowError("Run a report first."); return; }
            using var dlg = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = $"Report_{cmbReportYear.Text}_{cmbReportType.Text.Replace(" ", "_")}.csv" };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                var dt = (System.Data.DataTable?)dgvReport.DataSource;
                if (dt == null) return;
                var sb = new System.Text.StringBuilder();
                sb.AppendLine(string.Join(",", dt.Columns.Cast<System.Data.DataColumn>().Where(c => c.ColumnName != "MemberId").Select(c => $"\"{c.ColumnName}\"")));
                foreach (System.Data.DataRow row in dt.Rows)
                    sb.AppendLine(string.Join(",", dt.Columns.Cast<System.Data.DataColumn>().Where(c => c.ColumnName != "MemberId").Select(c => $"\"{row[c]}\"")));
                File.WriteAllText(dlg.FileName, sb.ToString());
                SetStatus($"Exported to {dlg.FileName}");
            }
            catch (Exception ex) { ShowError("Export failed: " + ex.Message); }
        }

        private void ReportExportPdf_Click(object? s, EventArgs e)
        {
            if (dgvReport.Rows.Count == 0) { ShowError("Run a report first."); return; }
            var dt = (System.Data.DataTable?)dgvReport.DataSource;
            if (dt == null) return;

            string label = cmbReportType.Text.StartsWith("Year-over-Year")
                ? $"{cmbCompareYear.Text}_vs_{cmbReportYear.Text}"
                : $"{cmbReportYear.Text}_{cmbReportPeriod.Text}";

            using var dlg = new SaveFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf",
                FileName = $"Report_{label}_{cmbReportType.Text.Replace(" ", "_")}.pdf"
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                var title = $"{cmbReportType.Text}  —  {label.Replace('_', ' ')}";
                var church = DataAccess.GetChurchSettings();
                Services.ReportPdfService.GenerateReport(dt, title, church.ChurchName, dlg.FileName);
                SetStatus($"Report saved: {dlg.FileName}");
                if (MessageBox.Show("Report saved. Open PDF?", "Done", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
            }
            catch (Exception ex) { ShowError("Export failed: " + ex.Message); }
        }

        private void GenerateReceipts_Click(object? s, EventArgs e)
        {
            if (cmbReportYear.SelectedItem is not int year) { ShowError("Select a year first."); return; }

            var settings = DataAccess.GetChurchSettings();
            if (string.IsNullOrWhiteSpace(settings.ChurchName))
            {
                ShowError("Please fill in Church Settings before generating receipts.");
                tabMain.SelectedTab = tabSettings;
                return;
            }

            var summary = DataAccess.GetReportTaxReceiptSummary(year, chkActiveOnly.Checked);
            if (summary.Rows.Count == 0) { ShowError($"No taxable donations found for {year}."); return; }

            if (MessageBox.Show(
                $"Generate {summary.Rows.Count} tax receipt PDF(s) for {year}?\n\nThis will increment the receipt counter for each donor.",
                "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                using var progress = new ProgressForm(summary.Rows.Count);
                int done = 0;
                string outputFolder = "";
                var thread = new Thread(() =>
                {
                    var result = PdfReceiptService.GenerateBatchReceipts(summary, year, settings, (n, total, name) =>
                    {
                        done = n;
                        progress.Invoke(() => progress.Update(n, total, name));
                    });
                    outputFolder = result.OutputFolder;
                    progress.Invoke(() => progress.MarkDone(result.Count, result.OutputFolder));
                });
                thread.Start();
                progress.ShowDialog(this);
                thread.Join();

                RefreshStats();
                SetStatus($"{done} receipt(s) generated in {outputFolder}");
                if (!string.IsNullOrEmpty(outputFolder) && Directory.Exists(outputFolder))
                {
                    if (MessageBox.Show("Receipts generated. Open output folder?", "Done", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        System.Diagnostics.Process.Start("explorer.exe", outputFolder);
                }
            }
            catch (Exception ex) { ShowError("Receipt generation failed: " + ex.Message); }
        }

        // ── Settings Tab ─────────────────────────────────────────────────────

        private void BuildSettingsTab()
        {
            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            var tlp = new TableLayoutPanel
            {
                Width = 600,
                AutoSize = true,
                ColumnCount = 2,
                Padding = new Padding(16),
                Location = new Point(0, 0)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360));

            TextBox AddRow(string label, string value, int width = 340)
            {
                tlp.Controls.Add(new Label { Text = label, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill, Padding = new Padding(0, 6, 0, 0) });
                var txt = new TextBox { Text = value, Width = width, Anchor = AnchorStyles.Left | AnchorStyles.Top, Margin = new Padding(0, 4, 0, 4) };
                tlp.Controls.Add(txt);
                return txt;
            }

            txtChurchName = AddRow("Church Name:", "");
            txtChurchAddress = AddRow("Street Address:", "");
            txtChurchCity = AddRow("City:", "");
            txtChurchProvince = AddRow("Province:", "", 60);
            txtChurchPostal = AddRow("Postal Code:", "", 100);
            txtChurchRegNum = AddRow("CRA Reg. Number:", "");
            txtAuthorizedSigner = AddRow("Authorized Signer:", "");

            tlp.Controls.Add(new Label { Text = "Next Receipt #:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill, Padding = new Padding(0, 6, 0, 0) });
            nudNextReceiptNum = new NumericUpDown { Minimum = 1, Maximum = 99999, Width = 100, Anchor = AnchorStyles.Left | AnchorStyles.Top, Margin = new Padding(0, 4, 0, 4) };
            tlp.Controls.Add(nudNextReceiptNum);

            tlp.Controls.Add(new Label { Text = "Church Logo:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill, Padding = new Padding(0, 6, 0, 0) });
            var logoPanel = new FlowLayoutPanel { AutoSize = true, Anchor = AnchorStyles.Left };
            picChurchLogo = new PictureBox { Width = 100, Height = 60, BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.Zoom };
            var btnBrowseLogo = new Button { Text = "Browse...", Width = 80, Height = 26 };
            var btnClearLogo = new Button { Text = "Clear", Width = 60, Height = 26 };
            lblLogoPath = new Label { AutoSize = true, ForeColor = Color.Gray };
            btnBrowseLogo.Click += (_, _) =>
            {
                using var fd = new OpenFileDialog { Filter = "Image files (*.png;*.jpg;*.bmp)|*.png;*.jpg;*.bmp" };
                if (fd.ShowDialog(this) != DialogResult.OK) return;
                try { picChurchLogo.Image = Image.FromFile(fd.FileName); lblLogoPath.Text = fd.FileName; }
                catch { ShowError("Could not load image."); }
            };
            btnClearLogo.Click += (_, _) => { picChurchLogo.Image = null; lblLogoPath.Text = ""; };
            logoPanel.Controls.AddRange(new Control[] { picChurchLogo, btnBrowseLogo, btnClearLogo, lblLogoPath });
            tlp.Controls.Add(logoPanel);

            tlp.Controls.Add(new Label());
            var btnSave = new Button { Text = "Save Settings", Width = 130, Height = 30, Margin = new Padding(0, 8, 0, 0) };
            btnSave.Click += SaveSettings_Click;
            tlp.Controls.Add(btnSave);

            scroll.Controls.Add(tlp);
            tabSettings.Controls.Add(scroll);
        }

        private void LoadSettings()
        {
            var s = DataAccess.GetChurchSettings();
            txtChurchName.Text = s.ChurchName;
            txtChurchAddress.Text = s.Address;
            txtChurchCity.Text = s.City;
            txtChurchProvince.Text = s.Province;
            txtChurchPostal.Text = s.PostalCode;
            txtChurchRegNum.Text = s.RegNumber;
            txtAuthorizedSigner.Text = s.AuthorizedSigner;
            nudNextReceiptNum.Value = s.NextReceiptNumber;

            if (!string.IsNullOrEmpty(s.LogoPath) && File.Exists(s.LogoPath))
            {
                try { picChurchLogo.Image = Image.FromFile(s.LogoPath); lblLogoPath.Text = s.LogoPath; }
                catch { }
            }
        }

        private void SaveSettings_Click(object? s, EventArgs e)
        {
            var settings = new ChurchSettings
            {
                ChurchName = txtChurchName.Text.Trim(),
                Address = txtChurchAddress.Text.Trim(),
                City = txtChurchCity.Text.Trim(),
                Province = txtChurchProvince.Text.Trim(),
                PostalCode = txtChurchPostal.Text.Trim(),
                RegNumber = txtChurchRegNum.Text.Trim(),
                AuthorizedSigner = txtAuthorizedSigner.Text.Trim(),
                LogoPath = lblLogoPath.Text.Trim(),
                NextReceiptNumber = (int)nudNextReceiptNum.Value
            };
            try { DataAccess.SaveChurchSettings(settings); RefreshStats(); SetStatus("Settings saved."); }
            catch (Exception ex) { ShowError("Could not save settings: " + ex.Message); }
        }

        // ── Multi-church ─────────────────────────────────────────────────────

        private void SwitchChurch_Click(object? s, EventArgs e)
        {
            var churches = DataAccess.GetAvailableChurches();
            using var dlg = new Forms.ChurchSelectorForm(churches, DataAccess.CurrentChurch);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            if (dlg.PendingRename.HasValue)
                DataAccess.RenameChurch(dlg.PendingRename.Value.From, dlg.PendingRename.Value.To);
            DataAccess.SwitchChurch(dlg.SelectedChurch);
            RefreshAll();
            UpdateTitle();
        }

        private void AddChurch_Click(object? s, EventArgs e)
        {
            using var dlg = new Forms.ChurchSelectorForm(DataAccess.GetAvailableChurches(), DataAccess.CurrentChurch, createMode: true);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            DataAccess.CreateChurch(dlg.SelectedChurch);
            RefreshAll();
            UpdateTitle();
        }

        // ── Mailing Labels ───────────────────────────────────────────────────

        private void PrintMailingLabels_Click(object? s, EventArgs e)
        {
            // Respect current search filter — labels for currently visible members
            var members = DataAccess.GetAllMembers();
            if (!string.IsNullOrWhiteSpace(txtMemberSearch.Text))
            {
                var q = txtMemberSearch.Text.ToLowerInvariant();
                members = members.Where(m =>
                    m.FirstName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    m.LastName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    m.EnvelopeNumber.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    m.City.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (members.Count == 0) { ShowError("No members to print labels for."); return; }

            using var save = new SaveFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf",
                FileName = $"MailingLabels_{DataAccess.CurrentChurch}_{DateTime.Today:yyyyMMdd}.pdf"
            };
            if (save.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                Services.MailingLabelService.GenerateLabels(members, save.FileName);
                SetStatus($"Mailing labels saved: {save.FileName}");
                if (MessageBox.Show("Labels generated. Open PDF?", "Done", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(save.FileName) { UseShellExecute = true });
            }
            catch (Exception ex) { ShowError("Could not generate labels: " + ex.Message); }
        }

        // ── Backup ───────────────────────────────────────────────────────────

        private void BackupToOneDrive_Click(object? s, EventArgs e)
        {
            // Locate OneDrive root — try the env var first, then common folder names.
            string? oneDrive = Environment.GetEnvironmentVariable("OneDrive")
                ?? Environment.GetEnvironmentVariable("OneDriveConsumer")
                ?? Environment.GetEnvironmentVariable("OneDriveCommercial");

            if (string.IsNullOrEmpty(oneDrive) || !Directory.Exists(oneDrive))
            {
                // Fall back: look for a OneDrive folder inside the user profile.
                string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                foreach (var candidate in new[] { "OneDrive", "OneDrive - Personal", "OneDrive - Business" })
                {
                    string path = Path.Combine(profile, candidate);
                    if (Directory.Exists(path)) { oneDrive = path; break; }
                }
            }

            if (string.IsNullOrEmpty(oneDrive) || !Directory.Exists(oneDrive))
            {
                ShowError("OneDrive folder not found on this machine.\n" +
                          "Make sure OneDrive is installed and signed in.");
                return;
            }

            string backupDir = Path.Combine(oneDrive, "EnvelopeSteward", "Backups");
            Directory.CreateDirectory(backupDir);

            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string dest  = Path.Combine(backupDir, $"envelopes_backup_{stamp}.db");

            try
            {
                // Flush WAL and close all pooled connections before copying.
                SqliteConnection.ClearAllPools();
                File.Copy(DataAccess.DbPath, dest, overwrite: false);

                var result = MessageBox.Show(
                    $"Backup saved to:\n{dest}\n\nOpen backup folder?",
                    "Backup Complete", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                    System.Diagnostics.Process.Start("explorer.exe", backupDir);

                SetStatus($"Backup saved: envelopes_backup_{stamp}.db");
            }
            catch (Exception ex) { ShowError("Backup failed: " + ex.Message); }
        }

        // ── CSV Imports ──────────────────────────────────────────────────────

        private void ImportMembers_Click(object? s, EventArgs e)
        {
            using var dlg = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*" };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                var r = DataAccess.ImportMembersFromCsv(dlg.FileName);
                RefreshMembers(); RefreshDonationCombos();
                var extra = r.NewColumns.Length > 0 ? $"  New columns added: {string.Join(", ", r.NewColumns)}." : "";
                SetStatus($"Members imported — {r.Inserted} added, {r.Updated} updated.{extra}");
            }
            catch (Exception ex) { ShowError("Import failed: " + ex.Message); }
        }

        private void ImportOfferings_Click(object? s, EventArgs e)
        {
            using var dlg = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*" };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                var r = DataAccess.ImportOfferingTypesFromCsv(dlg.FileName);
                RefreshOfferingTypes(); RefreshDonationCombos();
                var extra = r.NewColumns.Length > 0 ? $"  New columns added: {string.Join(", ", r.NewColumns)}." : "";
                SetStatus($"Offering types imported — {r.Inserted} added, {r.Updated} updated.{extra}");
            }
            catch (Exception ex) { ShowError("Import failed: " + ex.Message); }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static DataGridView MakeGrid()
        {
            var g = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
                BackgroundColor = SystemColors.Window,
                BorderStyle = BorderStyle.None
            };
            // Prevent the default error dialog from popping up on type mismatches.
            g.DataError += (_, e) => e.ThrowException = false;
            return g;
        }

        // Selects the row under the cursor on right-click so context menus
        // work without requiring a prior left-click to select the row.
        private static void GridSelectRowOnRightClick(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || sender is not DataGridView dgv) return;
            var hit = dgv.HitTest(e.X, e.Y);
            if (hit.RowIndex >= 0) dgv.Rows[hit.RowIndex].Selected = true;
        }

        private static bool TryGetSelectedId(DataGridView dgv, out int id)
        {
            id = 0;
            if (dgv.SelectedRows.Count == 0) return false;
            var row = dgv.SelectedRows[0];
            if (!dgv.Columns.Contains("Id")) return false;
            var cell = row.Cells["Id"];
            if (cell?.Value == null || cell.Value == DBNull.Value) return false;
            id = Convert.ToInt32(cell.Value);
            return true;
        }

        private void SetStatus(string msg) => toolStripStatusLabel1.Text = msg;
        private void ShowError(string msg) => MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    // ── Progress Dialog ──────────────────────────────────────────────────────

    internal class ProgressForm : Form
    {
        private readonly ProgressBar pb;
        private readonly Label lblStatus;
        private readonly Label lblFolder;
        private readonly Button btnClose;

        public ProgressForm(int total)
        {
            Text = "Generating Receipts...";
            Size = new Size(520, 210);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false; MinimizeBox = false;
            ControlBox = false;
            Padding = new Padding(12);

            lblStatus = new Label
            {
                Text = "Starting...", AutoSize = false,
                Dock = DockStyle.Top, Height = 28,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pb = new ProgressBar
            {
                Dock = DockStyle.Top, Height = 24, Maximum = total,
                Style = ProgressBarStyle.Continuous
            };
            lblFolder = new Label
            {
                AutoSize = false, Dock = DockStyle.Top, Height = 52,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(Font.FontFamily, 8f),
                ForeColor = System.Drawing.Color.FromArgb(60, 60, 60)
            };

            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom, Height = 44,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 6, 0, 0)
            };
            btnClose = new Button
            {
                Text = "Close", Width = 88, Height = 30,
                Enabled = false, DialogResult = DialogResult.OK
            };
            btnPanel.Controls.Add(btnClose);

            // Add in reverse order for Dock stacking
            Controls.AddRange(new Control[] { btnPanel, lblFolder, pb, lblStatus });
        }

        public void Update(int done, int total, string name)
        {
            pb.Value = done;
            lblStatus.Text = $"Generating {done} of {total} — {name}";
        }

        public void MarkDone(int count, string folder)
        {
            Text = "Receipts Generated";
            lblStatus.Text = $"✓  {count} receipt(s) generated successfully.";
            lblFolder.Text = $"Saved to:\n{folder}";
            btnClose.Enabled = true;
            ControlBox = true;
        }
    }
}
