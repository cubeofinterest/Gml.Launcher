using System;
using System.IO;
using System.Linq;
using Gml.Launcher.Assets;

namespace Gml.Launcher.Core.Services
{
    public class FileLogger
    {
        private readonly string _logFilePath;
        private readonly object _lockObject = new object();
        private readonly bool _isDebugMode;
        
        public FileLogger(bool isDebugMode = false)
        {
            _isDebugMode = isDebugMode;
            
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var logDirectory = Path.Combine(appDataPath, ResourceKeysDictionary.FolderName, 
                _isDebugMode ? "DevLogs" : "Logs");
            
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
            
            // Удаление старых лог-файлов (старше 7 дней для release, 3 дня для debug)
            CleanupOldLogFiles(logDirectory);
            
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var logPrefix = _isDebugMode ? "launcher_dev" : "launcher";
            _logFilePath = Path.Combine(logDirectory, $"{logPrefix}_{timestamp}.log");
            
            // Write initial header
            var sessionType = _isDebugMode ? "Dev" : "Release";
            WriteLog($"=== COINT Launcher {sessionType} Session Started ===", LogLevel.INFO);
            WriteLog($"Session ID: {timestamp}", LogLevel.INFO);
            WriteLog($"Log file: {_logFilePath}", LogLevel.INFO);
            WriteLog($"Launcher version: 0.1.2.0", LogLevel.INFO);
            WriteLog($"Build mode: {(_isDebugMode ? "DEBUG" : "RELEASE")}", LogLevel.INFO);
            WriteLog("=====================================", LogLevel.INFO);
        }
        
        public enum LogLevel
        {
            INFO,
            DEBUG,
            WARNING,
            ERROR,
            FILE_CHECK,
            FILE_DOWNLOAD,
            FILE_VALIDATE,
            PROFILE_ACTION,
            MOD_ACTION,
            NETWORK_REQUEST,
            NETWORK_RESPONSE,
            CLEANUP,
            CRITICAL
        }
        
        public void WriteLog(string message, LogLevel level = LogLevel.INFO, string details = "")
        {
            try
            {
                lock (_lockObject)
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var logEntry = $"[{timestamp}] [{level}] {message}";
                    
                    if (!string.IsNullOrEmpty(details))
                    {
                        logEntry += $"\n    Details: {details}";
                    }
                    
                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to write to log file: {ex.Message}");
                
                // В случае ошибки записи в файл, попробуем записать в консоль
                try
                {
                    Console.WriteLine($"[LOG ERROR] Failed to write log: {ex.Message}");
                    Console.WriteLine($"[LOG] [{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}");
                }
                catch { /* Ignore console errors */ }
            }
        }
        
        public void LogFileCheck(string filePath, bool exists, long size = 0, string hash = "")
        {
            // В release режиме логируем только критичные проверки файлов
            if (!_isDebugMode && exists) return;
            
            var details = $"Path: {filePath}, Exists: {exists}";
            if (exists)
            {
                details += $", Size: {size} bytes";
                if (!string.IsNullOrEmpty(hash))
                {
                    details += $", Hash: {hash.Substring(0, Math.Min(8, hash.Length))}...";
                }
            }
            WriteLog("File Check", LogLevel.FILE_CHECK, details);
        }
        
        public void LogFileDownload(string url, string localPath, long size = 0, bool success = true)
        {
            var status = success ? "SUCCESS" : "FAILED";
            var urlDisplay = _isDebugMode ? url : url.Split('/').LastOrDefault() ?? "unknown";
            var details = $"URL: {urlDisplay}, Local: {Path.GetFileName(localPath)}, Size: {size} bytes, Status: {status}";
            
            var logLevel = success ? LogLevel.FILE_DOWNLOAD : LogLevel.ERROR;
            WriteLog("File Download", logLevel, details);
        }
        
        public void LogFileValidation(string filePath, string expectedHash, string actualHash, bool isValid)
        {
            // В release режиме логируем только неудачные валидации
            if (!_isDebugMode && isValid) return;
            
            var status = isValid ? "VALID" : "INVALID";
            var expectedDisplay = _isDebugMode ? expectedHash : expectedHash.Substring(0, Math.Min(8, expectedHash.Length)) + "...";
            var actualDisplay = _isDebugMode ? actualHash : actualHash.Substring(0, Math.Min(8, actualHash.Length)) + "...";
            var details = $"File: {Path.GetFileName(filePath)}, Expected: {expectedDisplay}, Actual: {actualDisplay}, Status: {status}";
            
            var logLevel = isValid ? LogLevel.FILE_VALIDATE : LogLevel.ERROR;
            WriteLog("File Validation", logLevel, details);
        }
        
        public void LogProfileAction(string profileName, string action, string details = "")
        {
            WriteLog($"Profile Action: {action} - {profileName}", LogLevel.PROFILE_ACTION, details);
        }
        
        public void LogModAction(string modName, string action, string details = "")
        {
            WriteLog($"Mod Action: {action} - {modName}", LogLevel.MOD_ACTION, details);
        }
        
        public void LogDirectoryOperation(string operation, string path, bool success = true, string error = "")
        {
            // В release режиме логируем только неудачные операции
            if (!_isDebugMode && success) return;
            
            var status = success ? "SUCCESS" : "FAILED";
            var pathDisplay = _isDebugMode ? path : Path.GetFileName(path);
            var details = $"Operation: {operation}, Path: {pathDisplay}, Status: {status}";
            if (!success && !string.IsNullOrEmpty(error))
            {
                details += $", Error: {error}";
            }
            
            var logLevel = success ? LogLevel.DEBUG : LogLevel.ERROR;
            WriteLog("Directory Operation", logLevel, details);
        }
        
        public void LogException(Exception exception, string context = "")
        {
            var details = $"Exception: {exception.GetType().Name}, Message: {exception.Message}";
            if (!string.IsNullOrEmpty(context))
            {
                details = $"Context: {context}, {details}";
            }
            
            if (_isDebugMode)
            {
                details += $"\nStackTrace: {exception.StackTrace}";
            }
            
            WriteLog("Exception", LogLevel.ERROR, details);
        }
        
        public void LogCritical(string message, string details = "")
        {
            WriteLog(message, LogLevel.CRITICAL, details);
        }
        
        public string GetLogFilePath()
        {
            return _logFilePath;
        }
        
        public void WriteSessionEnd()
        {
            var sessionType = _isDebugMode ? "Dev" : "Release";
            WriteLog($"=== COINT Launcher {sessionType} Session Ended ===", LogLevel.INFO);
        }
        
        private void CleanupOldLogFiles(string logDirectory)
        {
            try
            {
                var retentionDays = _isDebugMode ? 3 : 7; // Debug логи храним меньше
                var cutoffDate = DateTime.Now.AddDays(-retentionDays);
                
                var logPatterns = _isDebugMode 
                    ? new[] { "launcher_dev_*.log", "startup_debug_*.log", "file_validation_*.log", "api_downloads_*.log" }
                    : new[] { "launcher_*.log" };
                
                var deletedCount = 0;
                
                foreach (var pattern in logPatterns)
                {
                    var logFiles = Directory.GetFiles(logDirectory, pattern);
                    foreach (var logFile in logFiles)
                    {
                        try
                        {
                            // Пропускаем текущий лог-файл
                            if (_logFilePath != null && logFile.Equals(_logFilePath, StringComparison.OrdinalIgnoreCase))
                                continue;
                            
                            var fileInfo = new FileInfo(logFile);
                            if (fileInfo.CreationTime < cutoffDate)
                            {
                                File.Delete(logFile);
                                deletedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Cannot delete log file {logFile}: {ex.Message}");
                        }
                    }
                }
                
                if (deletedCount > 0)
                {
                    WriteLog($"Cleanup: Удалено лог-файлов старше {retentionDays} дней: {deletedCount}", LogLevel.CLEANUP);
                }
            }
            catch (Exception ex)
            {
                WriteLog($"Cleanup Error: {ex.Message}", LogLevel.ERROR);
            }
        }
        
        public void LogNetworkRequest(string method, string url, string headers = "", string body = "")
        {
            // В release режиме логируем только основную информацию о сетевых запросах
            var urlDisplay = _isDebugMode ? url : url.Split('/').LastOrDefault() ?? "unknown";
            var details = $"Method: {method}, URL: {urlDisplay}";
            
            if (_isDebugMode)
            {
                if (!string.IsNullOrEmpty(headers))
                {
                    details += $"\nHeaders: {headers}";
                }
                
                if (!string.IsNullOrEmpty(body) && body.Length <= 500)
                {
                    details += $"\nBody: {body}";
                }
                else if (!string.IsNullOrEmpty(body))
                {
                    details += $"\nBody: [Length: {body.Length} characters - truncated]";
                }
            }
            
            WriteLog("Network Request", LogLevel.NETWORK_REQUEST, details);
        }
        
        public void LogNetworkResponse(string url, int statusCode, string responseBody = "", long responseTime = 0)
        {
            var urlDisplay = _isDebugMode ? url : url.Split('/').LastOrDefault() ?? "unknown";
            var details = $"URL: {urlDisplay}, Status: {statusCode}";
            
            if (responseTime > 0)
            {
                details += $", Time: {responseTime}ms";
            }
            
            if (_isDebugMode)
            {
                if (!string.IsNullOrEmpty(responseBody) && responseBody.Length <= 1000)
                {
                    details += $"\nResponse: {responseBody}";
                }
                else if (!string.IsNullOrEmpty(responseBody))
                {
                    details += $"\nResponse: [Length: {responseBody.Length} characters - truncated]";
                }
            }
            
            // В release логируем только ошибочные ответы
            var shouldLog = _isDebugMode || statusCode >= 400;
            if (shouldLog)
            {
                var logLevel = statusCode >= 400 ? LogLevel.ERROR : LogLevel.NETWORK_RESPONSE;
                WriteLog("Network Response", logLevel, details);
            }
        }
        
        public void LogNetworkError(string url, string error, string method = "")
        {
            var urlDisplay = _isDebugMode ? url : url.Split('/').LastOrDefault() ?? "unknown";
            var details = $"URL: {urlDisplay}, Error: {error}";
            if (!string.IsNullOrEmpty(method))
            {
                details = $"Method: {method}, " + details;
            }
            
            WriteLog("Network Error", LogLevel.ERROR, details);
        }
    }
}
