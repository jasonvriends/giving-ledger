namespace Envelope_Steward.Forms
{
    public class ChurchSelectorForm : Form
    {
        public string SelectedChurch { get; private set; } = "";

        public ChurchSelectorForm(string[] existing, string current, bool createMode = false)
        {
            Text = createMode ? "Add New Congregation" : "Switch Congregation";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false; MinimizeBox = false;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            MinimumSize = new Size(360, 0);

            var tlp = new TableLayoutPanel
            {
                ColumnCount = 1,
                Padding = new Padding(16),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(330, 0)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            if (createMode)
            {
                tlp.Controls.Add(new Label { Text = "Enter a short name for the new congregation:", AutoSize = true, Margin = new Padding(0, 0, 0, 4) });
                var txtName = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 8) };

                var btnOk     = new Button { Text = "Create",  DialogResult = DialogResult.OK,     Width = 90 };
                var btnCancel = new Button { Text = "Cancel",  DialogResult = DialogResult.Cancel, Width = 80 };
                btnOk.Click += (_, _) =>
                {
                    var name = SanitizeName(txtName.Text);
                    if (string.IsNullOrEmpty(name))
                    {
                        MessageBox.Show("Please enter a valid name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    SelectedChurch = name;
                };

                var btnRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft };
                btnRow.Controls.AddRange(new Control[] { btnCancel, btnOk });

                tlp.Controls.Add(txtName);
                tlp.Controls.Add(btnRow);
                AcceptButton = btnOk;
                CancelButton = btnCancel;
            }
            else
            {
                tlp.Controls.Add(new Label { Text = "Select a congregation:", AutoSize = true, Margin = new Padding(0, 0, 0, 4) });

                var listBox = new ListBox { Dock = DockStyle.Fill, Height = 160, Margin = new Padding(0, 0, 0, 8) };
                foreach (var c in existing) listBox.Items.Add(c);
                // Pre-select current
                int cur = listBox.Items.IndexOf(current);
                if (cur >= 0) listBox.SelectedIndex = cur;

                var btnOk     = new Button { Text = "Open",   DialogResult = DialogResult.OK,     Width = 80 };
                var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 80 };
                btnOk.Click += (_, _) =>
                {
                    if (listBox.SelectedItem is not string sel)
                    {
                        MessageBox.Show("Please select a congregation.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    SelectedChurch = sel;
                };
                listBox.DoubleClick += (_, _) => { btnOk.PerformClick(); };

                var btnRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft };
                btnRow.Controls.AddRange(new Control[] { btnCancel, btnOk });

                tlp.Controls.Add(listBox);
                tlp.Controls.Add(btnRow);
                AcceptButton = btnOk;
                CancelButton = btnCancel;
            }

            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 0 }; // dummy; buttons are inline
            Controls.Add(tlp);
        }

        private static string SanitizeName(string input)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return new string(input.Trim().Where(c => !invalid.Contains(c) && c != '.' && c != ' ').ToArray())
                .Replace(" ", "_");
        }
    }
}
