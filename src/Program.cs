using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Windows.Forms;

[assembly: AssemblyTitle("Полка")]
[assembly: AssemblyDescription("Заметки о фильмах, сериалах, книгах и играх")]
[assembly: AssemblyProduct("Полка")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace Polka
{
    public class Note
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public int Rating { get; set; }
        public string Year { get; set; }
        public List<string> Tags { get; set; }
        public string Text { get; set; }
        public string Poster { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }

        public Note()
        {
            Id = Guid.NewGuid().ToString("N");
            Title = "";
            Type = "movie";
            Status = "want";
            Year = "";
            Tags = new List<string>();
            Text = "";
            Poster = "";
            Created = DateTime.Now;
            Updated = DateTime.Now;
        }

        public Note Clone()
        {
            var n = (Note)MemberwiseClone();
            n.Tags = new List<string>(Tags == null ? new string[0] : Tags.ToArray());
            return n;
        }
    }

    static class Store
    {
        public static readonly string Dir = ResolveDir();
        public static readonly string FilePath = Path.Combine(Dir, "notes.json");

        static string ResolveDir()
        {
            string custom = Environment.GetEnvironmentVariable("POLKA_DATA_DIR");
            if (!string.IsNullOrEmpty(custom)) return custom;
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Polka");
        }

        public static List<Note> Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return null;
                string json = File.ReadAllText(FilePath);
                var ser = new JavaScriptSerializer();
                var list = ser.Deserialize<List<Note>>(json);
                if (list == null) return null;
                foreach (var n in list)
                {
                    if (n.Title == null) n.Title = "";
                    if (n.Tags == null) n.Tags = new List<string>();
                    if (n.Type == null) n.Type = "movie";
                    if (n.Status == null) n.Status = "want";
                    if (n.Year == null) n.Year = "";
                    if (n.Text == null) n.Text = "";
                    if (n.Poster == null) n.Poster = "";
                    if (n.Id == null) n.Id = Guid.NewGuid().ToString("N");
                }
                return list;
            }
            catch
            {
                try
                {
                    if (File.Exists(FilePath))
                        File.Copy(FilePath, FilePath + ".bak", true);
                }
                catch { }
                return null;
            }
        }

        public static void Save(List<Note> notes)
        {
            try
            {
                if (!Directory.Exists(Dir)) Directory.CreateDirectory(Dir);
                var ser = new JavaScriptSerializer();
                string json = ser.Serialize(notes);
                File.WriteAllText(FilePath, json);
            }
            catch { }
        }

        public static List<Note> Seed()
        {
            var list = new List<Note>();
            DateTime now = DateTime.Now;

            var a = new Note();
            a.Title = "Дюна: Часть вторая"; a.Type = "movie"; a.Status = "done"; a.Rating = 5; a.Year = "2024";
            a.Tags.AddRange(new string[] { "фантастика", "эпик" });
            a.Text = "Песчаные черви, политика и очень большой экран. Пересмотреть в IMAX.";
            a.Created = now.AddDays(-2);
            list.Add(a);

            var b = new Note();
            b.Title = "Оппенгеймер"; b.Type = "movie"; b.Status = "done"; b.Rating = 4; b.Year = "2023";
            b.Tags.AddRange(new string[] { "биография", "драма" });
            b.Text = "Три часа, но держит. Смотреть вечером, когда никто не дёргает.";
            b.Created = now.AddDays(-5);
            list.Add(b);

            var c = new Note();
            c.Title = "Разделение"; c.Type = "series"; c.Status = "doing"; c.Year = "2022";
            c.Tags.Add("триллер");
            c.Text = "Первый сезон раскачивается медленно, но затягивает.";
            c.Created = now.AddDays(-1);
            list.Add(c);

            var d = new Note();
            d.Title = "Мастер и Маргарита"; d.Type = "book"; d.Year = "1967";
            d.Tags.Add("классика");
            d.Text = "Хочу в бумаге, с закладками и без спешки.";
            d.Created = now.AddDays(-3);
            list.Add(d);

            var e = new Note();
            e.Title = "Baldur's Gate 3"; e.Type = "game"; e.Year = "2023";
            e.Tags.Add("rpg"); e.Tags.Add("кооп");
            e.Text = "Собрать компанию на троих и пройти хотя бы первый акт.";
            e.Created = now.AddDays(-4);
            list.Add(e);

            var f = new Note();
            f.Title = "Купить попкорн"; f.Type = "other";
            f.Tags.Add("бытовое");
            f.Text = "К пятничному просмотру.";
            f.Created = now.AddHours(-6);
            list.Add(f);

            return list;
        }
    }

    static class Meta
    {
        public static readonly string[] Types = { "movie", "series", "book", "game", "other" };
        public static readonly string[] Statuses = { "want", "doing", "done" };

        public static string TypeLabel(string t)
        {
            switch (t)
            {
                case "movie": return "Фильм";
                case "series": return "Сериал";
                case "book": return "Книга";
                case "game": return "Игра";
                default: return "Другое";
            }
        }

        public static string TypePlural(string t)
        {
            switch (t)
            {
                case "movie": return "Фильмы";
                case "series": return "Сериалы";
                case "book": return "Книги";
                case "game": return "Игры";
                default: return "Другое";
            }
        }

        public static string TypeIcon(string t)
        {
            switch (t)
            {
                case "movie": return "film";
                case "series": return "tv";
                case "book": return "book";
                case "game": return "game";
                default: return "bookmark";
            }
        }

        public static Color TypeColor(string t)
        {
            switch (t)
            {
                case "movie": return Color.FromArgb(165, 175, 255);
                case "series": return Color.FromArgb(124, 199, 255);
                case "book": return Color.FromArgb(255, 196, 107);
                case "game": return Color.FromArgb(110, 231, 168);
                default: return Color.FromArgb(244, 154, 193);
            }
        }

        public static string StatusLabel(string s)
        {
            switch (s)
            {
                case "doing": return "В процессе";
                case "done": return "Готово";
                default: return "Хочу";
            }
        }

        public static string StatusNext(string s)
        {
            switch (s)
            {
                case "want": return "doing";
                case "doing": return "done";
                default: return "want";
            }
        }

        public static Color StatusFg(string s)
        {
            switch (s)
            {
                case "doing": return Color.FromArgb(255, 217, 160);
                case "done": return Color.FromArgb(166, 243, 198);
                default: return Color.FromArgb(195, 202, 255);
            }
        }

        public static Color StatusBorder(string s)
        {
            switch (s)
            {
                case "doing": return Color.FromArgb(120, 245, 194, 91);
                case "done": return Color.FromArgb(110, 74, 222, 128);
                default: return Color.FromArgb(120, 139, 150, 255);
            }
        }

        public static string Plural(int n, string one, string few, string many)
        {
            int m10 = n % 10, m100 = n % 100;
            if (m10 == 1 && m100 != 11) return one;
            if (m10 >= 2 && m10 <= 4 && (m100 < 12 || m100 > 14)) return few;
            return many;
        }

        public static Color[] Cover(string title)
        {
            int[] hues = { 232, 268, 196, 32, 330, 158 };
            int hash = 0;
            foreach (char ch in title) hash = (hash * 31 + ch) % 9973;
            int h = hues[Math.Abs(hash) % hues.Length];
            return new Color[] { D.Hsl(h, 0.42, 0.32), D.Hsl(h + 20, 0.40, 0.13) };
        }
    }

    static class Program
    {
        public static string ShotPath;
        public static string ShotMode = "main";

        public static void Log(string text)
        {
            try
            {
                File.AppendAllText(Path.Combine(Path.GetTempPath(), "Polka.log"),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + text + "\r\n");
            }
            catch { }
        }

        public static void Capture(Form form, string path)
        {
            var r = form.RectangleToScreen(form.ClientRectangle);
            using (var bmp = new Bitmap(r.Width, r.Height))
            {
                using (var g = Graphics.FromImage(bmp))
                    g.CopyFromScreen(r.Location, Point.Empty, r.Size);
                bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        [STAThread]
        static void Main(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--shot" && i + 1 < args.Length) ShotPath = args[i + 1];
                if (args[i] == "--mode" && i + 1 < args.Length) ShotMode = args[i + 1];
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate(object s, System.Threading.ThreadExceptionEventArgs e)
            {
                Log("THREAD ERROR: " + e.Exception);
            };
            AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e)
            {
                Log("DOMAIN ERROR: " + e.ExceptionObject);
            };

            if (ShotMode == "selftest")
            {
                Log("selftest: dir=" + Store.Dir);
                var seed = Store.Seed();
                Store.Save(seed);
                var loaded = Store.Load();
                Log("selftest: saved=" + seed.Count + " loaded=" + (loaded == null ? -1 : loaded.Count));
                if (loaded == null) { Log("selftest: FAIL load"); return; }

                var extra = new Note();
                extra.Title = "Тестовая заметка";
                extra.Type = "series";
                extra.Status = "doing";
                extra.Rating = 3;
                extra.Tags.Add("тест");
                extra.Text = "Проверка сохранения";
                loaded.Insert(0, extra);
                Store.Save(loaded);
                var again = Store.Load();
                Log("selftest: after add loaded=" + (again == null ? -1 : again.Count) +
                    " first='" + (again != null && again.Count > 0 ? again[0].Title : "") + "'");
                Log("selftest: done");
                return;
            }

            if (ShotMode == "edittest")
            {
                var ed = new EditorForm(null);
                var r = ed.SelfTest();
                Log("edittest: title='" + r.Title + "' type=" + r.Type + " status=" + r.Status +
                    " rating=" + r.Rating + " year=" + r.Year +
                    " tags=" + string.Join("|", r.Tags.ToArray()) + " text='" + r.Text + "'");
                return;
            }

            if (ShotPath != null && (ShotMode == "editor" || ShotMode == "new"))
            {
                var data = Store.Load();
                if (data == null) data = Store.Seed();
                Note target = ShotMode == "new" ? null : (data.Count > 0 ? data[0] : null);
                var ed = new EditorForm(target);
                ed.TopMost = true;
                var shotTimer = new Timer();
                shotTimer.Interval = 1000;
                shotTimer.Tick += delegate
                {
                    shotTimer.Stop();
                    try { Capture(ed, ShotPath); }
                    catch (Exception ex) { Log("shot error: " + ex.Message); }
                    ed.Close();
                };
                ed.Shown += delegate { shotTimer.Start(); };
                Application.Run(ed);
                return;
            }

            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                Log("FATAL: " + ex);
                MessageBox.Show(ex.Message + "\n\n" + ex.StackTrace, "Полка — ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
