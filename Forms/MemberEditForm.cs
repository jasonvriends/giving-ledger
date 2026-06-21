using Envelope_Steward.Models;

namespace Envelope_Steward.Forms
{
    public class MemberEditForm : Form
    {
        public MemberRecord Member { get; private set; }

        private readonly TextBox txtEnvelope, txtFirstName, txtLastName,
            txtStreet, txtCity, txtProvince, txtPostal, txtPhone, txtEmail;
        private readonly CheckBox chkFullMember, chkShutIn, chkActive;

        public MemberEditForm(MemberRecord? existing = null)
        {
            Member = existing != null
                ? new MemberRecord
                {
                    Id = existing.Id,
                    EnvelopeNumber = existing.EnvelopeNumber,
                    FirstName = existing.FirstName,
                    LastName = existing.LastName,
                    StreetAddress = existing.StreetAddress,
                    City = existing.City,
                    Province = existing.Province,
                    PostalCode = existing.PostalCode,
                    HomePhone = existing.HomePhone,
                    Email = existing.Email,
                    FullMember = existing.FullMember,
                    ShutIn = existing.ShutIn,
                    Active = existing.Active,
                    Status = existing.Status
                }
                : new MemberRecord { Active = true };

            bool isEdit = existing?.Id > 0;

            Text = existing == null ? "Add Member" : "Edit Member";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false; MinimizeBox = false;

            var tlp = new TableLayoutPanel
            {
                ColumnCount = 2,
                Padding = new Padding(12),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(456, 0)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            txtEnvelope  = Add(tlp, "Envelope #:",     Member.EnvelopeNumber);
            txtFirstName = Add(tlp, "First Name:",     Member.FirstName);
            txtLastName  = Add(tlp, "Last Name:",      Member.LastName);
            txtStreet    = Add(tlp, "Street Address:", Member.StreetAddress);
            txtCity      = Add(tlp, "City:",           Member.City);
            txtProvince  = Add(tlp, "Province:",       Member.Province);
            txtPostal    = Add(tlp, "Postal Code:",    Member.PostalCode);
            txtPhone     = Add(tlp, "Home Phone:",     Member.HomePhone);
            txtEmail     = Add(tlp, "Email:",          Member.Email);

            chkFullMember = new CheckBox { Text = "Full Member", Checked = Member.FullMember, Anchor = AnchorStyles.Left };
            chkShutIn     = new CheckBox { Text = "Shut-In",     Checked = Member.ShutIn,     Anchor = AnchorStyles.Left };
            chkActive     = new CheckBox { Text = "Active",      Checked = Member.Active,     Anchor = AnchorStyles.Left };

            tlp.Controls.Add(new Label { Text = "Flags:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill });
            var flagPanel = new FlowLayoutPanel { AutoSize = true, Anchor = AnchorStyles.Left };
            flagPanel.Controls.AddRange(new Control[] { chkFullMember, chkShutIn, chkActive });
            tlp.Controls.Add(flagPanel);

            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 44,
                Padding = new Padding(8)
            };
            var btnOk     = new Button { Text = "Save",   AutoSize = true, Margin = new Padding(2, 4, 2, 4) };
            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true, Margin = new Padding(2, 4, 2, 4) };
            btnOk.Click += (_, _) =>
            {
                Member.EnvelopeNumber = txtEnvelope.Text.Trim();
                Member.FirstName      = txtFirstName.Text.Trim();
                Member.LastName       = txtLastName.Text.Trim();
                Member.StreetAddress  = txtStreet.Text.Trim();
                Member.City           = txtCity.Text.Trim();
                Member.Province       = txtProvince.Text.Trim();
                Member.PostalCode     = txtPostal.Text.Trim();
                Member.HomePhone      = txtPhone.Text.Trim();
                Member.Email          = txtEmail.Text.Trim();
                Member.FullMember     = chkFullMember.Checked;
                Member.ShutIn         = chkShutIn.Checked;
                Member.Active         = chkActive.Checked;
                DialogResult = DialogResult.OK;
            };
            btnPanel.Controls.AddRange(new Control[] { btnCancel, btnOk });
            AcceptButton = btnOk;
            CancelButton = btnCancel;

            if (isEdit)
            {
                // Tabbed layout with Details + Giving History
                AutoSize = false;
                Size = new Size(520, 530);
                MinimumSize = new Size(520, 530);

                var innerTabs = new TabControl { Dock = DockStyle.Fill };
                var tabDetails = new TabPage("Details");
                var tabHistory = new TabPage("Giving History");

                tabDetails.Controls.Add(tlp);
                tabHistory.Controls.Add(BuildHistoryPanel(existing!.Id));
                innerTabs.TabPages.AddRange(new[] { tabDetails, tabHistory });

                Controls.Add(btnPanel);
                Controls.Add(innerTabs);
            }
            else
            {
                // Simple auto-size layout for add mode
                AutoSize = true;
                AutoSizeMode = AutoSizeMode.GrowAndShrink;
                MinimumSize = new Size(480, 0);

                Controls.Add(btnPanel);
                Controls.Add(tlp);
            }
        }

        private static DataGridView BuildHistoryPanel(int memberId)
        {
            var dgv = new DataGridView
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
            dgv.DataError += (_, e) => e.ThrowException = false;

            var dt = DataAccess.GetDonationsDataTable(memberId: memberId);
            dgv.DataSource = dt;
            dgv.DataBindingComplete += (_, _) =>
            {
                if (dgv.Columns.Contains("Id"))     dgv.Columns["Id"]!.Visible = false;
                if (dgv.Columns.Contains("Env #"))  dgv.Columns["Env #"]!.Visible = false;
                if (dgv.Columns.Contains("Member")) dgv.Columns["Member"]!.Visible = false;
                if (dgv.Columns.Contains("Amount")) dgv.Columns["Amount"]!.DefaultCellStyle.Format = "C2";
            };
            return dgv;
        }

        private static TextBox Add(TableLayoutPanel tlp, string label, string value)
        {
            tlp.Controls.Add(new Label { Text = label, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill });
            var txt = new TextBox { Text = value, Dock = DockStyle.Fill };
            tlp.Controls.Add(txt);
            return txt;
        }
    }
}
