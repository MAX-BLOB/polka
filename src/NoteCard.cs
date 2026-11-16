using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Polka
{
    public class NoteCard : Control
    {
        public Note Note;
        public event EventHandler StatusClick;
        public event EventHandler EditClick;
        public event EventHandler DeleteClick;
        public event EventHandler OpenClick;

        bool flash;
        bool hover;
        int hoverAction = -1;
        Rectangle rCheck, rEdit, rTrash;

        public NoteCard(Note note)
        {
            Note = note;
            Size = new Size(280, 336);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            BackColor = C.Bg;
        }

        public bool Flash
        {
            get { return flash; }
            set { flash = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(C.Bg);

            var card = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = D.Round(card, 18))
            {
                using (var b = new SolidBrush(hover ? C.PanelHover : C.Panel)) g.FillPath(b, path);
                var state = g.Save();
                g.SetClip(path);
                PaintCover(g);
                g.Restore(state);
                Color bc = flash ? C.Accent : (hover ? Color.FromArgb(135, C.Accent) : C.Line);
                using (var pen = new Pen(bc, flash ? 2.2f : 1f)) g.DrawPath(pen, path);
            }
            PaintBody(g);
        }

        void PaintCover(Graphics g)
        {
            int coverH = Height < 320 ? 132 : 150;
            var colors = Meta.Cover(Note.Title);
            using (var br = new LinearGradientBrush(new Point(0, 0), new Point(Width, coverH), colors[0], colors[1]))
                g.FillRectangle(br, 0, 0, Width, coverH);
            using (var br = new LinearGradientBrush(new Rectangle(0, 0, Math.Max(1, Width), coverH),
                Color.FromArgb(26, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 40f))
                g.FillRectangle(br, 0, 0, Width, coverH);

            string letter = string.IsNullOrEmpty(Note.Title) ? "?" : Note.Title.Substring(0, 1).ToUpperInvariant();
            using (var b = new SolidBrush(Color.FromArgb(42, 255, 255, 255)))
            using (var sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                g.DrawString(letter, F.Big, b, new RectangleF(0, -8, Width, coverH + 16), sf);
            }

            using (var br = new LinearGradientBrush(new Point(0, coverH - 76), new Point(0, coverH),
                Color.FromArgb(0, 6, 8, 12), Color.FromArgb(175, 6, 8, 12)))
                g.FillRectangle(br, 0, coverH - 76, Width, 76);

            DrawBadge(g, Meta.StatusLabel(Note.Status), 10, 10, Meta.StatusFg(Note.Status),
                Color.FromArgb(208, 17, 20, 30), Meta.StatusBorder(Note.Status));
            if (Note.Rating > 0) DrawRating(g, Width - 10, 10);

            string type = Meta.TypeLabel(Note.Type);
            Size ts = TextRenderer.MeasureText(type, F.Tiny, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
            int chipW = 31 + ts.Width + 12;
            var chip = new Rectangle(12, coverH - 31, chipW, 22);
            D.FillRound(g, chip, 7, Color.FromArgb(150, 10, 12, 17));
            D.StrokeRound(g, chip, 7, Color.FromArgb(42, 255, 255, 255), 1f);
            D.Icon(g, Meta.TypeIcon(Note.Type), new RectangleF(chip.X + 7, chip.Y + 4, 14, 14),
                Color.FromArgb(235, 255, 255, 255), 1.5f);
            D.Text(g, type, F.Tiny, Color.FromArgb(238, 255, 255, 255),
                new Rectangle(chip.X + 26, chip.Y - 1, ts.Width + 6, chip.Height + 2),
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left);

            if (!string.IsNullOrEmpty(Note.Year))
            {
                D.Text(g, Note.Year, F.Tiny, Color.FromArgb(175, 255, 255, 255),
                    new Rectangle(Width - 82, coverH - 30, 70, 20),
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
            }
        }

        void DrawBadge(Graphics g, string text, int x, int y, Color fg, Color bg, Color border)
        {
            Size ts = TextRenderer.MeasureText(text, F.Tiny, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
            var r = new Rectangle(x, y, ts.Width + 16, 22);
            D.FillRound(g, r, 7, bg);
            D.StrokeRound(g, r, 7, border, 1f);
            D.Text(g, text, F.Tiny, fg, r, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        void DrawRating(Graphics g, int right, int y)
        {
            string t = Note.Rating.ToString();
            Size ts = TextRenderer.MeasureText(t, F.Tiny, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
            int w = 14 + ts.Width + 14;
            var r = new Rectangle(right - w, y, w, 22);
            D.FillRound(g, r, 7, Color.FromArgb(208, 22, 17, 9));
            D.StrokeRound(g, r, 7, Color.FromArgb(120, C.Gold), 1f);
            D.Star(g, new RectangleF(r.X + 7, r.Y + 6, 10, 10), C.Gold, true, 1f);
            D.Text(g, t, F.Tiny, C.Gold,
                new Rectangle(r.X + 21, r.Y, ts.Width + 4, r.Height),
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }

        void PaintBody(Graphics g)
        {
            int x = 14;
            int w = Width - 28;
            int coverH = Height < 320 ? 132 : 150;

            var titleRect = new Rectangle(x, coverH + 10, w, 40);
            TextRenderer.DrawText(g, Note.Title, F.CardTitle, titleRect, C.Text,
                TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

            if (!string.IsNullOrEmpty(Note.Text))
            {
                var noteRect = new Rectangle(x, coverH + 52, w, 34);
                TextRenderer.DrawText(g, Note.Text, F.Small, noteRect, C.Muted,
                    TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            }

            int tx = x;
            int ty = Height - 70;
            int shown = 0;
            if (Note.Tags != null)
            {
                foreach (string raw in Note.Tags)
                {
                    string tag = (raw ?? "").Trim();
                    if (tag.Length == 0) continue;
                    if (shown >= 3) break;
                    Size ts = TextRenderer.MeasureText("#" + tag, F.Tiny, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
                    int cw = ts.Width + 12;
                    if (tx + cw > Width - 14) break;
                    var r = new Rectangle(tx, ty, cw, 21);
                    D.FillRound(g, r, 6, C.TagBg);
                    D.Text(g, "#" + tag, F.Tiny, Color.FromArgb(156, 165, 184),
                        new Rectangle(r.X + 6, r.Y, ts.Width + 4, r.Height),
                        TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                    tx += cw + 5;
                    shown++;
                }
                if (shown < Note.Tags.Count && shown > 0)
                {
                    string more = "+" + (Note.Tags.Count - shown);
                    Size ms = TextRenderer.MeasureText(more, F.Tiny, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
                    var r = new Rectangle(tx, ty, ms.Width + 12, 21);
                    if (r.Right < Width - 14)
                    {
                        D.FillRound(g, r, 6, C.TagBg);
                        D.Text(g, more, F.Tiny, C.Muted2,
                            new Rectangle(r.X, r.Y, r.Width, r.Height),
                            TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
                    }
                }
            }

            rTrash = new Rectangle(Width - 14 - 30, Height - 45, 30, 30);
            rEdit = new Rectangle(rTrash.X - 32, rTrash.Y, 30, 30);
            rCheck = new Rectangle(rEdit.X - 32, rTrash.Y, 30, 30);

            DrawAction(g, rCheck, "check", hoverAction == 0, C.Green);
            DrawAction(g, rEdit, "pencil", hoverAction == 1, C.Text);
            DrawAction(g, rTrash, "trash", hoverAction == 2, C.Danger);
        }

        void DrawAction(Graphics g, Rectangle r, string icon, bool hot, Color hotColor)
        {
            if (hot)
            {
                D.FillRound(g, r, 9, Color.FromArgb(18, hotColor));
                D.Icon(g, icon, new RectangleF(r.X + 8, r.Y + 8, 14, 14), hotColor, 1.7f);
            }
            else
            {
                D.Icon(g, icon, new RectangleF(r.X + 8, r.Y + 8, 14, 14), C.Muted2, 1.6f);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int a = -1;
            if (rCheck.Contains(e.Location)) a = 0;
            else if (rEdit.Contains(e.Location)) a = 1;
            else if (rTrash.Contains(e.Location)) a = 2;
            if (a != hoverAction || !hover)
            {
                hoverAction = a;
                hover = true;
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hover = false;
            hoverAction = -1;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (rCheck.Contains(e.Location))
            {
                if (StatusClick != null) StatusClick(this, EventArgs.Empty);
                return;
            }
            if (rEdit.Contains(e.Location))
            {
                if (EditClick != null) EditClick(this, EventArgs.Empty);
                return;
            }
            if (rTrash.Contains(e.Location))
            {
                if (DeleteClick != null) DeleteClick(this, EventArgs.Empty);
                return;
            }
            if (OpenClick != null) OpenClick(this, EventArgs.Empty);
        }
    }

    public class CardsPanel : Panel
    {
        public string EmptyTitle = "Полка пока пустая";
        public string EmptyHint = "Добавьте первый фильм, сериал или книгу — сверху есть быстрая строка.";

        int scrollY, contentH, cols = 4, cardW = 280, cardH = 336, gap = 18;
        bool dragging;
        int dragOffset, dragThumbH, dragTrackY, dragTrackH;

        public CardsPanel()
        {
            BackColor = C.Bg;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        }

        public bool ScrollVisible { get { return contentH > ClientSize.Height; } }

        public void LayoutCards(int columns, int width, int height, int gapPx)
        {
            cols = Math.Max(1, columns);
            cardW = Math.Max(120, width);
            cardH = height;
            gap = gapPx;
            int n = Controls.Count;
            int rows = n == 0 ? 0 : (n + cols - 1) / cols;
            contentH = rows == 0 ? 0 : rows * (cardH + gap) - gap;
            scrollY = Math.Max(0, Math.Min(scrollY, Math.Max(0, contentH - ClientSize.Height)));
            Apply();
            Invalidate();
        }

        void Apply()
        {
            for (int i = 0; i < Controls.Count; i++)
            {
                int r = i / cols, c = i % cols;
                Controls[i].SetBounds(c * (cardW + gap), r * (cardH + gap) - scrollY, cardW, cardH);
            }
        }

        public void ResetScroll()
        {
            scrollY = 0;
            Apply();
            Invalidate();
        }

        public void ScrollBy(int dy)
        {
            int max = Math.Max(0, contentH - ClientSize.Height);
            int next = Math.Max(0, Math.Min(max, scrollY + dy));
            if (next == scrollY) return;
            scrollY = next;
            Apply();
            Invalidate();
        }

        public void EnsureVisible(Control c)
        {
            int top = c.Top + scrollY;
            if (top < scrollY) scrollY = top;
            else if (top + c.Height > scrollY + ClientSize.Height) scrollY = top + c.Height - ClientSize.Height;
            scrollY = Math.Max(0, Math.Min(Math.Max(0, contentH - ClientSize.Height), scrollY));
            Apply();
            Invalidate();
        }

        Rectangle TrackRect()
        {
            return new Rectangle(Width - 9, 2, 5, Math.Max(10, ClientSize.Height - 4));
        }

        Rectangle ThumbRect()
        {
            var track = TrackRect();
            int h = Math.Max(32, (int)((float)ClientSize.Height / Math.Max(1, contentH) * track.Height));
            int range = Math.Max(1, contentH - ClientSize.Height);
            int y = track.Y + (int)((float)scrollY / range * (track.Height - h));
            return new Rectangle(track.X, y, track.Width, h);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(C.Bg);

            if (Controls.Count == 0)
            {
                int cy = Math.Max(30, ClientSize.Height / 2 - 60);
                D.Icon(g, "film", new RectangleF(ClientSize.Width / 2f - 22, cy, 44, 44), Color.FromArgb(75, C.Muted2), 1.4f);
                D.Text(g, EmptyTitle, F.H2, C.Text,
                    new Rectangle(0, cy + 62, ClientSize.Width, 30),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.Top);
                D.Text(g, EmptyHint, F.Small, C.Muted,
                    new Rectangle(ClientSize.Width / 2 - 240, cy + 96, 480, 40),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.WordBreak);
                return;
            }

            if (ScrollVisible)
            {
                var track = TrackRect();
                var thumb = ThumbRect();
                D.FillRound(g, track, 3, Color.FromArgb(23, 27, 37));
                D.FillRound(g, thumb, 3, Color.FromArgb(86, 96, 120));
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (ScrollVisible && e.X >= Width - 12)
            {
                var thumb = ThumbRect();
                if (thumb.Contains(e.Location))
                {
                    dragging = true;
                    dragOffset = e.Y - thumb.Y;
                    dragThumbH = thumb.Height;
                    dragTrackY = TrackRect().Y;
                    dragTrackH = TrackRect().Height;
                }
                else
                {
                    ScrollBy(e.Y < thumb.Y ? -ClientSize.Height : ClientSize.Height);
                }
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (dragging)
            {
                int span = Math.Max(1, dragTrackH - dragThumbH);
                float ratio = (float)(e.Y - dragOffset - dragTrackY) / span;
                ratio = Math.Max(0f, Math.Min(1f, ratio));
                scrollY = (int)(ratio * Math.Max(0, contentH - ClientSize.Height));
                Apply();
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            dragging = false;
            base.OnMouseUp(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            dragging = false;
            base.OnMouseLeave(e);
        }
    }
}
