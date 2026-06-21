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

        // Congregation menu (rebuilt dynamically)
        private ToolStripMenuItem _congMenu = null!;

        // Status bar
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private ToolStripStatusLabel lblStatYtd, lblStatDonors, lblStatReceipt;

        // Sidebar nav
        private readonly Theme.NavButton[] _navBtns = new Theme.NavButton[5];

        // Sidebar stat labels (updated by RefreshStats)
        private Label lblSideYtdValue = null!;
        private Label lblSideDonors   = null!;
        private Label lblSideReceipt  = null!;
        private Label lblBrandName    = null!;
        private Label lblBrandCity    = null!;

        // Page header labels
        private Label lblPageTitle = null!;
        private Label lblPageDesc  = null!;

        // View panel container (Dock=Fill inside pnlMain)
        private Panel pnlContent = null!;

        // Maps nav id → content panel (populated in BuildUI)
        private readonly Dictionary<string, Panel> _views = [];

        private void InitializeComponent()
        {
            SuspendLayout();
            Text             = "Giving Ledger";
            ClientSize       = new Size(1280, 760);
            MinimumSize      = new Size(940, 600);
            StartPosition    = FormStartPosition.CenterScreen;
            BackColor        = Theme.GL.App;
            ResumeLayout(false);
        }
    }
}
