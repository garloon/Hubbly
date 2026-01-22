using System.Diagnostics;
using System.Text;

namespace Hubbly.Mobile.Utils;

public static class DebugLogger
{
    private const string PREFIX = "[HUBBLY] ";
    private static readonly StringBuilder _logBuffer = new();
    private static readonly string _logFilePath;
    private static readonly object _fileLock = new();

    static DebugLogger()
    {
        // Определяем путь для файла логов
        _logFilePath = Path.Combine(
            FileSystem.AppDataDirectory,
            "logs",
            $"hubbly_{DateTime.Now:yyyyMMdd_HHmmss}.log");

        // Создаем папку если нужно
        var logDir = Path.GetDirectoryName(_logFilePath);
        if (!Directory.Exists(logDir))
            Directory.CreateDirectory(logDir);
    }

    public static void Log(string message, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
    {
        var fullMessage = $"{PREFIX}{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{caller}] {message}";

        // В Debug output
        Debug.WriteLine(fullMessage);

        // В консоль
        Console.WriteLine(fullMessage);

        // В буфер для отображения
        lock (_logBuffer)
        {
            _logBuffer.AppendLine(fullMessage);
            if (_logBuffer.Length > 10000)
            {
                _logBuffer.Remove(0, _logBuffer.Length - 5000);
            }
        }

        // В файл (асинхронно чтобы не блокировать UI)
        Task.Run(() => WriteToFile(fullMessage));
    }

    private static void WriteToFile(string message)
    {
        lock (_fileLock)
        {
            try
            {
                // Режим добавления в конец файла
                using var writer = new StreamWriter(_logFilePath, true, Encoding.UTF8);
                writer.WriteLine(message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to write log to file: {ex.Message}");
            }
        }
    }

    public static string GetLogs()
    {
        lock (_logBuffer)
        {
            return _logBuffer.ToString();
        }
    }

    public static async Task<string> GetLogsFromFileAsync(int maxLines = 100)
    {
        try
        {
            if (!File.Exists(_logFilePath))
                return "Log file not found";

            var lines = await File.ReadAllLinesAsync(_logFilePath);
            return string.Join(Environment.NewLine, lines.TakeLast(maxLines));
        }
        catch (Exception ex)
        {
            return $"Error reading log file: {ex.Message}";
        }
    }

    public static async Task<List<string>> GetAllLogFilesAsync()
    {
        try
        {
            var logDir = Path.GetDirectoryName(_logFilePath);
            if (!Directory.Exists(logDir))
                return new List<string>();

            var files = Directory.GetFiles(logDir, "hubbly_*.log")
                .OrderByDescending(f => f)
                .ToList();

            return files;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error listing log files: {ex.Message}");
            return new List<string>();
        }
    }

    public static void Clear()
    {
        lock (_logBuffer)
        {
            _logBuffer.Clear();
        }
    }

    // Очистка старых логов (старше 7 дней)
    public static async Task CleanupOldLogsAsync(int daysToKeep = 7)
    {
        try
        {
            var logDir = Path.GetDirectoryName(_logFilePath);
            if (!Directory.Exists(logDir))
                return;

            var cutoff = DateTime.Now.AddDays(-daysToKeep);

            foreach (var file in Directory.GetFiles(logDir, "hubbly_*.log"))
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.CreationTime < cutoff)
                {
                    File.Delete(file);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error cleaning up old logs: {ex.Message}");
        }
    }
}