using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using Gml.Launcher.Core.Exceptions;
using Sentry;
using Splat;

namespace Gml.Launcher.Core.Services
{
    public class LogHandler
    {
        private readonly Subject<string> _errorDataSubject = new();
        private readonly Queue<string> _recentLogs = new();
        private readonly List<string> _logBuffer = new();
        private static readonly string[] LogLevels = { "[INFO]", "[WARNING]", "[ERROR]", "[DEBUG]" };
        
        private readonly ILoggingService? _loggingService;
        
#if DEBUG
        private readonly DevFileLogger? _devLogger;
#endif
        
        public Queue<string> RecentLogs => _recentLogs;
        public List<string> LogBuffer => _logBuffer;

        public LogHandler()
        {
            // Получаем сервис логирования из контейнера
            _loggingService = Locator.Current.GetService<ILoggingService>();
            
#if DEBUG
            _devLogger = new DevFileLogger();
            _devLogger.WriteLog("LogHandler initialized in DEBUG mode", DevFileLogger.LogLevel.INFO);
#endif
            
            _loggingService?.LogLauncherUpdate("LogHandler initialized", 
                $"Build mode: {(_loggingService != null ? "Logging enabled" : "Logging disabled")}");
            
            _errorDataSubject
                .Where(data => !string.IsNullOrEmpty(data)
                               && !data.Contains("[gml-patch]", StringComparison.OrdinalIgnoreCase)
                               && !data.Contains("/api/v1/integrations/texture/skins/", StringComparison.OrdinalIgnoreCase)
                               && !data.Contains("/api/v1/integrations/texture/capes/", StringComparison.OrdinalIgnoreCase)
                               && !data.Contains("level=\"INFO\"", StringComparison.OrdinalIgnoreCase)
                               && !data.Contains("[INFO]", StringComparison.OrdinalIgnoreCase)
                               )
                .Subscribe(ProcessLogLine);

            Observable.Interval(TimeSpan.FromSeconds(1))
                .Subscribe(_ => FlushLogBuffer());
        }

        private void ProcessLogLine(string logLine)
        {
#if DEBUG
            // Log all lines to dev log for comprehensive debugging
            _devLogger?.WriteLog($"Game Log: {logLine}", DevFileLogger.LogLevel.DEBUG);
#endif
            
            // Логируем все линии игрового лога для отладки
            _loggingService?.LogLauncherUpdate("Game Log", logLine);
            
            if (LogLevels.Any(level => logLine.Contains(level, StringComparison.OrdinalIgnoreCase)))
            {
                FlushLogBuffer();
            }

            _recentLogs.Enqueue(logLine);

            if (_recentLogs.Count > 30)
            {
                _recentLogs.Dequeue();
            }

            _logBuffer.Add(logLine);
        }

        private void FlushLogBuffer()
        {
            if (_logBuffer.Count > 0)
            {
                HandleErrorData(string.Join(Environment.NewLine, _logBuffer));
                _logBuffer.Clear();
            }
        }

        private void HandleErrorData(string data)
        {
            Debug.WriteLine(data);

            try
            {
                if (IsErrorLog(data))
                {
                    var exception = data.Contains("java.")
                        ? ExtractJavaException(data)
                        : ExtractException(data);

                    _loggingService?.LogException(exception, "Game Error Log");
                    ShowError(ResourceKeysDictionary.GameProfileError, data);
                    SentrySdk.CaptureException(exception);
                }
                else if (data.Contains("Exception", StringComparison.OrdinalIgnoreCase))
                {
                    var exception = new MinecraftException(data);
                    _loggingService?.LogException(exception, "Game Exception");
                    ShowError(ResourceKeysDictionary.GameProfileError, data);
                    SentrySdk.CaptureException(exception);
                }
            }
            catch (Exception exception)
            {
                _loggingService?.LogException(exception, "Error processing game log");
                SentrySdk.CaptureException(exception);
            }
        }

        private bool IsErrorLog(string data)
        {
            var errorLogRegex = new Regex(@"\blevel=""ERROR""\b", RegexOptions.Singleline);
            var throwableRegex = new Regex(@"<log4j:Throwable><!\[CDATA\[(.*?)\]\]></log4j:Throwable>", RegexOptions.Singleline);

            return errorLogRegex.IsMatch(data) || throwableRegex.IsMatch(data) ||
                   (data.Contains("Exception", StringComparison.OrdinalIgnoreCase) &&
                    !data.Contains("INFO", StringComparison.OrdinalIgnoreCase) &&
                    !data.Contains("WARN", StringComparison.OrdinalIgnoreCase));
        }

        private Exception ExtractException(string data)
        {
            var throwableRegex = new Regex(@"<log4j:Throwable><!\[CDATA\[(.*?)\]\]></log4j:Throwable>", RegexOptions.Singleline);
            var match = throwableRegex.Match(data);

            if (match.Success)
            {
                var stackTrace = match.Groups[1].Value;
                return new MinecraftException(stackTrace);
            }

            return new MinecraftException(data);
        }

        private Exception ExtractJavaException(string data)
        {
            var javaExceptionRegex = new Regex(@"(java\.(.*?)\.)Exception: (.*?)\n", RegexOptions.Singleline);
            var match = javaExceptionRegex.Match(data);

            if (match.Success)
            {
                var exceptionMessage = match.Groups[3].Value;
                return new MinecraftException(exceptionMessage);
            }

            return new MinecraftException(data);
        }

        private void ShowError(string key, string message)
        {
        }

        private static class ResourceKeysDictionary
        {
            public static string GameProfileError = "GameProfileError";
        }

        public void ProcessLogs(string data)
        {
#if DEBUG
            // Check if we're filtering an INFO message
            if (data.Contains("level=\"INFO\"", StringComparison.OrdinalIgnoreCase) || 
                data.Contains("[INFO]", StringComparison.OrdinalIgnoreCase))
            {
                _devLogger?.WriteLog($"Filtered INFO message: {data.Substring(0, Math.Min(100, data.Length))}...", 
                    DevFileLogger.LogLevel.DEBUG);
            }
#endif
            
            // Проверяем, фильтруем ли мы INFO сообщение
            if (data.Contains("level=\"INFO\"", StringComparison.OrdinalIgnoreCase) || 
                data.Contains("[INFO]", StringComparison.OrdinalIgnoreCase))
            {
                _loggingService?.LogLauncherUpdate("Filtered INFO message", 
                    data.Substring(0, Math.Min(100, data.Length)) + "...");
            }
            
            _errorDataSubject.OnNext(data);
        }
        
        public void LogFileOperation(string operation, string path, bool success = true, string details = "")
        {
#if DEBUG
            _devLogger?.WriteLog($"File Operation: {operation} - {path} - {(success ? "SUCCESS" : "FAILED")}", 
                DevFileLogger.LogLevel.DEBUG, details);
#endif
            _loggingService?.LogDirectoryCreation(path, success, details);
        }
        
        public void LogProfileAction(string profileName, string action, string details = "")
        {
#if DEBUG
            _devLogger?.LogProfileAction(profileName, action, details);
#endif
            _loggingService?.LogProfileAction(profileName, action, details);
        }
        
        public void LogModAction(string modName, string action, string details = "")
        {
#if DEBUG
            _devLogger?.LogModAction(modName, action, details);
#endif
            _loggingService?.LogModAction(modName, action, details);
        }
        
        public string? GetLogFilePath()
        {
#if DEBUG
            return _devLogger?.GetLogFilePath();
#else
            return _loggingService?.GetLogFilePath();
#endif
        }

#if DEBUG
        public DevFileLogger? DevLogger => _devLogger;
#endif
    }
}
