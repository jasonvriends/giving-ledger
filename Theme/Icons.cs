using System.Drawing.Drawing2D;

namespace Envelope_Steward.Theme;

// Feather-style stroke icons drawn in GDI+, 24x24 coordinate space.
internal static class Icons
{
    public static void Draw(Graphics g, string name, RectangleF r, Color color, float stroke = 1.8f)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float s = r.Width / 24f;
        float ox = r.X, oy = r.Y;
        PointF P(float x, float y) => new(ox + x * s, oy + y * s);

        using var pen = new Pen(color, stroke)
        {
            StartCap  = LineCap.Round,
            EndCap    = LineCap.Round,
            LineJoin  = LineJoin.Round,
        };

        switch (name)
        {
            case "members":
                g.DrawEllipse(pen, ox + 5 * s, oy + 2 * s, 10 * s, 10 * s);
                g.DrawArc(pen, ox, oy + 12 * s, 22 * s, 14 * s, 180, 180);
                break;

            case "tags":
                g.DrawLines(pen, new[] { P(11,2), P(21,12), P(12,21), P(4,21), P(2,19), P(2,11), P(11,2) });
                g.DrawEllipse(pen, ox + 6 * s, oy + 7 * s, 3 * s, 3 * s);
                break;

            case "donate":
                g.DrawLine(pen, P(12, 2), P(12, 22));
                g.DrawLines(pen, new[] { P(17,5), P(9.5f,5), P(9.5f,12), P(14.5f,12), P(14.5f,19), P(7,19) });
                break;

            case "reports":
                g.DrawLine(pen, P(3, 3),  P(3, 21));
                g.DrawLine(pen, P(3, 21), P(21, 21));
                g.DrawLine(pen, P(7, 15), P(7, 21));
                g.DrawLine(pen, P(11, 8), P(11, 21));
                g.DrawLine(pen, P(15, 12), P(15, 21));
                g.DrawLine(pen, P(19, 17), P(19, 21));
                break;

            case "settings":
                g.DrawEllipse(pen, ox + 9 * s, oy + 9 * s, 6 * s, 6 * s);
                for (int i = 0; i < 8; i++)
                {
                    double a  = i * Math.PI / 4;
                    float  ix = ox + 12 * s + (float)(Math.Cos(a) * 6.8f * s);
                    float  iy = oy + 12 * s + (float)(Math.Sin(a) * 6.8f * s);
                    float  ex = ox + 12 * s + (float)(Math.Cos(a) * 10f  * s);
                    float  ey = oy + 12 * s + (float)(Math.Sin(a) * 10f  * s);
                    g.DrawLine(pen, ix, iy, ex, ey);
                }
                break;

            case "search":
                g.DrawEllipse(pen, ox + 3 * s, oy + 3 * s, 13 * s, 13 * s);
                g.DrawLine(pen, P(14, 14), P(21, 21));
                break;

            case "plus":
                g.DrawLine(pen, P(12, 5), P(12, 19));
                g.DrawLine(pen, P(5, 12), P(19, 12));
                break;

            case "edit":
                g.DrawLines(pen, new[] { P(11,4), P(4,4), P(4,20), P(20,20), P(20,13) });
                g.DrawLines(pen, new[] { P(18.5f,2.5f), P(21.5f,5.5f), P(12,15), P(8,16), P(9,12), P(18.5f,2.5f) });
                break;

            case "trash":
                g.DrawLine(pen,  P(3, 6),  P(21, 6));
                g.DrawLines(pen, new[] { P(19,6), P(17,22), P(7,22), P(5,6) });
                g.DrawLines(pen, new[] { P(9,4), P(9,2), P(15,2), P(15,4) });
                break;

            case "printer":
                g.DrawLines(pen, new[] { P(6,9),  P(6,2),  P(18,2),  P(18,9) });
                g.DrawLines(pen, new[] { P(4,18), P(4,9),  P(20,9),  P(20,18) });
                g.DrawLines(pen, new[] { P(6,14), P(6,22), P(18,22), P(18,14), P(6,14) });
                break;

            case "church":
                g.DrawLine(pen,  P(12, 2),  P(12, 10));
                g.DrawLine(pen,  P(9, 4),   P(15, 4));
                g.DrawLines(pen, new[] { P(4,13), P(4,22), P(20,22), P(20,13), P(12,8), P(4,13) });
                g.DrawLines(pen, new[] { P(9,22), P(9,18), P(15,18), P(15,22) });
                break;

            case "check":
                g.DrawLines(pen, new[] { P(4, 12), P(9, 17), P(20, 6) });
                break;

            case "x":
                g.DrawLine(pen, P(6, 6),  P(18, 18));
                g.DrawLine(pen, P(18, 6), P(6, 18));
                break;

            case "folder":
                g.DrawLines(pen, new[] { P(22,19), P(4,19), P(2,5), P(7,5), P(9,8), P(22,8), P(22,19) });
                break;

            case "cloud":
                g.DrawArc(pen, ox + 2 * s, oy + 10 * s, 12 * s, 9 * s, 130, 260);
                g.DrawArc(pen, ox + 5 * s, oy +  6 * s, 11 * s, 9 * s, 200, 130);
                g.DrawArc(pen, ox + 12 * s, oy + 7 * s, 8  * s, 8 * s, 270, 180);
                break;

            case "upload":
                g.DrawLines(pen, new[] { P(7,8), P(12,3), P(17,8) });
                g.DrawLine(pen,   P(12, 3), P(12, 15));
                g.DrawLines(pen, new[] { P(3,17), P(3,20), P(21,20), P(21,17) });
                break;

            case "download":
                g.DrawLines(pen, new[] { P(7,10), P(12,15), P(17,10) });
                g.DrawLine(pen,   P(12, 3), P(12, 15));
                g.DrawLines(pen, new[] { P(3,17), P(3,20), P(21,20), P(21,17) });
                break;

            case "filter":
                g.DrawLines(pen, new[] { P(2,3), P(22,3), P(14,12.5f), P(14,19), P(10,21), P(10,12.5f), P(2,3) });
                break;

            case "calendar":
                g.DrawRectangle(pen, ox + 3 * s, oy + 4 * s, 18 * s, 16 * s);
                g.DrawLine(pen, P(16, 2), P(16, 6));
                g.DrawLine(pen, P(8, 2),  P(8, 6));
                g.DrawLine(pen, P(3, 10), P(21, 10));
                break;

            case "receipt":
                // Torn-edge receipt
                g.DrawLines(pen, new[] {
                    P(4,2),  P(4,22),  P(6,20.5f), P(8,22),  P(10,20.5f), P(12,22),
                    P(14,20.5f), P(16,22), P(18,20.5f), P(20,22), P(20,2),
                    P(18,3.5f), P(16,2), P(14,3.5f), P(12,2),  P(10,3.5f),
                    P(8,2),  P(6,3.5f), P(4,2),
                });
                g.DrawLine(pen, P(8, 8),  P(16, 8));
                g.DrawLine(pen, P(8, 12), P(16, 12));
                g.DrawLine(pen, P(8, 16), P(13, 16));
                break;

            case "info":
                g.DrawEllipse(pen, ox + 2 * s, oy + 2 * s, 20 * s, 20 * s);
                g.DrawLine(pen, P(12, 16), P(12, 11));
                g.DrawLine(pen, P(12, 8),  P(12.01f, 8)); // dot
                break;
        }
    }
}
