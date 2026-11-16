using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Polka
{
    static class Win
    {
        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        static extern int SetWindowTheme(IntPtr hwnd, string subApp, string subIdList);

        public static void DarkTitle(IntPtr handle)
        {
            try
            {
                int on = 1;
                if (DwmSetWindowAttribute(handle, 20, ref on, sizeof(int)) != 0)
                    DwmSetWindowAttribute(handle, 19, ref on, sizeof(int));
            }
            catch { }
        }

        public static void DarkScroll(IntPtr handle)
        {
            try { SetWindowTheme(handle, "DarkMode_Explorer", null); }
            catch { }
        }
    }

    public class EditorForm : Form
    {
        Note note;
        bool isNew;
        string selectedType;
        string selectedStatus;

        public Note Result;
        public bool Deleted;

        InputField fTitle, fYear, fTags, fPoster, fNote;
        StarRating rating;
        List<Pill> typePills = new List<Pill>();
        List<Pill> statusPills = new List<Pill>();
        FlatButton saveBtn, cancelBtn, deleteBtn, closeBtn;

        public EditorForm(Note existing)
        {
            isNew = existing == null;
            note = existing == null ? new Note() : existing.Clone();
            selectedType = note.Type;
            selectedStatus = note.Status;

            Text = isNew ? "Новая заметка" : "Заметка";
            ClientSize = new Size(620, 730);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            AutoScaleMode = AutoScaleMode.None;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ShowIcon = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = C.EditorBg;
            ForeColor = C.Text;
            Font = F.Body;
            KeyPreview = true;
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            BuildUI();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Win.DarkTitle(Handle);
        }

        void BuildUI()
        {
            int x = 24;
            int w = 572;

            foreach (string t in Meta.Types)
            {
                var p = new Pill(Meta.TypeLabel(t));
                p.Mode = "seg";
                p.Key = t;
                p.IconName = Meta.TypeIcon(t);
                p.IconColor = Meta.TypeColor(t);
                p.Surface = C.EditorBg;
                p.Height = 34;
                p.Selected = (t == note.Type);
                p.Width = p.PreferredWidth();
                p.Click += delegate(object s, EventArgs e)
                {
                    var pill = (Pill)s;
                    selectedType = pill.Key;
                    foreach (var q in typePills) { q.Selected = (q.Key == selectedType); q.Invalidate(); }
                };
                typePills.Add(p);
                Controls.Add(p);
            }

            int tx = x;
            foreach (var p in typePills)
            {
                p.Location = new Point(tx, 94);
                tx += p.Width + 6;
            }

            fTitle = new InputField(false, "Например: Дюна: Часть вторая");
            fTitle.Surface = C.EditorBg;
            fTitle.SetBounds(x, 164, w, 42);
            fTitle.Value = note.Title;
            fTitle.Box.TextChanged += delegate
            {
                if (fTitle.Error)
                {
                    fTitle.Error = false;
                    fTitle.Invalidate();
                }
            };
            Controls.Add(fTitle);
            EnterSaves(fTitle);

            fYear = new InputField(false, "2024");
            fYear.Surface = C.EditorBg;
            fYear.SetBounds(x, 238, 170, 42);
            fYear.Value = note.Year;
            Controls.Add(fYear);
            EnterSaves(fYear);

            rating = new StarRating();
            rating.Surface = C.EditorBg;
            rating.Value = note.Rating;
            rating.SetBounds(212, 242, 170, 34);
            Controls.Add(rating);

            foreach (string s in Meta.Statuses)
            {
                var p = new Pill(Meta.StatusLabel(s));
                p.Mode = "seg";
                p.Key = s;
                p.Surface = C.EditorBg;
                p.Height = 34;
                p.Selected = (s == note.Status);
                p.Width = p.PreferredWidth();
                p.Click += delegate(object sender, EventArgs e)
                {
                    var pill = (Pill)sender;
                    selectedStatus = pill.Key;
                    foreach (var q in statusPills) { q.Selected = (q.Key == selectedStatus); q.Invalidate(); }
                };
                statusPills.Add(p);
                Controls.Add(p);
            }

            int sx = x;
            foreach (var p in statusPills)
            {
                p.Location = new Point(sx, 316);
                sx += p.Width + 6;
            }

            fTags = new InputField(false, "фантастика, на вечер, посоветовал друг");
            fTags.Surface = C.EditorBg;
            fTags.SetBounds(x, 386, w, 42);
            fTags.Value = string.Join(", ", note.Tags.ToArray());
            Controls.Add(fTags);
            EnterSaves(fTags);

            fPoster = new InputField(false, "https://…");
            fPoster.Surface = C.EditorBg;
            fPoster.SetBounds(x, 462, w, 42);
            fPoster.Value = note.Poster;
            Controls.Add(fPoster);
            EnterSaves(fPoster);

            fNote = new InputField(true, "Чем зацепило, где смотреть, с кем обсудить…");
            fNote.Surface = C.EditorBg;
            fNote.SetBounds(x, 538, w, 100);
            fNote.Value = note.Text;
            Controls.Add(fNote);

            saveBtn = new FlatButton("Сохранить");
            saveBtn.Primary = true;
            saveBtn.SetBounds(476, 676, 120, 40);
            saveBtn.Click += delegate { SaveNote(); };
            Controls.Add(saveBtn);

            cancelBtn = new FlatButton("Отмена");
            cancelBtn.Ghost = true;
            cancelBtn.Surface = C.EditorBg;
            cancelBtn.SetBounds(348, 676, 120, 40);
            cancelBtn.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancelBtn);

            if (!isNew)
            {
                deleteBtn = new FlatButton("Удалить");
                deleteBtn.Danger = true;
                deleteBtn.Ghost = true;
                deleteBtn.Surface = C.EditorBg;
                deleteBtn.SetBounds(24, 676, 110, 40);
                deleteBtn.Click += delegate
                {
                    if (MessageBox.Show(this, "Удалить «" + note.Title + "»?", "Полка",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                    Deleted = true;
                    DialogResult = DialogResult.Abort;
                    Close();
                };
                Controls.Add(deleteBtn);
            }

            closeBtn = new FlatButton("");
            closeBtn.IconName = "close";
            closeBtn.Ghost = true;
            closeBtn.Surface = C.EditorBg;
            closeBtn.SetBounds(564, 16, 34, 34);
            closeBtn.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(closeBtn);
        }

        void EnterSaves(InputField f)
        {
            f.Box.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    SaveNote();
                }
            };
        }

        Note BuildResult()
        {
            note.Title = fTitle.Value.Trim();
            note.Type = selectedType;
            note.Status = selectedStatus;
            note.Rating = rating.Value;
            note.Year = fYear.Value.Trim();
            note.Poster = fPoster.Value.Trim();
            note.Text = fNote.Value.Trim();
            note.Tags = new List<string>();
            foreach (string part in fTags.Value.Split(new char[] { ',', ';' }))
            {
                string t = part.Trim();
                if (t.Length > 0 && !note.Tags.Contains(t) && note.Tags.Count < 8) note.Tags.Add(t);
            }
            note.Updated = DateTime.Now;
            return note;
        }

        public Note SelfTest()
        {
            fTitle.Value = "Проверка редактора";
            fYear.Value = "2025";
            fTags.Value = "тег1, тег2";
            fNote.Value = "Текст заметки";
            rating.Value = 4;
            selectedType = "book";
            selectedStatus = "doing";
            return BuildResult();
        }

        void SaveNote()
        {
            string title = fTitle.Value.Trim();
            if (title.Length == 0)
            {
                fTitle.Error = true;
                fTitle.Invalidate();
                fTitle.Box.Focus();
                return;
            }

            Result = BuildResult();
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return true;
            }
            if (keyData == (Keys.Control | Keys.Enter))
            {
                SaveNote();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        void Label(Graphics g, string text, int x, int y)
        {
            D.Text(g, text, F.Label, C.Muted, new Rectangle(x, y, 300, 15),
                TextFormatFlags.Left | TextFormatFlags.Top);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(C.EditorBg);

            using (var br = new LinearGradientBrush(new Rectangle(0, 0, ClientSize.Width, 90),
                Color.FromArgb(24, 28, 40), C.EditorBg, 90f))
                g.FillRectangle(br, 0, 0, ClientSize.Width, 90);

            D.Text(g, isNew ? "Новая заметка" : "Заметка", F.H2, C.Text,
                new Rectangle(24, 20, 500, 30), TextFormatFlags.Left | TextFormatFlags.Top);

            Label(g, "ТИП", 24, 76);
            Label(g, "НАЗВАНИЕ", 24, 146);
            Label(g, "ГОД", 24, 220);
            Label(g, "ОЦЕНКА", 214, 220);
            Label(g, "СТАТУС", 24, 298);
            Label(g, "ТЕГИ", 24, 368);
            Label(g, "ССЫЛКА НА ПОСТЕР", 24, 444);
            Label(g, "ЗАМЕТКА", 24, 520);

            Size lbl = TextRenderer.MeasureText("ССЫЛКА НА ПОСТЕР ", F.Label, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
            D.Text(g, "(необязательно)", F.Small, Color.FromArgb(120, C.Muted2),
                new Rectangle(24 + lbl.Width, 441, 160, 16), TextFormatFlags.Left | TextFormatFlags.Top);

            using (var pen = new Pen(C.Line))
                g.DrawLine(pen, 0, 660, ClientSize.Width, 660);
        }
    }
}
