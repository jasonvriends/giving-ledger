using Envelope_Steward.Services;

namespace Envelope_Steward.Forms
{
    public class LabelSizePickerForm : Form
    {
        public LabelSpec? SelectedSpec { get; private set; }

        public LabelSizePickerForm()
        {
            Text = "Choose Label Size";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false; MinimizeBox = false;
            Size = new Size(500, 260);

            var lbl = new Label
            {
                Text = "Select the label format you are printing on:",
                Dock = DockStyle.Top, Height = 30,
                Padding = new Padding(12, 10, 0, 0)
            };

            var listBox = new ListBox
            {
                Dock = DockStyle.Fill,
                IntegralHeight = false
            };
            foreach (var s in LabelSpec.BuiltIn)
                listBox.Items.Add(s.DisplayName);
            listBox.SelectedIndex = 0;

            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom, Height = 44,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(8)
            };
            var btnOk     = new Button { Text = "Print Labels", AutoSize = true, Margin = new Padding(2, 4, 2, 4) };
            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true, Margin = new Padding(2, 4, 2, 4) };
            btnOk.Click += (_, _) =>
            {
                int idx = listBox.SelectedIndex;
                if (idx < 0) { MessageBox.Show("Please select a label size.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                SelectedSpec = LabelSpec.BuiltIn[idx];
                DialogResult = DialogResult.OK;
            };
            listBox.DoubleClick += (_, _) => btnOk.PerformClick();

            btnPanel.Controls.AddRange(new Control[] { btnCancel, btnOk });
            AcceptButton = btnOk;
            CancelButton = btnCancel;

            Controls.Add(listBox);
            Controls.Add(btnPanel);
            Controls.Add(lbl);
        }
    }
}
