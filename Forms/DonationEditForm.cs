using Envelope_Steward.Models;

namespace Envelope_Steward.Forms
{
    public class DonationEditForm : Form
    {
        public DonationRecord Record { get; private set; }

        public DonationEditForm(DonationRecord existing, List<MemberRecord> members, List<OfferingTypeRecord> offeringTypes)
        {
            Record = new DonationRecord
            {
                Id = existing.Id,
                MemberId = existing.MemberId,
                OfferingTypeId = existing.OfferingTypeId,
                Amount = existing.Amount,
                Date = existing.Date,
                Notes = existing.Notes
            };

            Text = "Edit Donation";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false; MinimizeBox = false;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            MinimumSize = new Size(500, 0);

            var tlp = new TableLayoutPanel
            {
                ColumnCount = 2,
                Padding = new Padding(12),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(476, 0)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            tlp.Controls.Add(new Label { Text = "Member:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill });
            var cmbMember = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var m in members.OrderBy(x => x.EnvelopeNumber))
                cmbMember.Items.Add(m);
            cmbMember.DisplayMember = "ComboLabel";
            cmbMember.SelectedItem = members.FirstOrDefault(m => m.Id == existing.MemberId);
            tlp.Controls.Add(cmbMember);

            tlp.Controls.Add(new Label { Text = "Date:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill });
            var dtp = new DateTimePicker { Value = existing.Date, Format = DateTimePickerFormat.Short, Dock = DockStyle.Fill };
            tlp.Controls.Add(dtp);

            tlp.Controls.Add(new Label { Text = "Offering Type:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill });
            var cmbType = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var t in offeringTypes)
                cmbType.Items.Add(t);
            cmbType.DisplayMember = "DisplayName";
            cmbType.SelectedItem = offeringTypes.FirstOrDefault(t => t.Id == existing.OfferingTypeId);
            tlp.Controls.Add(cmbType);

            tlp.Controls.Add(new Label { Text = "Amount ($):", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill });
            var txtAmount = new TextBox { Text = existing.Amount.ToString("F2"), Dock = DockStyle.Fill };
            tlp.Controls.Add(txtAmount);

            tlp.Controls.Add(new Label { Text = "Notes:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill });
            var txtNotes = new TextBox { Text = existing.Notes, Dock = DockStyle.Fill };
            tlp.Controls.Add(txtNotes);

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
                if (!decimal.TryParse(txtAmount.Text, out decimal amt) || amt <= 0)
                {
                    MessageBox.Show("Please enter a valid amount.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (cmbMember.SelectedItem is not MemberRecord selMember)
                {
                    MessageBox.Show("Please select a member.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (cmbType.SelectedItem is not OfferingTypeRecord selType)
                {
                    MessageBox.Show("Please select an offering type.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                Record.MemberId = selMember.Id;
                Record.OfferingTypeId = selType.Id;
                Record.Amount = amt;
                Record.Date = dtp.Value;
                Record.Notes = txtNotes.Text.Trim();
            };
            btnPanel.Controls.AddRange(new Control[] { btnCancel, btnOk });
            AcceptButton = btnOk;
            CancelButton = btnCancel;

            // btnPanel docked Bottom, tlp above it; form AutoSizes to combined height
            Controls.Add(btnPanel);
            Controls.Add(tlp);
        }
    }
}
