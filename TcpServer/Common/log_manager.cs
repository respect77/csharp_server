using System.Diagnostics;
using System.Threading.Channels;

namespace TcpServer.Common
{
    public class LogManager
    {
        public enum LogLevel
        {
            Debug,
            Info,
            Warning,
            Error,
            Fatal
        }

        private static readonly Lazy<LogManager> _instance = new Lazy<LogManager>(() => new LogManager());
        private readonly StreamWriter _writer;
        private readonly Channel<string> _logMessageChannel = Channel.CreateUnbounded<string>();
        private readonly CancellationTokenSource _cts = new();
        public static LogManager Instance => _instance.Value;
        private LogManager()
        {
            var _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
            var now = DateTime.Now;
            var yearDirectory = Path.Combine(_logDirectory, now.ToString("yyyy"));
            var monthDirectory = Path.Combine(yearDirectory, now.ToString("MM"));

            // File name with year-month-day-hour.minute.second.millisecond.log
            var _logFilePath = Path.Combine(monthDirectory, now.ToString("yyyy-MM-dd-HH.mm.ss.fff") + ".log");

            // Create directories if they do not exist
            if (!Directory.Exists(yearDirectory))
            {
                Directory.CreateDirectory(yearDirectory);
            }
            if (!Directory.Exists(monthDirectory))
            {
                Directory.CreateDirectory(monthDirectory);
            }

            _writer = new StreamWriter(_logFilePath, append: true) { AutoFlush = true };
            Schedule(_cts.Token);
        }

        private async void Schedule(CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var logMessage in _logMessageChannel.Reader.ReadAllAsync(cancellationToken))
                {
                    Console.WriteLine(logMessage);
                    try
                    {
                        await _writer.WriteLineAsync(logMessage);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Logging error: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        public void Debug(string message, string userId = "")
        {
            LogExec(LogLevel.Debug, userId, message);
        }
        public void Info(string message, string userId = "")
        {
            LogExec(LogLevel.Info, userId, message);
        }
        public void Warning(string message, string userId = "")
        {
            LogExec(LogLevel.Warning, userId, message);
        }
        public void Error(string message, string userId = "")
        {
            LogExec(LogLevel.Error, userId, message);
        }

        private void LogExec(LogLevel level, string userId, string message)
        {

            if (userId != "")
            {
                userId = $"[{userId}]";
            }
#if DEBUG
            string fileInfo = "";
            try
            {
                StackTrace stackTrace = new StackTrace(true);
                StackFrame? stackFrame = stackTrace.GetFrame(2);
                string fullPath = stackFrame?.GetFileName() ?? "null";
                var fileName = Path.GetFileName(fullPath);
                var fileLine = stackFrame?.GetFileLineNumber() ?? -1;
                fileInfo = $"{fileName}:{fileLine}";
            }
            finally
            {
                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}][{fileInfo}][{level}]{userId} - {message}";
                _logMessageChannel.Writer.TryWrite(logMessage);
            }

#else
            string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}][{level}]{userId} - {message}";
            _logMessageChannel.Writer.TryWrite(logMessage);
#endif
        }
    }
}
