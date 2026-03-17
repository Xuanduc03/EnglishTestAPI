// App.Infrastructure/Services/GeminiService.cs
using App.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace App.Infrastructure.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private const string ENDPOINT =
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent";

        public GeminiService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _apiKey = config["Gemini:ApiKey"]
                ?? throw new InvalidOperationException("Gemini:ApiKey not configured");
        }

        public async Task<string> ExtractExamAsync(
            string base64Image,
            string mimeType,
            string examType,
            CancellationToken cancellationToken = default)
        {
            var prompt = GeminiPrompts.Get(examType);

            var body = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = prompt },
                            new { inline_data = new { mime_type = mimeType, data = base64Image } }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.1,
                    topP = 0.95,
                    maxOutputTokens = 8192
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, ENDPOINT);
            request.Headers.Add("X-goog-api-key", _apiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Gemini API error: {raw}");

            // Extract text từ Gemini response
            using var doc = JsonDocument.Parse(raw);
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "";

            // Clean markdown
            return text
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();
        }

        /// <summary>
        /// Service xử lý đọc nhiều ảnh 
        /// </summary>
        /// <param name="images"></param>
        /// <param name="examType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="HttpRequestException"></exception>
        public async Task<string> ExtractExamMultipleAsync(
    List<(string Base64, string MimeType)> images,
    string examType,
    CancellationToken cancellationToken = default)
        {
            var prompt = GeminiPrompts.Get(examType);

            // Build parts: prompt text + tất cả ảnh
            var parts = new List<object> { new { text = prompt } };

            foreach (var img in images)
            {
                parts.Add(new
                {
                    inline_data = new
                    {
                        mime_type = img.MimeType,
                        data = img.Base64
                    }
                });
            }

            // Thêm instruction merge ở cuối
            parts.Add(new
            {
                text = $"The above {images.Count} images are pages from the SAME exam. " +
                       "Extract ALL content across ALL pages and merge into ONE JSON response. " +
                       "Re-number questions sequentially starting from 1."
            });

            var body = new
            {
                contents = new[]
                {
            new { parts = parts.ToArray() }
        },
                generationConfig = new
                {
                    temperature = 0.1,
                    topP = 0.95,
                    maxOutputTokens = 8192
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, ENDPOINT);
            request.Headers.Add("X-goog-api-key", _apiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Gemini API error: {raw}");

            using var doc = JsonDocument.Parse(raw);
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "";

            return text.Replace("```json", "").Replace("```", "").Trim();
        }
    }
}