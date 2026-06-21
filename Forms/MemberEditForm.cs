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

            Text = existing == null ? "Add Member" : "Edit Member";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false; MinimizeBox = false;
            Size = new Size(460, 460);

            var tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                Padding = new Padding(12),
                AutoSize = true
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            txtEnvelope = Add(tlp, "Envelope #:", Member.EnvelopeNumber);
            txtFirstName = Add(tlp, "First Name:", Member.FirstName);
            txtLastName = Add(tlp, "Last Name:", Member.LastName);
            txtStreet = Add(tlp, "Street Address:", Member.StreetAddress);
            txtCity = Add(tlp, "City:", Member.City);
            txtProvince = Add(tlp, "Province:", Member.Province);
            txtPostal = Add(tlp, "Postal Code:", Member.PostalCode);
            txtPhone = Add(tlp, "Home Phone:", Member.HomePhone);
            txtEmail = Add(tlp, "Email:", Member.Email);

            chkFullMember = new CheckBox { Text = "Full Member", Checked = Member.FullMember, Anchor = AnchorStyles.Left };
            chkShutIn = new CheckBox { Text = "Shut-In", Checked = Member.ShutIn, Anchor = AnchorStyles.Left };
            chkActive = new CheckBox { Text = "Active", Checked = Member.Active, Anchor = AnchorStyles.Left };

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
            var btnOk = new Button { Text = "Save", DialogResult = DialogResult.OK, Width = 80 };
            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 80 };
            btnOk.Click += (_, _) =>
            {
                Member.EnvelopeNumber = txtEnvelope.Text.Trim();
                Member.FirstName = txtFirstName.Text.Trim();
                Member.LastName = txtLastName.Text.Trim();
                Member.StreetAddress = txtStreet.Text.Trim();
                Member.City = txtCity.Text.Trim();
                Member.Province = txtProvince.Text.Trim();
                Member.PostalCode = txtPostal.Text.Trim();
                Member.HomePhone = txtPhone.Text.Trim();
                Member.Email = txtEmail.Text.Trim();
                Member.FullMember = chkFullMember.Checked;
                Member.ShutIn = chkShutIn.Checked;
                Member.Active = chkActive.Checked;
            };
            btnPanel.Controls.AddRange(new Control[] { btnCancel, btnOk });
            AcceptButton = btnOk;
            CancelButton = btnCancel;

            Controls.Add(tlp);
            Controls.Add(btnPanel);
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
