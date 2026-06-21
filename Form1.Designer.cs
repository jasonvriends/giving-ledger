namespace Envelope_Steward
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        // Tab pages
        private TabControl tabMain;
        private TabPage tabMembers, tabOfferingTypes, tabDonations, tabReports, tabSettings;

        // Members tab
        private DataGridView dgvMembers;
        private TextBox txtMemberSearch;

        // Offering Types tab
        private DataGridView dgvOfferingTypes;

        // Donations tab
        private DataGridView dgvDonations;
        private ComboBox cmbDonationMember, cmbDonationOfferingType, cmbDonationYear, cmbDonationMemberFilter;
        private DateTimePicker dtpDonationDate;
        private TextBox txtDonationAmount, txtDonationNotes;

        // Reports tab
        private DataGridView dgvReport;
        private ComboBox cmbReportYear, cmbReportType, cmbReportPeriod, cmbCompareYear;
        private CheckBox chkActiveOnly;

        // Settings tab
        private TextBox txtChurchName, txtChurchAddress, txtChurchCity,
                        txtChurchProvince, txtChurchPostal, txtChurchRegNum, txtAuthorizedSigner;
        private NumericUpDown nudNextReceiptNum;
        private PictureBox picChurchLogo;
        private Label lblLogoPath;

        // Status
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private ToolStripStatusLabel lblStatYtd, lblStatDonors, lblStatReceipt;

        private void InitializeComponent()
        {
            SuspendLayout();
            Text = "Giving Ledger";
            ClientSize = new Size(1280, 760);
            MinimumSize = new Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;
            ResumeLayout(false);
        }
    }
}
