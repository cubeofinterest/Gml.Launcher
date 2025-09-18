using Gml.Web.Api.Dto.Files;
using System.Net.Http;

namespace Gml.Launcher.Core.Services
{
    public interface IDevLoggingService
    {
        void LogFileCheck(string filePath, bool exists, long size = 0, string hash = "");
        void LogFileDownloadStart(string url, string localPath, string hash);
        void LogFileDownloadComplete(string localPath, bool success, long size = 0, string error = "");
        void LogFileValidation(string filePath, string expectedHash, string actualHash, bool isValid);
        void LogProfileAction(string profileName, string action, string details = "");
        void LogModAction(string modName, string action, string details = "");
        void LogDirectoryCreation(string path, bool success, string error = "");
        void LogLauncherUpdate(string action, string details = "");
        string? GetLogFilePath();
        void LogNetworkRequest(string method, string url, string headers = "", string body = "");
        void LogNetworkResponse(string url, int statusCode, string responseBody = "", long responseTime = 0);
        void LogNetworkError(string url, string error, string method = "");
        HttpMessageHandler CreateNetworkLoggingHandler(HttpMessageHandler innerHandler);
    }
}