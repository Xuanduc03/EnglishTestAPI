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
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

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
    }
}