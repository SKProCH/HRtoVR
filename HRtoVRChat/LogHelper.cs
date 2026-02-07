using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace HRtoVRChat;

public static class LogHelper {
    public static readonly List<string> AllLogs = new();

    private static string time {
        get => DateTime.Now.ToString(CultureInfo.CurrentCulture).Split(' ')[1];
    }

    public static event Action<string, LogLevel> OnLog;

    public enum LogLevel {
        Debug,
        Log,
        Warn,
        Error
    }

    public static void Debug(object obj) {
        var frame = new StackFrame(1);
        var msg = $"[{time}] [{frame.GetMethod()?.DeclaringType}:{frame.GetMethod()}] (DEBUG): {obj}";
        #if DEBUG
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine(msg);
        Console.ForegroundColor = ConsoleColor.White;
        #endif
        AllLogs.Add(msg);
        OnLog?.Invoke(msg, LogLevel.Debug);
    }

    public static void Log(object obj, ConsoleColor color = ConsoleColor.White) {
        var frame = new StackFrame(1);
        var msg = $"[{time}] [{frame.GetMethod()?.DeclaringType}] (LOG): {obj}";
        Console.ForegroundColor = color;
        Console.WriteLine(msg);
        Console.ForegroundColor = ConsoleColor.White;
        AllLogs.Add(msg);
        OnLog?.Invoke(msg, LogLevel.Log);
    }

    public static void Warn(object obj) {
        var frame = new StackFrame(1);
        var msg = $"[{time}] [{frame.GetMethod()?.DeclaringType}] (WARN): {obj}";
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(msg);
        Console.ForegroundColor = ConsoleColor.White;
        AllLogs.Add(msg);
        OnLog?.Invoke(msg, LogLevel.Warn);
    }

    public static void Error(object obj, Exception e = null) {
        var frame = new StackFrame(1);
        object log = $"[{time}] [{frame.GetMethod()?.DeclaringType}:{frame.GetMethod()}] (ERROR): {obj}";
        if (e != null)
            log = $"{log} | Exception: {e}";
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(log);
        Console.ForegroundColor = ConsoleColor.White;
        AllLogs.Add((string)log);
        OnLog?.Invoke((string)log, LogLevel.Error);
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