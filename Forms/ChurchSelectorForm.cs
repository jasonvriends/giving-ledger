namespace Envelope_Steward.Forms
{
    public class ChurchSelectorForm : Form
    {
        public string SelectedChurch { get; private set; } = "";

        // Set when a rename was performed; caller must call DataAccess.RenameChurch().
        public (string From, string To)? PendingRename { get; private set; }

        public ChurchSelectorForm(string[] existing, string current, bool createMode = false)
        {
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition   = FormStartPosition.CenterParent;
            MaximizeBox = false; MinimizeBox = false;

            if (createMode)
                BuildCreateMode();
            else
                BuildSwitchMode(existing, current);
        }

        // ── Create mode ──────────────────────────────────────────────────────

        private void BuildCreateMode()
        {
            Text = "Add New Congregation";
            Size = new Size(420, 160);

            var lbl = new Label
            {
                Text = "Enter a short name for the new congregation:",
                Dock = DockStyle.Top, Height = 28,
                Padding = new Padding(12, 8, 0, 0)
            };
            var txtName = new TextBox
            {
                Dock = DockStyle.Top,
                Margin = new Padding(12, 0, 12, 0)
            };

            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom, Height = 44,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(8)
            };
            var btnOk     = new Button { Text = "Create", DialogResult = DialogResult.OK,     AutoSize = true, Margin = new Padding(2, 4, 2, 4) };
            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true, Margin = new Padding(2, 4, 2, 4) };
            btnOk.Click += (_, _) =>
            {
                var name = Sanitize(txtName.Text);
                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("Please enter a valid name (letters, numbers, underscores).", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                SelectedChurch = name;
            };
            btnPanel.Controls.AddRange(new Control[] { btnCancel, btnOk });
            AcceptButton = btnOk;
            CancelButton = btnCancel;

            // Dock order: Bottom first, then Top controls stack downward
            Controls.AddRange(new Control[] { btnPanel, txtName, lbl });
        }

        // ── Switch mode ──────────────────────────────────────────────────────

        private void BuildSwitchMode(string[] existing, string current)
        {
            Text = "Switch Congregation";
            Size = new Size(420, 320);

            var listBox = new ListBox { Dock = DockStyle.Fill };
            foreach (var c in existing) listBox.Items.Add(c);
            int cur = listBox.Items.IndexOf(current);
            if (cur >= 0) listBox.SelectedIndex = cur;

            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom, Height = 44,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(8)
            };

            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true, Margin = new Padding(2, 4, 2, 4) };
            var btnOpen   = new Button { Text = "Open",   DialogResult = DialogResult.OK,     AutoSize = true, Margin = new Padding(2, 4, 2, 4) };
            var btnRename = new Button { Text = "Rename…", AutoSize = true, Margin = new Padding(2, 4, 2, 4) };

            btnOpen.Click += (_, _) =>
            {
                if (listBox.SelectedItem is not string sel)
                {
                    MessageBox.Show("Please select a congregation.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                SelectedChurch = sel;
            };

            btnRename.Click += (_, _) =>
            {
                if (listBox.SelectedItem is not string sel) return;
                var newName = PromptRename(sel);
                if (newName == null || newName == sel) return;

                // Validate: no duplicates
                if (listBox.Items.Cast<string>().Any(s => s == newName))
                {
                    MessageBox.Show($"A congregation named \"{newName}\" already exists.", "Conflict", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int idx = listBox.Items.IndexOf(sel);
                listBox.Items[idx] = newName;
                listBox.SelectedIndex = idx;
                PendingRename = (sel, newName);
            };

            listBox.DoubleClick += (_, _) => btnOpen.PerformClick();

            // Right-to-left order so Open is on the right
            btnPanel.Controls.AddRange(new Control[] { btnCancel, btnOpen, btnRename });
            AcceptButton = btnOpen;
            CancelButton = btnCancel;

            Controls.Add(listBox);
            Controls.Add(btnPanel);
        }

        private string? PromptRename(string current)
        {
            using var dlg = new Form
            {
                Text = "Rename Congregation",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false, MinimizeBox = false,
                Size = new Size(400, 160)
            };
            var lbl = new Label
            {
                Text = $"New name for \"{current}\":",
                Dock = DockStyle.Top, Height = 28,
                Padding = new Padding(12, 8, 0, 0)
            };
            var txt = new TextBox
            {
                Text = current,
                Dock = DockStyle.Top,
                Margin = new Padding(12, 0, 12, 0)
            };

            var bp = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
            var ok = new Button { Text = "Rename", DialogResult = DialogResult.OK, Width = 80 };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 80 };
            ok.Click += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(Sanitize(txt.Text)))
                {
                    MessageBox.Show("Please enter a valid name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            };
            bp.Controls.AddRange(new Control[] { cancel, ok });
            dlg.AcceptButton = ok; dlg.CancelButton = cancel;
            // Add in reverse dock order: Bottom first, then Top controls stack downward
            dlg.Controls.AddRange(new Control[] { bp, txt, lbl });
            Load += (_, _) => txt.SelectAll();

            return dlg.ShowDialog(this) == DialogResult.OK ? Sanitize(txt.Text) : null;
        }

        private static string Sanitize(string input)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var s = new string(input.Trim().Where(c => !invalid.Contains(c) && c != '.').ToArray()).Replace(' ', '_');
            return s;
        }
    }
}
