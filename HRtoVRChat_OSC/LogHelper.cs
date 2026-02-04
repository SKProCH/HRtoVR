using System.Diagnostics;
using System.Globalization;

namespace HRtoVRChat_OSC;

public static class LogHelper {
    public static readonly List<string> AllLogs = new();

    private static string time {
        get => DateTime.Now.ToString(CultureInfo.CurrentCulture).Split(' ')[1];
    }

    public static void Debug(object obj) {
        var frame = new StackFrame(1);
        Console.ForegroundColor = ConsoleColor.Gray;
        var msg = $"[{time}] [{frame.GetMethod()?.DeclaringType}:{frame.GetMethod()}] (DEBUG): {obj}";
        Console.WriteLine(msg);
        Console.ForegroundColor = ConsoleColor.White;
        AllLogs.Add(msg);
    }

    public static void Log(object obj, ConsoleColor color = ConsoleColor.White) {
        var frame = new StackFrame(1);
        Console.ForegroundColor = color;
        var msg = $"[{time}] [{frame.GetMethod()?.DeclaringType}] (LOG): {obj}";
        Console.WriteLine(msg);
        Console.ForegroundColor = ConsoleColor.White;
        AllLogs.Add(msg);
    }

    public static void Warn(object obj) {
        var frame = new StackFrame(1);
        Console.ForegroundColor = ConsoleColor.Yellow;
        var msg = $"[{time}] [{frame.GetMethod()?.DeclaringType}] (WARN): {obj}";
        Console.WriteLine(msg);
        Console.ForegroundColor = ConsoleColor.White;
        AllLogs.Add(msg);
    }

    public static void Error(object obj, Exception e = null) {
        var frame = new StackFrame(1);
        Console.ForegroundColor = ConsoleColor.Red;
        object log = $"[{time}] [{frame.GetMethod()?.DeclaringType}:{frame.GetMethod()}] (ERROR): {obj}";
        if (e != null)
            log = $"{log} | Exception: {e}";
        Console.WriteLine(log);
        Console.ForegroundColor = ConsoleColor.White;
        AllLogs.Add((string)log);
    }

    public static void SaveToFile(string filename) {
        if (!Directory.Exists("Logs"))
            Directory.CreateDirectory("Logs");
        var fileContent = string.Empty;
        foreach (var allLog in AllLogs)
            fileContent += allLog + "\n";
        Debug("Writing Logs to file");
        File.WriteAllText(Path.Combine("Logs", filename + ".txt"), fileContent);
    }
}