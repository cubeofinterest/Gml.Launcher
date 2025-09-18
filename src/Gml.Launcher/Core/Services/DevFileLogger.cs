using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Gml.Launcher.Assets;

namespace Gml.Launcher.Core.Services
{
    public class DevFileLogger
    {
        private readonly string _logFilePath;
        private readonly object _lockObject = new object();
        
        public DevFileLogger()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var logDirectory = Path.Combine(appDataPath, ResourceKeysDictionary.FolderName, "DevLogs");
            
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
            
            // Удаление старых лог-файлов (старше 7 дней)
            CleanupOldLogFiles(logDirectory);
            
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            _logFilePath = Path.Combine(logDirectory, $"launcher_dev_{timestamp}.log");
            
            // Write initial header
            WriteLog("=== COINT Launcher Dev Session Started ===", LogLevel.INFO);
            WriteLog($"Session ID: {timestamp}", LogLevel.INFO);
            WriteLog($"Log file: {_logFilePath}", LogLevel.INFO);
            WriteLog($"Launcher version: 0.1.2.0", LogLevel.INFO);
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
            CLEANUP
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
            }
        }
        
        public void LogFileCheck(string filePath, bool exists, long size = 0, string hash = "")
        {
            var details = $"Path: {filePath}, Exists: {exists}";
            if (exists)
            {
                details += $", Size: {size} bytes";
                if (!string.IsNullOrEmpty(hash))
                {
                    details += $", Hash: {hash}";
                }
            }
            WriteLog("File Check", LogLevel.FILE_CHECK, details);
        }
        
        public void LogFileDownload(string url, string localPath, long size = 0, bool success = true)
        {
            var status = success ? "SUCCESS" : "FAILED";
            var details = $"URL: {url}, Local: {localPath}, Size: {size} bytes, Status: {status}";
            WriteLog("File Download", LogLevel.FILE_DOWNLOAD, details);
        }
        
        public void LogFileValidation(string filePath, string expectedHash, string actualHash, bool isValid)
        {
            var status = isValid ? "VALID" : "INVALID";
            var details = $"File: {filePath}, Expected: {expectedHash}, Actual: {actualHash}, Status: {status}";
            WriteLog("File Validation", LogLevel.FILE_VALIDATE, details);
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
            var status = success ? "SUCCESS" : "FAILED";
            var details = $"Operation: {operation}, Path: {path}, Status: {status}";
            if (!success && !string.IsNullOrEmpty(error))
            {
                details += $", Error: {error}";
            }
            WriteLog("Directory Operation", LogLevel.DEBUG, details);
        }
        
        public string GetLogFilePath()
        {
            return _logFilePath;
        }
        
        public void WriteSessionEnd()
        {
            WriteLog("=== COINT Launcher Dev Session Ended ===", LogLevel.INFO);
        }
        
        private void CleanupOldLogFiles(string logDirectory)
        {
            try
            {
                // Получаем список всех лог-файлов
                var logPatterns = new[] { "launcher_dev_*.log", "startup_debug_*.log", "file_validation_*.log", "api_downloads_*.log" };
                var deletedCount = 0;
                
                // Удаляем все файлы по каждому шаблону
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
                                
                            File.Delete(logFile);
                            deletedCount++;
                        }
                        catch (Exception ex)
                        {
                            // Если не удается удалить файл, продолжаем
                            System.Diagnostics.Debug.WriteLine($"Cannot delete log file {logFile}: {ex.Message}");
                        }
                    }
                }
                
                WriteLog($"Cleanup: Удалено лог-файлов: {deletedCount}", LogLevel.CLEANUP);
            }
            catch (Exception ex)
            {
                WriteLog($"Cleanup Error: {ex.Message}", LogLevel.ERROR);
            }
        }
        
        public void LogNetworkRequest(string method, string url, string headers = "", string body = "")
        {
            var details = $"Method: {method}, URL: {url}";
            
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
            
            WriteLog("Network Request", LogLevel.NETWORK_REQUEST, details);
        }
        
        public void LogNetworkResponse(string url, int statusCode, string responseBody = "", long responseTime = 0)
        {
            var details = $"URL: {url}, Status: {statusCode}";
            
            if (responseTime > 0)
            {
                details += $", Time: {responseTime}ms";
            }
            
            if (!string.IsNullOrEmpty(responseBody) && responseBody.Length <= 1000)
            {
                details += $"\nResponse: {responseBody}";
            }
            else if (!string.IsNullOrEmpty(responseBody))
            {
                details += $"\nResponse: [Length: {responseBody.Length} characters - truncated]";
            }
            
            WriteLog("Network Response", LogLevel.NETWORK_RESPONSE, details);
        }
        
        public void LogNetworkError(string url, string error, string method = "")
        {
            var details = $"URL: {url}, Error: {error}";
            if (!string.IsNullOrEmpty(method))
            {
                details = $"Method: {method}, " + details;
            }
            
            WriteLog("Network Error", LogLevel.ERROR, details);
        }
    }
}