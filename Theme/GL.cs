using System.Drawing.Drawing2D;

namespace Envelope_Steward.Theme;

internal static class GL
{
    // ── Color tokens ────────────────────────────────────────────────────────
    public static readonly Color Bg          = Color.FromArgb(0xEF, 0xE7, 0xDA);
    public static readonly Color App         = Color.FromArgb(0xF7, 0xF1, 0xE8);
    public static readonly Color Surface     = Color.FromArgb(0xFF, 0xFD, 0xFA);
    public static readonly Color Surface2    = Color.FromArgb(0xF5, 0xEE, 0xE2);
    public static readonly Color Surface3    = Color.FromArgb(0xEF, 0xE6, 0xD6);
    public static readonly Color Ink         = Color.FromArgb(0x2A, 0x25, 0x21);
    public static readonly Color InkSoft     = Color.FromArgb(0x6F, 0x65, 0x5A);
    public static readonly Color InkFaint    = Color.FromArgb(0x9A, 0x8E, 0x7E);
    public static readonly Color Line        = Color.FromArgb(0xE3, 0xD8, 0xC6);
    public static readonly Color LineStrong  = Color.FromArgb(0xD4, 0xC6, 0xAF);
    public static readonly Color Primary     = Color.FromArgb(0x2F, 0x5D, 0x4F);
    public static readonly Color Primary700  = Color.FromArgb(0x26, 0x4C, 0x41);
    public static readonly Color Primary100  = Color.FromArgb(0xDF, 0xE9, 0xE3);
    public static readonly Color Accent      = Color.FromArgb(0xBD, 0x6C, 0x34);
    public static readonly Color Accent700   = Color.FromArgb(0x9C, 0x55, 0x26);
    public static readonly Color Accent100   = Color.FromArgb(0xF4, 0xE3, 0xD2);
    public static readonly Color Good        = Color.FromArgb(0x3D, 0x7A, 0x5F);
    public static readonly Color GoodBg      = Color.FromArgb(0xE2, 0xEE, 0xE7);
    public static readonly Color MutedBg     = Color.FromArgb(0xEC, 0xE3, 0xD4);
    public static readonly Color Danger      = Color.FromArgb(0xB1, 0x49, 0x2F);
    public static readonly Color DangerBg    = Color.FromArgb(0xF4, 0xE0, 0xD8);
    public static readonly Color Gold        = Color.FromArgb(0xC7, 0x9A, 0x3E);
    public static readonly Color GoldBg      = Color.FromArgb(0xF4, 0xEA, 0xD0);

    // ── Font factories (dispose after use or store in a field) ───────────────
    public static Font Sans(float pt, FontStyle fs = FontStyle.Regular)  => new("Segoe UI", pt, fs);
    public static Font Serif(float pt, FontStyle fs = FontStyle.Regular) => new("Georgia", pt, fs);
    public static Font Mono(float pt, FontStyle fs = FontStyle.Regular)  => new("Consolas", pt, fs);

    // ── Drawing helpers ──────────────────────────────────────────────────────
    public static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        int d = radius * 2;
        var p = new GraphicsPath();
        p.AddArc(r.X,          r.Y,           d, d, 180, 90);
        p.AddArc(r.Right - d,  r.Y,           d, d, 270, 90);
        p.AddArc(r.Right - d,  r.Bottom - d,  d, d,   0, 90);
        p.AddArc(r.X,          r.Bottom - d,  d, d,  90, 90);
        p.CloseFigure();
        return p;
    }

    public static void FillRounded(Graphics g, Rectangle r, Color fill, int radius = 8)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundedRect(r, radius);
        using var br   = new SolidBrush(fill);
        g.FillPath(br, path);
    }

    // ── DataGridView theming ─────────────────────────────────────────────────
    public static void ThemeDgv(DataGridView dgv)
    {
        dgv.BackgroundColor              = Surface;
        dgv.GridColor                    = Line;
        dgv.BorderStyle                  = BorderStyle.None;
        dgv.CellBorderStyle              = DataGridViewCellBorderStyle.SingleHorizontal;
        dgv.ColumnHeadersBorderStyle     = DataGridViewHeaderBorderStyle.Single;
        dgv.EnableHeadersVisualStyles    = false;
        dgv.SelectionMode                = DataGridViewSelectionMode.FullRowSelect;
        dgv.MultiSelect                  = false;
        dgv.RowHeadersVisible            = false;
        dgv.AllowUserToAddRows           = false;
        dgv.AllowUserToDeleteRows        = false;
        dgv.ReadOnly                     = true;
        dgv.AutoSizeRowsMode             = DataGridViewAutoSizeRowsMode.None;
        dgv.RowTemplate.Height           = 34;
        dgv.Font                         = Sans(9f);

        dgv.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor          = Surface,
            ForeColor          = Ink,
            SelectionBackColor = Primary100,
            SelectionForeColor = Ink,
            Padding            = new Padding(8, 0, 8, 0),
        };
        dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor          = Surface2,
            ForeColor          = InkFaint,
            Font               = Sans(7.5f, FontStyle.Bold),
            Padding            = new Padding(8, 0, 8, 0),
            SelectionBackColor = Surface2,
            SelectionForeColor = InkFaint,
        };
        dgv.ColumnHeadersHeight             = 32;
        dgv.ColumnHeadersHeightSizeMode     = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
    }

    // ── Button factory ───────────────────────────────────────────────────────
    public enum BtnKind { Default, Primary, Accent, Danger, Ghost }

    public static Button MakeBtn(string text, BtnKind kind = BtnKind.Default)
    {
        var b = new Button
        {
            Text      = text,
            AutoSize  = true,
            FlatStyle = FlatStyle.Flat,
            Cursor    = Cursors.Hand,
            Font      = Sans(9f),
            Padding   = new Padding(8, 4, 8, 4),
            Margin    = new Padding(2, 2, 2, 2),
            UseVisualStyleBackColor = false,
        };
        ApplyBtnStyle(b, kind);
        return b;
    }

    public static void ApplyBtnStyle(Button b, BtnKind kind)
    {
        b.FlatAppearance.BorderSize = 1;
        switch (kind)
        {
            case BtnKind.Primary:
                b.BackColor = Primary;
                b.ForeColor = Color.White;
                b.FlatAppearance.BorderColor        = Primary700;
                b.FlatAppearance.MouseOverBackColor = Primary700;
                b.FlatAppearance.MouseDownBackColor = Primary700;
                break;
            case BtnKind.Accent:
                b.BackColor = Accent;
                b.ForeColor = Color.White;
                b.FlatAppearance.BorderColor        = Accent700;
                b.FlatAppearance.MouseOverBackColor = Accent700;
                b.FlatAppearance.MouseDownBackColor = Accent700;
                break;
            case BtnKind.Danger:
                b.BackColor = DangerBg;
                b.ForeColor = Danger;
                b.FlatAppearance.BorderColor        = Danger;
                b.FlatAppearance.MouseOverBackColor = Color.FromArgb(0xEB, 0xCA, 0xC2);
                b.FlatAppearance.MouseDownBackColor = Color.FromArgb(0xEB, 0xCA, 0xC2);
                break;
            case BtnKind.Ghost:
                b.BackColor = Color.Transparent;
                b.ForeColor = InkSoft;
                b.FlatAppearance.BorderColor        = Line;
                b.FlatAppearance.MouseOverBackColor = Surface3;
                b.FlatAppearance.MouseDownBackColor = Surface3;
                break;
            default: // Default
                b.BackColor = Surface2;
                b.ForeColor = InkSoft;
                b.FlatAppearance.BorderColor        = LineStrong;
                b.FlatAppearance.MouseOverBackColor = Surface3;
                b.FlatAppearance.MouseDownBackColor = Surface3;
                break;
        }
    }

    // ── Toolbar panel builder ────────────────────────────────────────────────
    // Returns a panel styled as the "toolbar card" — surface-2 bg with bottom border.
    public static (Panel outer, FlowLayoutPanel flow) MakeToolbar(int height = 52)
    {
        var outer = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = height,
            BackColor = Surface2,
        };
        outer.Paint += (_, e) =>
        {
            using var pen = new Pen(Line);
            e.Graphics.DrawLine(pen, 0, outer.Height - 1, outer.Width, outer.Height - 1);
        };
        var flow = new FlowLayoutPanel
        {
            Dock            = DockStyle.Fill,
            FlowDirection   = FlowDirection.LeftToRight,
            WrapContents    = false,
            AutoSize        = false,
            Padding         = new Padding(8, 0, 8, 0),
        };
        outer.Controls.Add(flow);
        return (outer, flow);
    }
}
