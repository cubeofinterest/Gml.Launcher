using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Gml.Launcher.Core.Services
{
    public class DevNetworkLogger : DelegatingHandler
    {
        private readonly DevFileLogger _logger;

        public DevNetworkLogger(HttpMessageHandler innerHandler, DevFileLogger logger) : base(innerHandler)
        {
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString().Substring(0, 8); // Создаем уникальный ID для отслеживания запроса
            
            try
            {
                // Логируем исходящий запрос
                await LogRequest(request, requestId);

                // Выполняем запрос
                var response = await base.SendAsync(request, cancellationToken);
                
                stopwatch.Stop();

                // Логируем ответ
                await LogResponse(response, stopwatch.ElapsedMilliseconds, requestId);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                // Логируем ошибку
                _logger.LogNetworkError(request.RequestUri?.ToString() ?? "Unknown", 
                    $"[REQ-{requestId}] {ex.Message}", request.Method.ToString());
                
                throw;
            }
        }

        private async Task LogRequest(HttpRequestMessage request, string requestId)
        {
            try
            {
                var url = request.RequestUri?.ToString() ?? "Unknown";
                var method = request.Method.ToString();
                var headers = "";
                var body = "";

                // Собираем заголовки (включая заголовки содержимого)
                var headerList = new System.Collections.Generic.List<string>();
                if (request.Headers != null)
                {
                    foreach (var header in request.Headers)
                    {
                        // Пропускаем чувствительные заголовки
                        if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) ||
                            header.Key.Equals("Cookie", StringComparison.OrdinalIgnoreCase))
                        {
                            headerList.Add($"{header.Key}: [REDACTED]");
                        }
                        else
                        {
                            headerList.Add($"{header.Key}: {string.Join(", ", header.Value)}");
                        }
                    }
                }
                
                // Добавляем заголовки содержимого, если они есть
                if (request.Content?.Headers != null)
                {
                    foreach (var header in request.Content.Headers)
                    {
                        headerList.Add($"{header.Key}: {string.Join(", ", header.Value)}");
                    }
                }
                
                headers = string.Join("; ", headerList);

                // Собираем тело запроса (если есть)
                if (request.Content != null)
                {
                    try
                    {
                        // Если это бинарные данные, не пытаемся читать как строку
                        if (request.Content is StreamContent || 
                            request.Content.Headers.ContentType?.MediaType?.StartsWith("image/") == true)
                        {
                            var contentLength = request.Content.Headers.ContentLength ?? 0;
                            body = $"[Binary content, length: {contentLength} bytes]";
                        }
                        else
                        {
                            body = await request.Content.ReadAsStringAsync();
                        }
                    }
                    catch
                    {
                        body = "[Cannot read request body]";
                    }
                }

                _logger.LogNetworkRequest(method, $"[REQ-{requestId}] {url}", headers, body);
            }
            catch (Exception ex)
            {
                _logger.WriteLog($"Error logging request: {ex.Message}", DevFileLogger.LogLevel.ERROR);
            }
        }

        private async Task LogResponse(HttpResponseMessage response, long responseTime, string requestId)
        {
            try
            {
                var url = response.RequestMessage?.RequestUri?.ToString() ?? "Unknown";
                var statusCode = (int)response.StatusCode;
                var responseBody = "";
                var headers = new StringBuilder();

                // Добавляем заголовки ответа
                if (response.Headers != null)
                {
                    foreach (var header in response.Headers)
                    {
                        // Пропускаем чувствительные заголовки
                        if (header.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
                        {
                            headers.Append($"{header.Key}: [REDACTED]; ");
                        }
                        else
                        {
                            headers.Append($"{header.Key}: {string.Join(", ", header.Value)}; ");
                        }
                    }
                }

                // Добавляем заголовки содержимого
                if (response.Content?.Headers != null)
                {
                    foreach (var header in response.Content.Headers)
                    {
                        headers.Append($"{header.Key}: {string.Join(", ", header.Value)}; ");
                    }
                }

                // Читаем тело ответа
                if (response.Content != null)
                {
                    try
                    {
                        // Проверяем тип содержимого
                        var contentType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
                        
                        // Если это бинарные данные, не пытаемся читать как строку
                        if (contentType != null && (
                            contentType.StartsWith("image/") || 
                            contentType.StartsWith("audio/") || 
                            contentType.StartsWith("video/") || 
                            contentType.StartsWith("application/octet-stream") ||
                            contentType.StartsWith("application/zip") ||
                            contentType.StartsWith("application/pdf")))
                        {
                            var contentLength = response.Content.Headers.ContentLength ?? 0;
                            responseBody = $"[Binary content ({contentType}), length: {contentLength} bytes]";
                        }
                        else
                        {
                            responseBody = await response.Content.ReadAsStringAsync();
                        }
                    }
                    catch
                    {
                        responseBody = "[Cannot read response body]";
                    }
                }

                _logger.LogNetworkResponse($"[REQ-{requestId}] {url}", statusCode, 
                    $"{headers}\n{responseBody}", responseTime);
            }
            catch (Exception ex)
            {
                _logger.WriteLog($"Error logging response: {ex.Message}", DevFileLogger.LogLevel.ERROR);
            }
        }
    }
}