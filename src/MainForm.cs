using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace Polka
{
    class WheelFilter : IMessageFilter
    {
        CardsPanel panel;
        MainForm owner;

        public WheelFilter(MainForm ownerForm, CardsPanel cards)
        {
            owner = ownerForm;
            panel = cards;
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg != 0x020A) return false;
            if (!owner.ContainsFocus) return false;
            if (!panel.IsHandleCreated || !panel.Visible) return false;

            Point client = panel.PointToClient(Cursor.Position);
            if (!panel.ClientRectangle.Contains(client)) return false;

            long w = m.WParam.ToInt64();
            int delta = (int)((w >> 16) & 0xFFFF);
            if (delta > 32767) delta -= 65536;
            panel.ScrollBy(delta > 0 ? -110 : 110);
            return true;
        }
    }

    public class MainForm : Form
    {
        List<Note> notes = new List<Note>();
        string typeFilter = "all";
        string statusFilter = "all";
        string sortMode = "new";
        string query = "";
        string quickType = "movie";

        InputField search, quickInput;
        FlatButton importBtn, exportBtn, addBtn, quickTypeBtn, quickAddBtn, diceBtn, sortBtn;
        CardsPanel cards;
        List<Pill> tabs = new List<Pill>();
        List<Pill> chips = new List<Pill>();
        ContextMenuStrip typeMenu, sortMenu;
        Bitmap bgCache;
        Rectangle quickRect;
        int contentLeft, contentW;
        int chipsTop = 356;
        int visibleCount;
        Toast toast;
        Timer flashTimer;
        NoteCard flashing;
        WheelFilter wheelFilter;

        public MainForm()
        {
            Text = "Полка";
            ClientSize = new Size(1280, 800);
            MinimumSize = new Size(1040, 782);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = C.Bg;
            ForeColor = C.Text;
            Font = F.Body;
            KeyPreview = true;
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch { }

            BuildUI();

            notes = Store.Load();
            if (notes == null)
            {
                notes = Store.Seed();
                Store.Save(notes);
            }

            RefreshAll();

            wheelFilter = new WheelFilter(this, cards);
            Application.AddMessageFilter(wheelFilter);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (Program.ShotPath != null)
            {
                TopMost = true;
                Activate();
                if (Program.ShotMode == "small") ClientSize = new Size(1024, 741);
                if (Program.ShotMode == "flow")
                {
                    var flowTimer = new Timer();
                    flowTimer.Interval = 600;
                    flowTimer.Tick += delegate
                    {
                        flowTimer.Stop();
                        quickInput.Value = "Твин Пикс";
                        quickType = "series";
                        quickAddBtn.Text = "Добавить";
                        QuickAdd();
                    };
                    flowTimer.Start();
                }
                var timer = new Timer();
                timer.Interval = 1100;
                timer.Tick += delegate
                {
                    timer.Stop();
                    CaptureShot();
                };
                timer.Start();
            }
        }

        void CaptureShot()
        {
            try
            {
                Program.Capture(this, Program.ShotPath);
            }
            catch (Exception ex)
            {
                Program.Log("shot error: " + ex.Message);
            }
            Close();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Win.DarkTitle(Handle);
            if (cards != null && cards.IsHandleCreated) Win.DarkScroll(cards.Handle);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (wheelFilter != null) Application.RemoveMessageFilter(wheelFilter);
            if (bgCache != null) { bgCache.Dispose(); bgCache = null; }
            base.OnFormClosed(e);
        }

        /* ------------------------------------------------------------------ */
        /*  Построение интерфейса                                             */
        /* ------------------------------------------------------------------ */

        void BuildUI()
        {
            search = new InputField(false, "Поиск по названию, тегам, заметкам…");
            search.ShowSearch = true;
            search.Surface = C.Bg;
            search.SetBounds(0, 0, 300, 40);
            search.Box.TextChanged += delegate
            {
                query = search.Box.Text;
                RefreshCards();
                Invalidate();
            };
            Controls.Add(search);

            importBtn = new FlatButton("");
            importBtn.IconName = "upload";
            importBtn.Ghost = true;
            importBtn.SetBounds(0, 0, 40, 40);
            importBtn.Click += delegate { ImportJson(); };
            Controls.Add(importBtn);

            exportBtn = new FlatButton("");
            exportBtn.IconName = "download";
            exportBtn.Ghost = true;
            exportBtn.SetBounds(0, 0, 40, 40);
            exportBtn.Click += delegate { ExportJson(); };
            Controls.Add(exportBtn);

            addBtn = new FlatButton("Заметка");
            addBtn.IconName = "plus";
            addBtn.Primary = true;
            addBtn.SetBounds(0, 0, 122, 40);
            addBtn.Click += delegate { OpenEditor(null); };
            Controls.Add(addBtn);

            quickTypeBtn = new FlatButton(Meta.TypeLabel(quickType) + "  ▾");
            quickTypeBtn.Ghost = true;
            quickTypeBtn.Surface = C.Quick;
            quickTypeBtn.SetBounds(0, 0, 96, 44);
            quickTypeBtn.Click += delegate { typeMenu.Show(quickTypeBtn, new Point(0, quickTypeBtn.Height + 6)); };
            Controls.Add(quickTypeBtn);

            quickInput = new InputField(false, "Быстро добавить: название фильма, сериала, книги…");
            quickInput.Surface = C.Quick;
            quickInput.SetBounds(0, 0, 300, 44);
            quickInput.Box.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    QuickAdd();
                }
            };
            Controls.Add(quickInput);

            quickAddBtn = new FlatButton("Добавить");
            quickAddBtn.IconName = "plus";
            quickAddBtn.Primary = true;
            quickAddBtn.SetBounds(0, 0, 118, 44);
            quickAddBtn.Click += delegate { QuickAdd(); };
            Controls.Add(quickAddBtn);

            diceBtn = new FlatButton("Что посмотреть?");
            diceBtn.IconName = "dice";
            diceBtn.Ghost = true;
            diceBtn.SetBounds(0, 0, 180, 34);
            diceBtn.Click += delegate { RandomPick(); };
            Controls.Add(diceBtn);

            sortBtn = new FlatButton("Сначала новые  ▾");
            sortBtn.Ghost = true;
            sortBtn.SetBounds(0, 0, 168, 34);
            sortBtn.Click += delegate
            {
                sortMenu.Show(sortBtn, new Point(sortBtn.Width - 168, sortBtn.Height + 6));
            };
            Controls.Add(sortBtn);

            cards = new CardsPanel();
            Controls.Add(cards);

            typeMenu = BuildMenu();
            foreach (string t in Meta.Types)
            {
                string key = t;
                var item = new ToolStripMenuItem(Meta.TypeLabel(t));
                item.ForeColor = C.Text;
                item.Padding = new Padding(2, 6, 18, 6);
                item.Click += delegate
                {
                    quickType = key;
                    quickTypeBtn.Text = Meta.TypeLabel(key) + "  ▾";
                    quickTypeBtn.Invalidate();
                };
                typeMenu.Items.Add(item);
            }

            sortMenu = BuildMenu();
            AddSortItem(sortMenu, "Сначала новые", "new");
            AddSortItem(sortMenu, "Сначала старые", "old");
            AddSortItem(sortMenu, "По названию", "title");
            AddSortItem(sortMenu, "По оценке", "rating");
        }

        ContextMenuStrip BuildMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable());
            menu.RenderMode = ToolStripRenderMode.Professional;
            menu.BackColor = C.Panel2;
            menu.ForeColor = C.Text;
            menu.Font = F.Body;
            menu.ShowImageMargin = false;
            menu.ShowCheckMargin = false;
            return menu;
        }

        void AddSortItem(ContextMenuStrip menu, string label, string key)
        {
            var item = new ToolStripMenuItem(label);
            item.ForeColor = C.Text;
            item.Padding = new Padding(2, 6, 18, 6);
            item.Click += delegate
            {
                sortMode = key;
                sortBtn.Text = label + "  ▾";
                sortBtn.Invalidate();
                RefreshCards();
            };
            menu.Items.Add(item);
        }

        /* ------------------------------------------------------------------ */
        /*  Раскладка                                                          */
        /* ------------------------------------------------------------------ */

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (bgCache != null) { bgCache.Dispose(); bgCache = null; }
            if (cards != null) LayoutAll();
            Invalidate();
        }

        void LayoutAll()
        {
            int w = ClientSize.Width;
            contentW = Math.Min(w - 96, 1180);
            contentLeft = (w - contentW) / 2;
            int right = contentLeft + contentW;

            addBtn.SetBounds(right - 122, 18, 122, 40);
            exportBtn.SetBounds(addBtn.Left - 8 - 40, 18, 40, 40);
            importBtn.SetBounds(exportBtn.Left - 8 - 40, 18, 40, 40);

            int searchW = Math.Max(190, Math.Min(360, importBtn.Left - 24 - (contentLeft + 220)));
            search.SetBounds(importBtn.Left - 14 - searchW, 18, searchW, 40);

            bool compact = ClientSize.Height < 790;
            int quickTop = compact ? 192 : 212;
            int tabsTop = quickTop + 92;
            chipsTop = tabsTop + 52;
            int cardsTop = chipsTop + 48;

            quickRect = new Rectangle(contentLeft, quickTop, contentW, 66);
            quickTypeBtn.SetBounds(contentLeft + 10, quickTop + 11, 96, 44);
            quickAddBtn.SetBounds(right - 10 - 118, quickTop + 11, 118, 44);
            quickInput.SetBounds(contentLeft + 112, quickTop + 11, quickAddBtn.Left - 12 - (contentLeft + 112), 44);

            int tx = contentLeft;
            foreach (var t in tabs)
            {
                t.SetBounds(tx, tabsTop, t.PreferredWidth(), 36);
                tx += t.Width + 8;
            }

            int cx = contentLeft;
            foreach (var c in chips)
            {
                c.SetBounds(cx, chipsTop, c.PreferredWidth(), 32);
                cx += c.Width + 6;
            }

            sortBtn.SetBounds(right - 168, chipsTop - 1, 168, 34);
            diceBtn.SetBounds(sortBtn.Left - 8 - 180, chipsTop - 1, 180, 34);

            cards.SetBounds(contentLeft, cardsTop, contentW, Math.Max(120, ClientSize.Height - cardsTop - 20));
            LayoutCards();
        }

        void LayoutCards()
        {
            const int gap = 18;
            const int minW = 230;
            int avail = Math.Max(200, cards.ClientSize.Width - 12);
            int cols = Math.Max(1, (avail + gap) / (minW + gap));
            int cardW = (avail - (cols - 1) * gap) / cols;
            int cardH = Math.Max(296, Math.Min(336, cards.ClientSize.Height - 2));
            cards.LayoutCards(cols, cardW, cardH, gap);
        }

        /* ------------------------------------------------------------------ */
        /*  Данные и перерисовка                                               */
        /* ------------------------------------------------------------------ */

        IEnumerable<Note> VisibleNotes()
        {
            IEnumerable<Note> list = notes;
            if (typeFilter != "all") list = list.Where(n => n.Type == typeFilter);
            if (statusFilter != "all") list = list.Where(n => n.Status == statusFilter);

            string q = (query ?? "").Trim().ToLowerInvariant();
            if (q.Length > 0)
            {
                list = list.Where(n =>
                    (n.Title ?? "").ToLowerInvariant().Contains(q) ||
                    (n.Text ?? "").ToLowerInvariant().Contains(q) ||
                    (n.Year ?? "").Contains(q) ||
                    n.Tags.Any(t => (t ?? "").ToLowerInvariant().Contains(q)));
            }

            switch (sortMode)
            {
                case "old": return list.OrderBy(n => n.Created);
                case "title": return list.OrderBy(n => n.Title, StringComparer.CurrentCulture);
                case "rating": return list.OrderByDescending(n => n.Rating).ThenByDescending(n => n.Created);
                default: return list.OrderByDescending(n => n.Created);
            }
        }

        void RefreshAll()
        {
            RebuildTabs();
            RebuildChips();
            RefreshCards();
            LayoutAll();
            Invalidate();
        }

        void RefreshDeferred()
        {
            try { BeginInvoke(new Action(RefreshAll)); }
            catch { }
        }

        Pill CreateTab(string label, string key, string icon, Color iconColor, int count, bool selected)
        {
            var p = new Pill(label);
            p.Mode = "tab";
            p.Key = key;
            p.IconName = icon;
            p.IconColor = iconColor;
            p.Count = count;
            p.Selected = selected;
            p.Surface = C.Bg;
            p.Click += delegate
            {
                string k = p.Key;
                typeFilter = k;
                cards.ResetScroll();
                RefreshDeferred();
            };
            tabs.Add(p);
            Controls.Add(p);
            return p;
        }

        void RebuildTabs()
        {
            foreach (var p in tabs) { Controls.Remove(p); p.Dispose(); }
            tabs.Clear();

            CreateTab("Все", "all", "", C.Accent, notes.Count, typeFilter == "all");
            foreach (string t in Meta.Types)
            {
                string key = t;
                int count = notes.Count(n => n.Type == key);
                CreateTab(Meta.TypePlural(key), key, Meta.TypeIcon(key), Meta.TypeColor(key), count, typeFilter == key);
            }
        }

        Pill CreateChip(string label, string key, int count, bool selected)
        {
            var p = new Pill(label);
            p.Mode = "chip";
            p.Key = key;
            p.Count = count;
            p.Selected = selected;
            p.Surface = C.Bg;
            p.Height = 32;
            p.Click += delegate
            {
                statusFilter = p.Key;
                cards.ResetScroll();
                RefreshDeferred();
            };
            chips.Add(p);
            Controls.Add(p);
            return p;
        }

        void RebuildChips()
        {
            foreach (var p in chips) { Controls.Remove(p); p.Dispose(); }
            chips.Clear();

            CreateChip("Все", "all", notes.Count, statusFilter == "all");
            foreach (string s in Meta.Statuses)
            {
                string key = s;
                int count = notes.Count(n => n.Status == key);
                CreateChip(Meta.StatusLabel(key), key, count, statusFilter == key);
            }
        }

        void RefreshCards()
        {
            cards.SuspendLayout();
            while (cards.Controls.Count > 0)
            {
                var c = cards.Controls[0];
                cards.Controls.Remove(c);
                c.Dispose();
            }

            var list = VisibleNotes().ToList();
            visibleCount = list.Count;

            foreach (var n in list)
            {
                Note note = n;
                var card = new NoteCard(note);
                card.StatusClick += delegate { CycleStatus(note); };
                card.EditClick += delegate { OpenEditor(note); };
                card.DeleteClick += delegate { DeleteNote(note); };
                card.OpenClick += delegate { OpenEditor(note); };
                cards.Controls.Add(card);
            }

            cards.EmptyTitle = notes.Count == 0 ? "Полка пока пустая" : "Ничего не нашлось";
            cards.EmptyHint = notes.Count == 0
                ? "Добавьте первый фильм, сериал или книгу — сверху есть быстрая строка."
                : "Под текущие фильтры заметок нет. Попробуйте изменить поиск или сбросить фильтры.";

            cards.ResumeLayout();
            LayoutCards();
            Invalidate();
        }

        /* ------------------------------------------------------------------ */
        /*  Действия                                                           */
        /* ------------------------------------------------------------------ */

        void CycleStatus(Note n)
        {
            n.Status = Meta.StatusNext(n.Status);
            n.Updated = DateTime.Now;
            Store.Save(notes);
            RefreshAll();
            ShowToast("«" + n.Title + "» → " + Meta.StatusLabel(n.Status));
        }

        void DeleteNote(Note n)
        {
            if (MessageBox.Show(this, "Удалить «" + n.Title + "»?", "Полка",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            notes.RemoveAll(x => x.Id == n.Id);
            Store.Save(notes);
            RefreshAll();
            ShowToast("Удалено");
        }

        void OpenEditor(Note note)
        {
            using (var dlg = new EditorForm(note))
            {
                var res = dlg.ShowDialog(this);
                if (dlg.Deleted)
                {
                    if (note != null)
                    {
                        notes.RemoveAll(x => x.Id == note.Id);
                        Store.Save(notes);
                        RefreshAll();
                        ShowToast("Удалено");
                    }
                    return;
                }
                if (res != DialogResult.OK || dlg.Result == null) return;

                if (note == null)
                {
                    notes.Insert(0, dlg.Result);
                    typeFilter = "all";
                    statusFilter = "all";
                    query = "";
                    search.Value = "";
                    ShowToast("«" + dlg.Result.Title + "» добавлено");
                }
                else
                {
                    int i = notes.FindIndex(x => x.Id == note.Id);
                    if (i >= 0) notes[i] = dlg.Result;
                    ShowToast("Сохранено");
                }
                Store.Save(notes);
                RefreshAll();
            }
        }

        void QuickAdd()
        {
            string title = quickInput.Value.Trim();
            if (title.Length == 0)
            {
                quickInput.Box.Focus();
                return;
            }
            var n = new Note();
            n.Title = title;
            n.Type = quickType;
            n.Status = "want";
            n.Created = DateTime.Now;
            notes.Insert(0, n);

            typeFilter = "all";
            statusFilter = "all";
            query = "";
            search.Value = "";
            quickInput.Value = "";
            Store.Save(notes);
            RefreshAll();
            ShowToast("«" + n.Title + "» в списке");
        }

        void RandomPick()
        {
            var pool = VisibleNotes().Where(x => x.Status != "done").ToList();
            if (pool.Count == 0) pool = VisibleNotes().ToList();
            if (pool.Count == 0)
            {
                ShowToast("Список пуст — сначала добавьте заметку");
                return;
            }

            var pick = pool[new Random().Next(pool.Count)];
            NoteCard found = null;
            foreach (Control c in cards.Controls)
            {
                var nc = c as NoteCard;
                if (nc != null && nc.Note.Id == pick.Id) { found = nc; break; }
            }
            if (found != null)
            {
                cards.EnsureVisible(found);
                if (flashing != null) flashing.Flash = false;
                flashing = found;
                found.Flash = true;
                if (flashTimer == null)
                {
                    flashTimer = new Timer();
                    flashTimer.Interval = 1500;
                    flashTimer.Tick += delegate
                    {
                        flashTimer.Stop();
                        if (flashing != null) { flashing.Flash = false; flashing = null; }
                    };
                }
                flashTimer.Stop();
                flashTimer.Start();
            }
            ShowToast("Случайный выбор: «" + pick.Title + "»");
        }

        void ShowToast(string message)
        {
            if (toast != null)
            {
                try { toast.Close(); }
                catch { }
                toast = null;
            }
            var t = new Toast(message, this);
            toast = t;
            t.FormClosed += delegate { if (toast == t) toast = null; };
            t.Show(this);
        }

        /* ------------------------------------------------------------------ */
        /*  Экспорт / импорт                                                   */
        /* ------------------------------------------------------------------ */

        void ExportJson()
        {
            if (notes.Count == 0)
            {
                ShowToast("Пока нечего экспортировать");
                return;
            }
            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "JSON (*.json)|*.json";
                dlg.FileName = "polka-" + DateTime.Now.ToString("yyyy-MM-dd") + ".json";
                dlg.Title = "Экспорт полки";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    var ser = new JavaScriptSerializer();
                    File.WriteAllText(dlg.FileName, ser.Serialize(notes));
                    ShowToast("Файл сохранён");
                }
                catch
                {
                    ShowToast("Не удалось сохранить файл");
                }
            }
        }

        void ImportJson()
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = "JSON (*.json)|*.json|Все файлы (*.*)|*.*";
                dlg.Title = "Импорт полки";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    var ser = new JavaScriptSerializer();
                    var list = ser.Deserialize<List<Note>>(File.ReadAllText(dlg.FileName));
                    if (list == null) throw new Exception("empty");

                    var seen = new HashSet<string>(notes.Select(x => (x.Title ?? "").ToLowerInvariant() + "|" + x.Type));
                    int added = 0;
                    foreach (var raw in list)
                    {
                        if (raw == null || string.IsNullOrEmpty(raw.Title)) continue;
                        string key = raw.Title.ToLowerInvariant() + "|" + (raw.Type ?? "other");
                        if (seen.Contains(key)) continue;
                        seen.Add(key);
                        raw.Id = Guid.NewGuid().ToString("N");
                        if (raw.Tags == null) raw.Tags = new List<string>();
                        if (raw.Created == default(DateTime)) raw.Created = DateTime.Now;
                        notes.Add(raw);
                        added++;
                    }
                    Store.Save(notes);
                    RefreshAll();
                    ShowToast(added > 0 ? "Импортировано: " + added : "Новых заметок нет");
                }
                catch
                {
                    ShowToast("Не удалось прочитать файл");
                }
            }
        }

        /* ------------------------------------------------------------------ */
        /*  Клавиатура                                                         */
        /* ------------------------------------------------------------------ */

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            bool typing = (search != null && search.Box.Focused) || (quickInput != null && quickInput.Box.Focused);

            if (!typing && (keyData == Keys.OemQuestion || keyData == Keys.Divide))
            {
                search.Box.Focus();
                return true;
            }
            if (!typing && keyData == Keys.N)
            {
                OpenEditor(null);
                return true;
            }
            if (keyData == Keys.Escape && search.Box.Focused)
            {
                search.Value = "";
                query = "";
                RefreshCards();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        /* ------------------------------------------------------------------ */
        /*  Отрисовка                                                          */
        /* ------------------------------------------------------------------ */

        Bitmap RenderBg()
        {
            int w = Math.Max(1, ClientSize.Width);
            int h = Math.Max(1, ClientSize.Height);
            var bmp = new Bitmap(w, h);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var br = new LinearGradientBrush(new Rectangle(0, 0, w, h),
                    Color.FromArgb(13, 16, 24), C.Bg, 90f))
                    g.FillRectangle(br, 0, 0, w, h);

                float r = Math.Max(220, w / 3f);
                D.Glow(g, new PointF(w * 0.16f, 300), r, Color.FromArgb(52, 92, 106, 236));
                D.Glow(g, new PointF(w * 0.90f, 110), r * 0.85f, Color.FromArgb(38, 152, 98, 226));

                using (var b = new SolidBrush(Color.FromArgb(236, C.Bg)))
                    g.FillRectangle(b, 0, 0, w, 76);
                using (var pen = new Pen(Color.FromArgb(235, C.Line)))
                    g.DrawLine(pen, 0, 75, w, 75);
            }
            return bmp;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (bgCache == null || bgCache.Width != ClientSize.Width || bgCache.Height != ClientSize.Height)
            {
                if (bgCache != null) bgCache.Dispose();
                bgCache = RenderBg();
            }
            e.Graphics.DrawImageUnscaled(bgCache, 0, 0);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var hint = g.TextRenderingHint;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            DrawBrand(g);
            DrawHero(g); 

            D.FillRound(g, quickRect, 16, C.Quick);
            using (var pen = new Pen(C.Line))
            using (var path = D.Round(quickRect, 16))
                g.DrawPath(pen, path);
            using (var pen = new Pen(Color.FromArgb(80, 255, 255, 255)))
                g.DrawLine(pen, quickRect.X + 16, quickRect.Y + 1, quickRect.Right - 16, quickRect.Y + 1);

            DrawCounter(g);
            g.TextRenderingHint = hint;
        }

        void DrawBrand(Graphics g)
        {
            var logo = new Rectangle(contentLeft, 20, 36, 36);
            using (var br = new LinearGradientBrush(logo, C.Accent, C.Accent2, 45f))
            using (var path = D.Round(logo, 11))
                g.FillPath(br, path);
            D.Text(g, "П", F.H0, Color.FromArgb(11, 13, 18), logo,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            D.Text(g, "Полка", F.H0, C.Text, new Rectangle(contentLeft + 47, 20, 220, 22),
                TextFormatFlags.Left | TextFormatFlags.Top);
            D.Text(g, "что посмотреть и почитать", F.Small, C.Muted,
                new Rectangle(contentLeft + 47, 43, 240, 18),
                TextFormatFlags.Left | TextFormatFlags.Top);
        }

        void DrawHero(Graphics g)
        {
            int total = notes.Count;
            int done = 0, doing = 0;
            foreach (var n in notes)
            {
                if (n.Status == "done") done++;
                else if (n.Status == "doing") doing++;
            }

            string sub = total == 0
                ? "Фильмы, сериалы, книги и игры — в одном списке."
                : total + " " + Meta.Plural(total, "заметка", "заметки", "заметок") +
                  (doing > 0 ? " · " + doing + " в процессе" : "") +
                  (done > 0 ? " · " + done + " готово" : "");
            D.Text(g, sub, F.Body, C.Muted, new Rectangle(contentLeft, 150, 620, 22),
                TextFormatFlags.Left | TextFormatFlags.Top);

            using (var br = new LinearGradientBrush(new Rectangle(contentLeft, 92, 580, 54),
                Color.White, Color.FromArgb(214, 188, 255), 0f))
            {
                g.DrawString("Что посмотреть дальше?", F.H1, br, contentLeft, 92);
            }

            int pw = 300;
            int px = contentLeft + contentW - pw;
            int py = 98;
            int pct = total == 0 ? 0 : (int)Math.Round(done * 100.0 / total);

            D.Text(g, done + " из " + total + " готово", F.Small, C.Muted,
                new Rectangle(px, py, pw - 60, 18), TextFormatFlags.Left | TextFormatFlags.Top);
            D.Text(g, pct + "%", F.Small, C.Muted,
                new Rectangle(px + pw - 50, py, 50, 18), TextFormatFlags.Right | TextFormatFlags.Top);

            var track = new Rectangle(px, py + 26, pw, 6);
            D.FillRound(g, track, 3, Color.FromArgb(24, 28, 39));
            if (pct > 0)
            {
                var fill = new Rectangle(px, py + 26, Math.Max(6, pw * pct / 100), 6);
                using (var br = new LinearGradientBrush(fill, C.Accent, C.Accent2, 0f))
                using (var path = D.Round(fill, 3))
                    g.FillPath(br, path);
            }
        }

        void DrawCounter(Graphics g)
        {
            string text = "Показано " + visibleCount + " из " + notes.Count;
            D.Text(g, text, F.Small, C.Muted2,
                new Rectangle(diceBtn.Left - 210, chipsTop - 1, 200, 34),
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
        }
    }
}
