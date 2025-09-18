using System;
using System.Net.Http;

namespace Gml.Launcher.Core.Services
{
    public class DevLoggingService : IDevLoggingService
    {
#if DEBUG
        private readonly DevFileLogger _logger;
        
        public DevLoggingService()
        {
            _logger = new DevFileLogger();
            _logger.WriteLog("DevLoggingService initialized", DevFileLogger.LogLevel.INFO);
        }
#endif

        public void LogFileCheck(string filePath, bool exists, long size = 0, string hash = "")
        {
#if DEBUG
            _logger?.LogFileCheck(filePath, exists, size, hash);
#endif
        }

        public void LogFileDownloadStart(string url, string localPath, string hash)
        {
#if DEBUG
            _logger?.WriteLog($"Starting file download", DevFileLogger.LogLevel.FILE_DOWNLOAD, 
                $"URL: {url}, Local: {localPath}, Hash: {hash}");
#endif
        }

        public void LogFileDownloadComplete(string localPath, bool success, long size = 0, string error = "")
        {
#if DEBUG
            _logger?.LogFileDownload("", localPath, size, success);
            if (!success && !string.IsNullOrEmpty(error))
            {
                _logger?.WriteLog($"Download failed: {error}", DevFileLogger.LogLevel.ERROR, $"File: {localPath}");
            }
#endif
        }

        public void LogFileValidation(string filePath, string expectedHash, string actualHash, bool isValid)
        {
#if DEBUG
            _logger?.LogFileValidation(filePath, expectedHash, actualHash, isValid);
#endif
        }

        public void LogProfileAction(string profileName, string action, string details = "")
        {
#if DEBUG
            _logger?.LogProfileAction(profileName, action, details);
#endif
        }

        public void LogModAction(string modName, string action, string details = "")
        {
#if DEBUG
            _logger?.LogModAction(modName, action, details);
#endif
        }

        public void LogDirectoryCreation(string path, bool success, string error = "")
        {
#if DEBUG
            _logger?.LogDirectoryOperation("CREATE", path, success, error);
#endif
        }

        public void LogLauncherUpdate(string action, string details = "")
        {
#if DEBUG
            _logger?.WriteLog($"Launcher Update: {action}", DevFileLogger.LogLevel.INFO, details);
#endif
        }

        public string? GetLogFilePath()
        {
#if DEBUG
            return _logger?.GetLogFilePath();
#else
            return null;
#endif
        }

        public void LogNetworkRequest(string method, string url, string headers = "", string body = "")
        {
#if DEBUG
            _logger?.LogNetworkRequest(method, url, headers, body);
#endif
        }

        public void LogNetworkResponse(string url, int statusCode, string responseBody = "", long responseTime = 0)
        {
#if DEBUG
            _logger?.LogNetworkResponse(url, statusCode, responseBody, responseTime);
#endif
        }

        public void LogNetworkError(string url, string error, string method = "")
        {
#if DEBUG
            _logger?.LogNetworkError(url, error, method);
#endif
        }

        public HttpMessageHandler CreateNetworkLoggingHandler(HttpMessageHandler innerHandler)
        {
#if DEBUG
            return new DevNetworkLogger(innerHandler, _logger);
#else
            return innerHandler;
#endif
        }
    }
}