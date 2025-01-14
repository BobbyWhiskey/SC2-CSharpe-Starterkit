﻿// ReSharper disable AssignNullToNotNullAttribute

namespace Bot;

public static class Logger
{
    private static string? _logFile;
    private static bool _stdoutClosed;

    private static void Initialize()
    {
        _logFile = "Logs/" + DateTime.UtcNow.ToString("yyyy-MM-dd HH.mm.ss") + ".log";
        var path = Path.GetDirectoryName(_logFile);
        if (path != null)
        {
            Directory.CreateDirectory(path);
        }
    }


    private static void WriteLine(string type, string line, params object?[] parameters)
    {
        if (_logFile == null)
        {
            Initialize();
        }

        if (_logFile == null)
        {
            throw new Exception("Could not get logfile");
        }

        var msg = "[" + DateTime.UtcNow.ToString("HH:mm:ss") + " " + type + "] " + string.Format(line, parameters);

        var file = new StreamWriter(_logFile, true);
        file.WriteLine(msg);
        file.Close();
        // do not write to stdout if it is closed (LadderServer on linux)
        if (!_stdoutClosed)
        {
            try
            {
                Console.WriteLine(msg, parameters);
            }
            catch
            {
                _stdoutClosed = true;
            }
        }
    }

    public static void Info(string line, params object?[] parameters)
    {
        WriteLine("INFO", line, parameters);
    }

    public static void Warning(string line, params object?[] parameters)
    {
        WriteLine("WARNING", line, parameters);
    }

    public static void Error(string line, params object?[] parameters)
    {
        WriteLine("ERROR", line, parameters);
    }
}