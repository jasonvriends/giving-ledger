using Envelope_Steward.Models;

namespace Envelope_Steward.Forms
{
    public class OfferingTypeEditForm : Form
    {
        public OfferingTypeRecord Record { get; private set; }

        public OfferingTypeEditForm(OfferingTypeRecord? existing = null)
        {
            Record = existing != null
                ? new OfferingTypeRecord { Id = existing.Id, Name = existing.Name, Description = existing.Description, TaxReceiptable = existing.TaxReceiptable }
                : new OfferingTypeRecord { TaxReceiptable = true };

            Text = existing == null ? "Add Offering Type" : "Edit Offering Type";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false; MinimizeBox = false;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            MinimumSize = new Size(440, 0);

            var tlp = new TableLayoutPanel
            {
                ColumnCount = 2,
                Padding = new Padding(12),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(416, 0)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            tlp.Controls.Add(new Label { Text = "Code / Short Name:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill });
            var txtName = new TextBox { Text = Record.Name, Dock = DockStyle.Fill };
            tlp.Controls.Add(txtName);

            tlp.Controls.Add(new Label { Text = "Description:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill });
            var txtDesc = new TextBox { Text = Record.Description, Dock = DockStyle.Fill };
            tlp.Controls.Add(txtDesc);

            tlp.Controls.Add(new Label { Text = "Tax Receiptable:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill });
            var chkTax = new CheckBox { Checked = Record.TaxReceiptable, Text = "Qualifies for tax receipt", Anchor = AnchorStyles.Left };
            tlp.Controls.Add(chkTax);

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
                if (string.IsNullOrWhiteSpace(txtName.Text) && string.IsNullOrWhiteSpace(txtDesc.Text))
                {
                    MessageBox.Show("Please enter a code or description.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                Record.Name = txtName.Text.Trim();
                Record.Description = txtDesc.Text.Trim();
                Record.TaxReceiptable = chkTax.Checked;
            };
            btnPanel.Controls.AddRange(new Control[] { btnCancel, btnOk });
            AcceptButton = btnOk;
            CancelButton = btnCancel;

            Controls.Add(btnPanel);
            Controls.Add(tlp);
        }
    }
}
