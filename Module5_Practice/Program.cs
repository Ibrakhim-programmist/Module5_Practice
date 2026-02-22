using System.Text;
using System.Text.Json;


// ===== SINGLETON =========

public enum LogLevel { INFO = 1, WARNING = 2, ERROR = 3 }

public class LoggerConfig
{
    public string FilePath { get; set; }
    public string LogLevel { get; set; }
}

public sealed class Logger
{
    private static readonly Lazy<Logger> _instance =
        new Lazy<Logger>(() => new Logger());

    private readonly object _lock = new();
    private string _path;
    private LogLevel _level;
    private const long MaxSize = 1024 * 1024;

    private Logger() => LoadConfig("loggerconfig.json");

    public static Logger GetInstance() => _instance.Value;

    public void SetLogLevel(LogLevel level) => _level = level;

    public void Log(string msg, LogLevel level)
    {
        if (level < _level) return;

        lock (_lock)
        {
            if (File.Exists(_path) && new FileInfo(_path).Length >= MaxSize)
                File.Move(_path, $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

            string line = $"{DateTime.Now} [{level}] {msg}";
            File.AppendAllText(_path, line + Environment.NewLine);
            Console.WriteLine(line);
        }
    }

    private void LoadConfig(string file)
    {
        if (!File.Exists(file))
        {
            _path = "app.log";
            _level = LogLevel.INFO;
            return;
        }

        var cfg = JsonSerializer.Deserialize<LoggerConfig>(File.ReadAllText(file));
        _path = cfg.FilePath;
        _level = Enum.Parse<LogLevel>(cfg.LogLevel);
    }
}

public class LogReader
{
    private readonly string _path;
    public LogReader(string path) => _path = path;

    public void Read(LogLevel level)
    {
        if (!File.Exists(_path)) return;
        foreach (var l in File.ReadAllLines(_path))
            if (l.Contains($"[{level}]"))
                Console.WriteLine(l);
    }
}

// ===================== BUILDER ===============

public class ReportStyle
{
    public string BackgroundColor { get; set; }
    public string FontColor { get; set; }
    public int FontSize { get; set; }
}

public class Report
{
    public string Header, Content, Footer;
    public List<string> Sections = new();
    public ReportStyle Style;

    public string Export()
    {
        var sb = new StringBuilder();
        sb.AppendLine(Header).AppendLine(Content);
        Sections.ForEach(s => sb.AppendLine(s));
        sb.AppendLine(Footer);
        return sb.ToString();
    }
}

public interface IReportBuilder
{
    void SetHeader(string h);
    void SetContent(string c);
    void SetFooter(string f);
    void AddSection(string n, string c);
    void SetStyle(ReportStyle s);
    Report GetReport();
}

public class TextReportBuilder : IReportBuilder
{
    private Report r = new();

    public void SetHeader(string h) => r.Header = h;
    public void SetContent(string c) => r.Content = c;
    public void SetFooter(string f) => r.Footer = f;
    public void AddSection(string n, string c) => r.Sections.Add($"{n}: {c}");
    public void SetStyle(ReportStyle s) => r.Style = s;
    public Report GetReport() => r;
}

public class ReportDirector
{
    public void Construct(IReportBuilder b, ReportStyle s)
    {
        b.SetHeader("=== ОТЧЕТ ===");
        b.SetContent("Основное содержание");
        b.AddSection("Раздел 1", "Данные");
        b.SetFooter("=== КОНЕЦ ===");
        b.SetStyle(s);
    }
}

// ======== PROTOTYPE =====================

public class Skill : ICloneable
{
    public string Name;
    public int Power;
    public object Clone() => new Skill { Name = Name, Power = Power };
}

public class Weapon : ICloneable
{
    public string Name;
    public int Damage;
    public object Clone() => new Weapon { Name = Name, Damage = Damage };
}

public class Armor : ICloneable
{
    public string Name;
    public int Defense;
    public object Clone() => new Armor { Name = Name, Defense = Defense };
}

public class Character : ICloneable
{
    public int Health, Strength, Agility, Intelligence;
    public Weapon Weapon;
    public Armor Armor;
    public List<Skill> Skills = new();

    public object Clone()
    {
        var c = (Character)MemberwiseClone();
        c.Weapon = (Weapon)Weapon.Clone();
        c.Armor = (Armor)Armor.Clone();
        c.Skills = new();
        Skills.ForEach(s => c.Skills.Add((Skill)s.Clone()));
        return c;
    }
}

// MAIN

class Program
{
    static void Main()
    {
        // --- Singleton test ---
        var logger = Logger.GetInstance();

        Thread t1 = new(() =>
        {
            for (int i = 0; i < 5; i++)
                logger.Log($"Info {i}", LogLevel.INFO);
        });

        Thread t2 = new(() =>
        {
            for (int i = 0; i < 5; i++)
                logger.Log($"Error {i}", LogLevel.ERROR);
        });

        t1.Start(); t2.Start();
        t1.Join(); t2.Join();

        // ---- Builder test ----
        var builder = new TextReportBuilder();
        new ReportDirector().Construct(builder,
            new ReportStyle { BackgroundColor = "White", FontColor = "Black", FontSize = 14 });

        Console.WriteLine(builder.GetReport().Export());

        // -Prototype test -
        var hero = new Character
        {
            Health = 100,
            Weapon = new Weapon { Name = "Sword", Damage = 20 },
            Armor = new Armor { Name = "Plate", Defense = 10 }
        };

        hero.Skills.Add(new Skill { Name = "Slash", Power = 15 });

        var clone = (Character)hero.Clone();
        clone.Health = 200;

        Console.WriteLine($"Original HP: {hero.Health}");
        Console.WriteLine($"Clone HP: {clone.Health}");
    }
}
//В работе сделали Singleton, Builder и Prototype. Логгер потокобезопасный, бәрі норм жұмыс істейді,конфиг оқиды, файл айналдырады.
//Отчет Director арқылы жиналады.
//Персонаждар deep clone жасайды, баг жоқ, бәрі четко.
