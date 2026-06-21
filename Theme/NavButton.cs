using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace Envelope_Steward.Theme;

// Owner-drawn sidebar navigation button with active/hover states and optional count chip.
internal sealed class NavButton : Control
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string NavId    { get; init; } = "";
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string IconName { get; init; } = "";
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int?   Count    { get; set; }

    private bool _active, _hovered;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Active
    {
        get => _active;
        set { _active = value; Invalidate(); }
    }

    public event EventHandler? Activated;

    public NavButton()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint            |
            ControlStyles.DoubleBuffer         |
            ControlStyles.ResizeRedraw,
            true);
        Cursor  = Cursors.Hand;
        Height  = 40;
        TabStop = true;
    }

    protected override void OnMouseEnter(EventArgs e) { _hovered = true;  Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnClick(EventArgs e)      { Activated?.Invoke(this, e); base.OnClick(e); }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Enter or Keys.Space)
            Activated?.Invoke(this, e);
        base.OnKeyDown(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode       = SmoothingMode.AntiAlias;
        g.TextRenderingHint   = TextRenderingHint.ClearTypeGridFit;

        // Inner hit rectangle (4px margin each side, 2px top/bottom)
        var bg = new Rectangle(4, 2, Width - 8, Height - 4);

        if (_active)
            GL.FillRounded(g, bg, GL.Primary, 9);
        else if (_hovered)
            GL.FillRounded(g, bg, GL.Surface3, 9);

        Color fg = _active ? Color.White : GL.InkSoft;

        // Icon — 18 × 18 px, left-padded at x=11 from bg edge
        var iconRect = new RectangleF(bg.X + 11, bg.Y + (bg.Height - 18) / 2f, 18, 18);
        if (!string.IsNullOrEmpty(IconName))
            Icons.Draw(g, IconName, iconRect, fg, 1.7f);

        // Label
        int chipW    = Count.HasValue ? 30 : 4;
        var textRect = new Rectangle(
            (int)(iconRect.Right + 10),
            bg.Y,
            bg.Width - (int)(iconRect.Right - bg.X) - 10 - chipW,
            bg.Height);
        using var font = GL.Sans(9.5f);
        TextRenderer.DrawText(g, Text, font, textRect, fg,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.SingleLine);

        // Count chip — mono, right-aligned
        if (Count.HasValue)
        {
            using var mf  = GL.Mono(7.5f);
            string    ct  = Count.Value.ToString();
            var       csz = TextRenderer.MeasureText(g, ct, mf);
            Color     cfg = _active ? Color.FromArgb(180, 255, 255, 255) : GL.InkFaint;
            var       cr  = new Rectangle(
                bg.Right - csz.Width - 10,
                bg.Y + (bg.Height - csz.Height) / 2,
                csz.Width + 2,
                csz.Height);
            TextRenderer.DrawText(g, ct, mf, cr, cfg,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }
    }
}
