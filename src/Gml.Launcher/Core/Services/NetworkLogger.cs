using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Gml.Launcher.Core.Services
{
    public class NetworkLogger : DelegatingHandler
    {
        private readonly FileLogger _logger;

        public NetworkLogger(HttpMessageHandler innerHandler, FileLogger logger) : base(innerHandler)
        {
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString().Substring(0, 8);
            
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

                // Собираем заголовки (исключая чувствительные)
                var headerList = new List<string>();
                if (request.Headers != null)
                {
                    foreach (var header in request.Headers)
                    {
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
                
                if (request.Content?.Headers != null)
                {
                    foreach (var header in request.Content.Headers)
                    {
                        headerList.Add($"{header.Key}: {string.Join(", ", header.Value)}");
                    }
                }
                
                headers = string.Join("; ", headerList);

                // Собираем тело запроса
                if (request.Content != null)
                {
                    try
                    {
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
                _logger.WriteLog($"Error logging request: {ex.Message}", FileLogger.LogLevel.ERROR);
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
                        var contentType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
                        
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
                _logger.WriteLog($"Error logging response: {ex.Message}", FileLogger.LogLevel.ERROR);
            }
        }
    }
}
