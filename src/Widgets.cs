using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Polka
{
    public class InputField : Control
    {
        public TextBox Box;
        public Label Hint;
        public bool ShowSearch;
        public bool Error;
        public Color Surface = C.Bg;
        int radius = 11;
        bool focused;

        public InputField(bool multiline, string placeholder)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = C.Bg;
            Box = new TextBox();
            Box.BorderStyle = BorderStyle.None;
            Box.BackColor = C.Field;
            Box.ForeColor = C.Text;
            Box.Font = F.Input;
            Box.Multiline = multiline;
            Box.AcceptsReturn = multiline;
            Box.ScrollBars = ScrollBars.None;
            Controls.Add(Box);

            Hint = new Label();
            Hint.AutoSize = false;
            Hint.AutoEllipsis = true;
            Hint.BackColor = C.Field;
            Hint.ForeColor = C.Muted2;
            Hint.Font = F.Input;
            Hint.Text = placeholder;
            Hint.TextAlign = multiline ? ContentAlignment.TopLeft : ContentAlignment.MiddleLeft;
            Hint.Cursor = Cursors.IBeam;
            Hint.Click += delegate { Box.Focus(); };
            Controls.Add(Hint);

            Box.TextChanged += delegate { SyncHint(); };
            Box.Enter += delegate { focused = true; SyncHint(); Invalidate(); };
            Box.Leave += delegate { focused = false; SyncHint(); Invalidate(); };
        }

        public string Value
        {
            get { return Box.Text; }
            set { Box.Text = value ?? ""; }
        }

        public void SyncHint()
        {
            if (Hint == null) return;
            Hint.Visible = Box.TextLength == 0 && !Box.Focused;
            if (Hint.Visible) Hint.BringToFront();
        }

        void LayoutInner()
        {
            int pad = ShowSearch ? 38 : 13;
            if (Box.Multiline)
            {
                Box.SetBounds(11, 9, Math.Max(10, Width - 24), Math.Max(10, Height - 18));
                Hint.SetBounds(13, 11, Math.Max(10, Width - 26), 20);
            }
            else
            {
                int h = Box.PreferredHeight;
                Box.SetBounds(pad, (Height - h) / 2 + 1, Math.Max(10, Width - pad - 12), h);
                Hint.SetBounds(pad + 1, 0, Math.Max(10, Width - pad - 14), Height);
            }
            SyncHint();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutInner();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Surface);
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var b = new SolidBrush(C.Field)) using (var p = D.Round(r, radius)) g.FillPath(b, p);
            Color bc = Error ? C.Danger : (focused ? Color.FromArgb(160, C.Accent) : C.Line);
            using (var pen = new Pen(bc, (Error || focused) ? 1.6f : 1f))
            using (var p = D.Round(r, radius)) g.DrawPath(pen, p);
            if (ShowSearch)
                D.Icon(g, "search", new RectangleF(13, (Height - 15) / 2f, 15, 15), C.Muted2, 1.6f);
        }
    }

    public class FlatButton : Control
    {
        public string IconName;
        public bool Primary;
        public bool Danger;
        public bool Ghost;
        public Color Surface = C.Bg;
        bool hover, down;

        public FlatButton(string text)
        {
            Text = text;
            Font = F.Btn;
            Size = new Size(110, 40);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            BackColor = C.Bg;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Surface);
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            Color fg;

            if (Primary)
            {
                var c1 = down ? Color.FromArgb(168, 177, 255) : Color.FromArgb(139, 150, 255);
                var c2 = down ? Color.FromArgb(204, 158, 255) : Color.FromArgb(192, 132, 252);
                using (var br = new LinearGradientBrush(new Rectangle(0, 0, Math.Max(2, Width), Math.Max(2, Height)), c1, c2, 0f))
                using (var p = D.Round(r, 12)) g.FillPath(br, p);
                if (hover)
                {
                    using (var b = new SolidBrush(Color.FromArgb(22, 255, 255, 255)))
                    using (var p = D.Round(r, 12)) g.FillPath(b, p);
                }
                fg = Color.FromArgb(11, 13, 18);
            }
            else
            {
                Color fill = Ghost ? Surface : C.Panel;
                using (var b = new SolidBrush(fill)) using (var p = D.Round(r, 12)) g.FillPath(b, p);
                if (hover && Ghost)
                {
                    using (var b = new SolidBrush(Color.FromArgb(16, 255, 255, 255)))
                    using (var p = D.Round(r, 12)) g.FillPath(b, p);
                }
                Color border = Danger ? Color.FromArgb(90, C.Danger) : (hover ? C.Line2 : C.Line);
                using (var pen = new Pen(border)) using (var p = D.Round(r, 12)) g.DrawPath(pen, p);
                fg = Danger ? C.Danger : (hover ? C.Text : Color.FromArgb(185, 191, 205));
            }

            int ico = 16;
            int gap = 8;
            Size ts = string.IsNullOrEmpty(Text) ? Size.Empty :
                TextRenderer.MeasureText(Text, Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
            int total = (string.IsNullOrEmpty(IconName) ? 0 : ico) + (string.IsNullOrEmpty(Text) ? 0 : ts.Width) +
                        ((string.IsNullOrEmpty(IconName) || string.IsNullOrEmpty(Text)) ? 0 : gap);
            int x = (Width - total) / 2;
            if (!string.IsNullOrEmpty(IconName))
            {
                D.Icon(g, IconName, new RectangleF(x, (Height - ico) / 2f - 0.5f, ico, ico), fg, 1.7f);
                x += ico + gap;
            }
            if (!string.IsNullOrEmpty(Text))
            {
                var tr = new Rectangle(x, 0, ts.Width + 2, Height);
                TextRenderer.DrawText(g, Text, Font, tr, fg,
                    TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
            }
        }
    }

    public class Pill : Control
    {
        public string Key;
        public string IconName;
        public int Count = -1;
        public bool Selected;
        public Color Surface = C.Bg;
        public string Mode = "tab";
        public Color IconColor = C.Accent;
        bool hover;

        public Pill(string text)
        {
            Text = text;
            Font = F.Pill;
            Height = 36;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            BackColor = C.Bg;
        }

        public int PreferredWidth()
        {
            int pad = Mode == "tab" ? 14 : (Mode == "seg" ? 12 : 13);
            Size ts = TextRenderer.MeasureText(Text, Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
            int w = pad * 2 + ts.Width;
            if (!string.IsNullOrEmpty(IconName)) w += 22;
            if (Count >= 0)
            {
                Size cs = TextRenderer.MeasureText(Count.ToString(), F.Tiny, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
                w += 6 + cs.Width + 12;
            }
            return w;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Surface);
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            bool isTab = Mode == "tab";
            bool isChip = Mode == "chip";
            int radius = isChip ? 9 : (isTab ? 18 : 9);
            Color textColor = C.Muted;

            if (Selected)
            {
                if (isChip)
                {
                    D.FillRound(g, r, radius, Color.FromArgb(32, C.Accent));
                    D.StrokeRound(g, r, radius, Color.FromArgb(80, C.Accent), 1f);
                    textColor = C.Text;
                }
                else
                {
                    using (var br = new LinearGradientBrush(new Rectangle(0, 0, Math.Max(2, Width), Math.Max(2, Height)),
                        Color.FromArgb(163, 173, 255), Color.FromArgb(200, 166, 255), 0f))
                    using (var p = D.Round(r, radius)) g.FillPath(br, p);
                    textColor = Color.FromArgb(11, 13, 18);
                }
            }
            else
            {
                if (hover) D.FillRound(g, r, radius, Color.FromArgb(16, 255, 255, 255));
                if (!isChip) D.StrokeRound(g, r, radius, C.Line, 1f);
            }

            int pad = isTab ? 14 : (Mode == "seg" ? 12 : 13);
            int x = pad;
            int textW = TextRenderer.MeasureText(Text, Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;

            int cy = (Height - 15) / 2;
            if (!string.IsNullOrEmpty(IconName))
            {
                Color ic = Selected ? textColor : IconColor;
                D.Icon(g, IconName, new RectangleF(x, cy, 15, 15), ic, 1.6f);
                x += 22;
            }

            var tr = new Rectangle(x, 0, textW + 2, Height);
            TextRenderer.DrawText(g, Text, Font, tr, textColor,
                TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);

            if (Count >= 0)
            {
                string cs = Count.ToString();
                Size csz = TextRenderer.MeasureText(cs, F.Tiny, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
                int bw = csz.Width + 12;
                var br2 = new Rectangle(Width - pad - bw, (Height - 18) / 2, bw, 18);
                Color bg = Selected && !isChip ? Color.FromArgb(38, 0, 0, 0) : Color.FromArgb(20, 255, 255, 255);
                if (Selected && isChip) bg = Color.FromArgb(26, C.Accent);
                D.FillRound(g, br2, 9, bg);
                Color cf = Selected && !isChip ? Color.FromArgb(150, 11, 13, 18) : C.Muted;
                TextRenderer.DrawText(g, cs, F.Tiny, br2, cf,
                    TextFormatFlags.NoPadding | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
    }

    public class StarRating : Control
    {
        public int Value;
        public Color Surface = C.Bg;
        int hoverIndex = -1;

        public StarRating()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Size = new Size(160, 32);
            BackColor = C.Bg;
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Surface);
            int shown = hoverIndex >= 0 ? hoverIndex + 1 : Value;
            for (int i = 0; i < 5; i++)
            {
                bool on = i < shown;
                var r = new RectangleF(i * 30 + 2, (Height - 22) / 2f, 22, 22);
                if (on) D.Star(g, r, C.Gold, true, 1f);
                else D.Star(g, r, Color.FromArgb(74, 83, 102), false, 1.3f);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int idx = Math.Max(0, Math.Min(4, e.X / 30));
            if (idx != hoverIndex) { hoverIndex = idx; Invalidate(); }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hoverIndex = -1;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            int idx = Math.Max(0, Math.Min(4, e.X / 30)) + 1;
            Value = (Value == idx) ? 0 : idx;
            Invalidate();
            base.OnMouseClick(e);
        }
    }

    public class DarkColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return C.Panel2; } }
        public override Color MenuBorder { get { return C.Line2; } }
        public override Color MenuItemBorder { get { return C.Line2; } }
        public override Color MenuItemSelected { get { return Color.FromArgb(45, 52, 70); } }
        public override Color MenuItemSelectedGradientBegin { get { return Color.FromArgb(45, 52, 70); } }
        public override Color MenuItemSelectedGradientEnd { get { return Color.FromArgb(45, 52, 70); } }
        public override Color ImageMarginGradientBegin { get { return C.Panel2; } }
        public override Color ImageMarginGradientMiddle { get { return C.Panel2; } }
        public override Color ImageMarginGradientEnd { get { return C.Panel2; } }
        public override Color SeparatorDark { get { return C.Line; } }
        public override Color SeparatorLight { get { return C.Line; } }
        public override Color MenuStripGradientBegin { get { return C.Panel2; } }
        public override Color MenuStripGradientEnd { get { return C.Panel2; } }
    }

    public class Toast : Form
    {
        Timer timer;

        public Toast(string message, Form owner)
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = C.Panel2;
            Font = F.Body;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);

            Size ts = TextRenderer.MeasureText(message, F.Body, new Size(700, 100), TextFormatFlags.NoPadding);
            ClientSize = new Size(Math.Min(560, ts.Width + 36), 42);
            Region = new Region(D.Round(new Rectangle(0, 0, ClientSize.Width, ClientSize.Height), 12));
            Text = message;

            if (owner != null)
            {
                Location = new Point(owner.Left + (owner.Width - Width) / 2, owner.Top + owner.Height - Height - 44);
            }
            timer = new Timer();
            timer.Interval = 2400;
            timer.Tick += delegate { timer.Stop(); Close(); };
            timer.Start();
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(C.Panel2);
            using (var p = D.Round(new Rectangle(0, 0, ClientSize.Width - 1, ClientSize.Height - 1), 12))
            using (var pen = new Pen(C.Line2)) g.DrawPath(pen, p);
            D.Text(g, Text, F.Body, C.Text,
                new Rectangle(16, 0, ClientSize.Width - 32, ClientSize.Height),
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }
    }
}
