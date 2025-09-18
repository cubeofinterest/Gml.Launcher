using System;
using System.Net.Http;

namespace Gml.Launcher.Core.Services
{
    public class LoggingService : ILoggingService
    {
        private readonly FileLogger _logger;
        
        public LoggingService()
        {
#if DEBUG
            _logger = new FileLogger(isDebugMode: true);
#else
            _logger = new FileLogger(isDebugMode: false);
#endif
            _logger.WriteLog("LoggingService initialized", FileLogger.LogLevel.INFO);
        }

        public void LogFileCheck(string filePath, bool exists, long size = 0, string hash = "")
        {
            _logger?.LogFileCheck(filePath, exists, size, hash);
        }

        public void LogFileDownloadStart(string url, string localPath, string hash)
        {
            _logger?.WriteLog($"Starting file download", FileLogger.LogLevel.FILE_DOWNLOAD, 
                $"URL: {url}, Local: {localPath}, Hash: {hash}");
        }

        public void LogFileDownloadComplete(string localPath, bool success, long size = 0, string error = "")
        {
            _logger?.LogFileDownload("", localPath, size, success);
            if (!success && !string.IsNullOrEmpty(error))
            {
                _logger?.WriteLog($"Download failed: {error}", FileLogger.LogLevel.ERROR, $"File: {localPath}");
            }
        }

        public void LogFileValidation(string filePath, string expectedHash, string actualHash, bool isValid)
        {
            _logger?.LogFileValidation(filePath, expectedHash, actualHash, isValid);
        }

        public void LogProfileAction(string profileName, string action, string details = "")
        {
            _logger?.LogProfileAction(profileName, action, details);
        }

        public void LogModAction(string modName, string action, string details = "")
        {
            _logger?.LogModAction(modName, action, details);
        }

        public void LogDirectoryCreation(string path, bool success, string error = "")
        {
            _logger?.LogDirectoryOperation("CREATE", path, success, error);
        }

        public void LogLauncherUpdate(string action, string details = "")
        {
            _logger?.WriteLog($"Launcher Update: {action}", FileLogger.LogLevel.INFO, details);
        }

        public void LogException(Exception exception, string context = "")
        {
            _logger?.LogException(exception, context);
        }

        public void LogCritical(string message, string details = "")
        {
            _logger?.LogCritical(message, details);
        }

        public string? GetLogFilePath()
        {
            return _logger?.GetLogFilePath();
        }

        public void LogNetworkRequest(string method, string url, string headers = "", string body = "")
        {
            _logger?.LogNetworkRequest(method, url, headers, body);
        }

        public void LogNetworkResponse(string url, int statusCode, string responseBody = "", long responseTime = 0)
        {
            _logger?.LogNetworkResponse(url, statusCode, responseBody, responseTime);
        }

        public void LogNetworkError(string url, string error, string method = "")
        {
            _logger?.LogNetworkError(url, error, method);
        }

        public HttpMessageHandler CreateNetworkLoggingHandler(HttpMessageHandler innerHandler)
        {
            return new NetworkLogger(innerHandler, _logger);
        }
    }
}
