using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace Polka
{
    static class C
    {
        public static readonly Color Bg = Color.FromArgb(10, 12, 17);
        public static readonly Color Panel = Color.FromArgb(18, 21, 29);
        public static readonly Color PanelHover = Color.FromArgb(22, 26, 36);
        public static readonly Color Panel2 = Color.FromArgb(24, 28, 39);
        public static readonly Color Field = Color.FromArgb(14, 17, 24);
        public static readonly Color Quick = Color.FromArgb(20, 24, 34);
        public static readonly Color EditorBg = Color.FromArgb(17, 20, 28);
        public static readonly Color Line = Color.FromArgb(35, 41, 54);
        public static readonly Color Line2 = Color.FromArgb(50, 58, 76);
        public static readonly Color Text = Color.FromArgb(232, 236, 244);
        public static readonly Color Muted = Color.FromArgb(138, 147, 166);
        public static readonly Color Muted2 = Color.FromArgb(99, 108, 127);
        public static readonly Color Accent = Color.FromArgb(139, 150, 255);
        public static readonly Color Accent2 = Color.FromArgb(192, 132, 252);
        public static readonly Color Gold = Color.FromArgb(245, 194, 91);
        public static readonly Color Green = Color.FromArgb(74, 222, 128);
        public static readonly Color Danger = Color.FromArgb(248, 113, 113);
        public static readonly Color TagBg = Color.FromArgb(31, 36, 48);
    }

    static class F
    {
        public static readonly Font H1 = new Font("Segoe UI Semibold", 26f);
        public static readonly Font H2 = new Font("Segoe UI Semibold", 16f);
        public static readonly Font CardTitle = new Font("Segoe UI Semibold", 11.2f);
        public static readonly Font Body = new Font("Segoe UI", 9.4f);
        public static readonly Font Input = new Font("Segoe UI", 10f);
        public static readonly Font Small = new Font("Segoe UI", 8.6f);
        public static readonly Font Tiny = new Font("Segoe UI Semibold", 8f);
        public static readonly Font Label = new Font("Segoe UI Semibold", 7.8f);
        public static readonly Font Big = new Font("Segoe UI", 46f, FontStyle.Bold);
        public static readonly Font Pill = new Font("Segoe UI Semibold", 9f);
        public static readonly Font Btn = new Font("Segoe UI Semibold", 9.2f);
        public static readonly Font H0 = new Font("Segoe UI Semibold", 13f);
    }

    static class D
    {
        public static GraphicsPath Round(Rectangle r, int radius)
        {
            var p = new GraphicsPath();
            if (r.Width <= 0 || r.Height <= 0) { p.AddRectangle(r); return p; }
            int d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            if (d <= 2) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void FillRound(Graphics g, Rectangle r, int radius, Color color)
        {
            using (var path = Round(r, radius))
            using (var b = new SolidBrush(color))
                g.FillPath(b, path);
        }

        public static void StrokeRound(Graphics g, Rectangle r, int radius, Color color, float width)
        {
            using (var path = Round(r, radius))
            using (var pen = new Pen(color, width))
                g.DrawPath(pen, path);
        }

        public static void Text(Graphics g, string text, Font font, Color color, Rectangle rect, TextFormatFlags flags)
        {
            TextRenderer.DrawText(g, text, font, rect, color, flags | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        }

        public static Color Hsl(double h, double s, double l)
        {
            h = ((h % 360) + 360) % 360 / 360.0;
            double r, gg, b;
            if (s <= 0) { r = gg = b = l; }
            else
            {
                double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
                double p = 2 * l - q;
                r = Hue(p, q, h + 1.0 / 3);
                gg = Hue(p, q, h);
                b = Hue(p, q, h - 1.0 / 3);
            }
            return Color.FromArgb(255, Byte(r), Byte(gg), Byte(b));
        }

        static double Hue(double p, double q, double t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1.0 / 6) return p + (q - p) * 6 * t;
            if (t < 0.5) return q;
            if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
            return p;
        }

        static int Byte(double v)
        {
            int i = (int)Math.Round(v * 255);
            return Math.Max(0, Math.Min(255, i));
        }

        public static Color Blend(Color a, Color b, double t)
        {
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * t),
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        public static void Glow(Graphics g, PointF center, float radius, Color color)
        {
            var old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            for (int i = 0; i < 16; i++)
            {
                float t = i / 16f;
                float rr = radius * (1 - t * 0.92f);
                int a = (int)(color.A * (1 - t) * (1 - t) * 0.45f);
                if (a <= 0 || rr <= 1) continue;
                using (var b = new SolidBrush(Color.FromArgb(a, color)))
                    g.FillEllipse(b, center.X - rr, center.Y - rr, rr * 2, rr * 2);
            }
            g.SmoothingMode = old;
        }

        public static void Star(Graphics g, RectangleF r, Color color, bool filled, float stroke)
        {
            var old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            PointF[] pts = new PointF[10];
            float cx = r.X + r.Width / 2f;
            float cy = r.Y + r.Height / 2f;
            float outer = Math.Min(r.Width, r.Height) / 2f;
            float inner = outer * 0.44f;
            for (int i = 0; i < 10; i++)
            {
                double ang = -Math.PI / 2 + i * Math.PI / 5;
                float rad = (i % 2 == 0) ? outer : inner;
                pts[i] = new PointF(cx + (float)Math.Cos(ang) * rad, cy + (float)Math.Sin(ang) * rad);
            }
            if (filled)
            {
                using (var b = new SolidBrush(color)) g.FillPolygon(b, pts);
            }
            else
            {
                using (var pen = new Pen(color, stroke)) g.DrawPolygon(pen, pts);
            }
            g.SmoothingMode = old;
        }

        static PointF P(float x, float y, float ox, float oy, float s)
        {
            return new PointF(ox + x * s, oy + y * s);
        }

        public static void Icon(Graphics g, string name, RectangleF r, Color color, float w)
        {
            var old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float s = Math.Min(r.Width, r.Height);
            float ox = r.X + (r.Width - s) / 2f;
            float oy = r.Y + (r.Height - s) / 2f;

            using (var pen = new Pen(color, w))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                switch (name)
                {
                    case "film":
                        g.DrawPath(pen, Round(new Rectangle((int)(ox + s * .08f), (int)(oy + s * .18f), (int)(s * .84f), (int)(s * .64f)), (int)(s * .12f)));
                        g.DrawLine(pen, ox + s * .35f, oy + s * .18f, ox + s * .35f, oy + s * .82f);
                        g.DrawLine(pen, ox + s * .65f, oy + s * .18f, ox + s * .65f, oy + s * .82f);
                        break;

                    case "tv":
                        g.DrawPath(pen, Round(new Rectangle((int)(ox + s * .08f), (int)(oy + s * .30f), (int)(s * .84f), (int)(s * .62f)), (int)(s * .1f)));
                        g.DrawLine(pen, ox + s * .33f, oy + s * .1f, ox + s * .5f, oy + s * .3f);
                        g.DrawLine(pen, ox + s * .67f, oy + s * .1f, ox + s * .5f, oy + s * .3f);
                        break;

                    case "book":
                        using (var path = new GraphicsPath())
                        {
                            path.AddBezier(P(.5f, .22f, ox, oy, s), P(.38f, .13f, ox, oy, s), P(.22f, .13f, ox, oy, s), P(.14f, .18f, ox, oy, s));
                            path.AddLine(P(.14f, .18f, ox, oy, s), P(.14f, .82f, ox, oy, s));
                            path.AddBezier(P(.14f, .82f, ox, oy, s), P(.24f, .77f, ox, oy, s), P(.38f, .77f, ox, oy, s), P(.5f, .86f, ox, oy, s));
                            path.AddBezier(P(.5f, .86f, ox, oy, s), P(.62f, .77f, ox, oy, s), P(.76f, .77f, ox, oy, s), P(.86f, .82f, ox, oy, s));
                            path.AddLine(P(.86f, .82f, ox, oy, s), P(.86f, .18f, ox, oy, s));
                            path.AddBezier(P(.86f, .18f, ox, oy, s), P(.78f, .13f, ox, oy, s), P(.62f, .13f, ox, oy, s), P(.5f, .22f, ox, oy, s));
                            g.DrawPath(pen, path);
                        }
                        g.DrawLine(pen, ox + s * .5f, oy + s * .24f, ox + s * .5f, oy + s * .84f);
                        break;

                    case "game":
                        g.DrawPath(pen, Round(new Rectangle((int)(ox + s * .06f), (int)(oy + s * .30f), (int)(s * .88f), (int)(s * .42f)), (int)(s * .21f)));
                        g.DrawLine(pen, ox + s * .28f, oy + s * .42f, ox + s * .28f, oy + s * .60f);
                        g.DrawLine(pen, ox + s * .19f, oy + s * .51f, ox + s * .37f, oy + s * .51f);
                        using (var b = new SolidBrush(color))
                        {
                            g.FillEllipse(b, ox + s * .66f, oy + s * .44f, s * .07f, s * .07f);
                            g.FillEllipse(b, ox + s * .77f, oy + s * .55f, s * .07f, s * .07f);
                        }
                        break;

                    case "bookmark":
                        using (var path = new GraphicsPath())
                        {
                            path.AddLine(P(.24f, .14f, ox, oy, s), P(.76f, .14f, ox, oy, s));
                            path.AddLine(P(.76f, .14f, ox, oy, s), P(.76f, .88f, ox, oy, s));
                            path.AddLine(P(.76f, .88f, ox, oy, s), P(.5f, .7f, ox, oy, s));
                            path.AddLine(P(.5f, .7f, ox, oy, s), P(.24f, .88f, ox, oy, s));
                            path.CloseFigure();
                            g.DrawPath(pen, path);
                        }
                        break;

                    case "search":
                        g.DrawEllipse(pen, ox + s * .14f, oy + s * .14f, s * .5f, s * .5f);
                        g.DrawLine(pen, ox + s * .64f, oy + s * .64f, ox + s * .86f, oy + s * .86f);
                        break;

                    case "plus":
                        g.DrawLine(pen, ox + s * .5f, oy + s * .18f, ox + s * .5f, oy + s * .82f);
                        g.DrawLine(pen, ox + s * .18f, oy + s * .5f, ox + s * .82f, oy + s * .5f);
                        break;

                    case "download":
                        g.DrawLine(pen, ox + s * .5f, oy + s * .14f, ox + s * .5f, oy + s * .64f);
                        g.DrawLine(pen, ox + s * .28f, oy + s * .44f, ox + s * .5f, oy + s * .66f);
                        g.DrawLine(pen, ox + s * .72f, oy + s * .44f, ox + s * .5f, oy + s * .66f);
                        g.DrawLine(pen, ox + s * .16f, oy + s * .74f, ox + s * .16f, oy + s * .86f);
                        g.DrawLine(pen, ox + s * .16f, oy + s * .86f, ox + s * .84f, oy + s * .86f);
                        g.DrawLine(pen, ox + s * .84f, oy + s * .86f, ox + s * .84f, oy + s * .74f);
                        break;

                    case "upload":
                        g.DrawLine(pen, ox + s * .5f, oy + s * .66f, ox + s * .5f, oy + s * .14f);
                        g.DrawLine(pen, ox + s * .28f, oy + s * .36f, ox + s * .5f, oy + s * .14f);
                        g.DrawLine(pen, ox + s * .72f, oy + s * .36f, ox + s * .5f, oy + s * .14f);
                        g.DrawLine(pen, ox + s * .16f, oy + s * .74f, ox + s * .16f, oy + s * .86f);
                        g.DrawLine(pen, ox + s * .16f, oy + s * .86f, ox + s * .84f, oy + s * .86f);
                        g.DrawLine(pen, ox + s * .84f, oy + s * .86f, ox + s * .84f, oy + s * .74f);
                        break;

                    case "check":
                        g.DrawLine(pen, ox + s * .16f, oy + s * .52f, ox + s * .4f, oy + s * .76f);
                        g.DrawLine(pen, ox + s * .4f, oy + s * .76f, ox + s * .84f, oy + s * .26f);
                        break;

                    case "pencil":
                        using (var path = new GraphicsPath())
                        {
                            path.AddLine(P(.16f, .84f, ox, oy, s), P(.2f, .63f, ox, oy, s));
                            path.AddLine(P(.2f, .63f, ox, oy, s), P(.66f, .17f, ox, oy, s));
                            path.AddLine(P(.66f, .17f, ox, oy, s), P(.83f, .34f, ox, oy, s));
                            path.AddLine(P(.83f, .34f, ox, oy, s), P(.37f, .8f, ox, oy, s));
                            path.CloseFigure();
                            g.DrawPath(pen, path);
                        }
                        g.DrawLine(pen, ox + s * .58f, oy + s * .25f, ox + s * .75f, oy + s * .42f);
                        break;

                    case "trash":
                        g.DrawLine(pen, ox + s * .12f, oy + s * .24f, ox + s * .88f, oy + s * .24f);
                        g.DrawLine(pen, ox + s * .38f, oy + s * .24f, ox + s * .38f, oy + s * .13f);
                        g.DrawLine(pen, ox + s * .38f, oy + s * .13f, ox + s * .62f, oy + s * .13f);
                        g.DrawLine(pen, ox + s * .62f, oy + s * .13f, ox + s * .62f, oy + s * .24f);
                        using (var path = new GraphicsPath())
                        {
                            path.AddLine(P(.21f, .24f, ox, oy, s), P(.27f, .88f, ox, oy, s));
                            path.AddLine(P(.27f, .88f, ox, oy, s), P(.73f, .88f, ox, oy, s));
                            path.AddLine(P(.73f, .88f, ox, oy, s), P(.79f, .24f, ox, oy, s));
                            g.DrawPath(pen, path);
                        }
                        break;

                    case "dice":
                        g.DrawPath(pen, Round(new Rectangle((int)(ox + s * .1f), (int)(oy + s * .1f), (int)(s * .8f), (int)(s * .8f)), (int)(s * .16f)));
                        using (var b = new SolidBrush(color))
                        {
                            g.FillEllipse(b, ox + s * .28f, oy + s * .28f, s * .1f, s * .1f);
                            g.FillEllipse(b, ox + s * .62f, oy + s * .28f, s * .1f, s * .1f);
                            g.FillEllipse(b, ox + s * .45f, oy + s * .45f, s * .1f, s * .1f);
                            g.FillEllipse(b, ox + s * .28f, oy + s * .62f, s * .1f, s * .1f);
                            g.FillEllipse(b, ox + s * .62f, oy + s * .62f, s * .1f, s * .1f);
                        }
                        break;

                    case "close":
                        g.DrawLine(pen, ox + s * .26f, oy + s * .26f, ox + s * .74f, oy + s * .74f);
                        g.DrawLine(pen, ox + s * .74f, oy + s * .26f, ox + s * .26f, oy + s * .74f);
                        break;

                    case "chevron":
                        g.DrawLine(pen, ox + s * .22f, oy + s * .38f, ox + s * .5f, oy + s * .66f);
                        g.DrawLine(pen, ox + s * .78f, oy + s * .38f, ox + s * .5f, oy + s * .66f);
                        break;
                }
            }
            g.SmoothingMode = old;
        }
    }
}
