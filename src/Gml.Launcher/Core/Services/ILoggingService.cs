using System;

namespace Gml.Launcher.Core.Services
{
    /// <summary>
    /// Интерфейс для сервиса логирования, работающего в любом режиме сборки
    /// </summary>
    public interface ILoggingService
    {
        /// <summary>
        /// Проверяет файл и логирует результат
        /// </summary>
        void LogFileCheck(string filePath, bool exists, long size = 0, string hash = "");
        
        /// <summary>
        /// Логирует начало загрузки файла
        /// </summary>
        void LogFileDownloadStart(string url, string localPath, string hash);
        
        /// <summary>
        /// Логирует завершение загрузки файла
        /// </summary>
        void LogFileDownloadComplete(string localPath, bool success, long size = 0, string error = "");
        
        /// <summary>
        /// Логирует валидацию файла
        /// </summary>
        void LogFileValidation(string filePath, string expectedHash, string actualHash, bool isValid);
        
        /// <summary>
        /// Логирует действие с профилем
        /// </summary>
        void LogProfileAction(string profileName, string action, string details = "");
        
        /// <summary>
        /// Логирует действие с модом
        /// </summary>
        void LogModAction(string modName, string action, string details = "");
        
        /// <summary>
        /// Логирует создание директории
        /// </summary>
        void LogDirectoryCreation(string path, bool success, string error = "");
        
        /// <summary>
        /// Логирует обновление лаунчера
        /// </summary>
        void LogLauncherUpdate(string action, string details = "");
        
        /// <summary>
        /// Логирует исключение
        /// </summary>
        void LogException(Exception exception, string context = "");
        
        /// <summary>
        /// Логирует критичную ошибку
        /// </summary>
        void LogCritical(string message, string details = "");
        
        /// <summary>
        /// Получает путь к файлу логов
        /// </summary>
        string? GetLogFilePath();
        
        /// <summary>
        /// Логирует сетевой запрос
        /// </summary>
        void LogNetworkRequest(string method, string url, string headers = "", string body = "");
        
        /// <summary>
        /// Логирует ответ сети
        /// </summary>
        void LogNetworkResponse(string url, int statusCode, string responseBody = "", long responseTime = 0);
        
        /// <summary>
        /// Логирует ошибку сети
        /// </summary>
        void LogNetworkError(string url, string error, string method = "");
        
        /// <summary>
        /// Создает обработчик для логирования сетевых запросов
        /// </summary>
        System.Net.Http.HttpMessageHandler CreateNetworkLoggingHandler(System.Net.Http.HttpMessageHandler innerHandler);
    }
}
