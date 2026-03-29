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
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

        public GeminiService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _apiKey = config["Gemini:ApiKey"]
                ?? throw new InvalidOperationException("Gemini:ApiKey not configured");
        }

        // ── Single image ──────────────────────────────────────────
        public async Task<string> ExtractExamAsync(
            string base64Image,
            string mimeType,
            string examType,
            CancellationToken cancellationToken = default)
        {
            var safeExamType = examType == "IELTS_READING"
                ? "IELTS_READING_QUESTIONS_ONLY"
                : examType;

            var prompt = GeminiPrompts.Get(safeExamType);

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
                    maxOutputTokens = 65536
                }
            };

            var raw = await CallGeminiAsync(body, cancellationToken);
            return ParseGeminiResponse(raw);
        }

        // ── Multiple images ───────────────────────────────────────
        public async Task<string> ExtractExamMultipleAsync(
            List<(string Base64, string MimeType)> images,
            string examType,
            CancellationToken cancellationToken = default)
        {
            var prompt = GeminiPrompts.Get(examType);

            var parts = new List<object> { new { text = prompt } };
            foreach (var img in images)
                parts.Add(new { inline_data = new { mime_type = img.MimeType, data = img.Base64 } });

            var body = new
            {
                contents = new[] { new { parts = parts.ToArray() } },
                generationConfig = new
                {
                    temperature = 0.1,
                    topP = 0.95,
                    maxOutputTokens = 65536
                }
            };

            var raw = await CallGeminiAsync(body, cancellationToken);
            return ParseGeminiResponse(raw);
        }

        // ── Extract questions với passage context ─────────────────
        public async Task<string> ExtractQuestionsWithPassageAsync(
            List<(string Base64, string MimeType)> questionImages,
            string passageContent,
            string examType,
            CancellationToken cancellationToken = default)
        {
            // Dùng prompt chuẩn đã fix trong GeminiPrompts.cs
            var basePrompt = GeminiPrompts.Get(examType + "_QUESTIONS_ONLY");

            // Append passage như context tham khảo
            var fullPrompt = $@"{basePrompt}

            ══════════════════════════════
            PASSAGE CONTEXT (reference only — DO NOT reproduce in output)
            ══════════════════════════════
            {passageContent}

            REMINDER:
            - Extract questions and options EXACTLY from the images above
            - For MCQ: include ALL options A/B/C/D with FULL text content
            - For Matching with option box: include ALL box options as answers
            - For Diagram matching: include labels A through H as answers
            - DO NOT leave answers array empty for MCQ or Matching questions
            - DO NOT mark any answer as isCorrect
            ";

            var parts = new List<object> { new { text = fullPrompt } };
            foreach (var img in questionImages)
                parts.Add(new { inline_data = new { mime_type = img.MimeType, data = img.Base64 } });

            var body = new
            {
                contents = new[] { new { parts = parts.ToArray() } },
                generationConfig = new
                {
                    temperature = 0.1,
                    topP = 0.95,
                    maxOutputTokens = 65536
                }
            };

            var raw = await CallGeminiAsync(body, cancellationToken);
            return ParseGeminiResponse(raw);
        }

        // ── Shared HTTP call ──────────────────────────────────────
        private async Task<string> CallGeminiAsync(object body, CancellationToken ct)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, ENDPOINT);
            request.Headers.Add("X-goog-api-key", _apiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(
                    $"Gemini API error {(int)response.StatusCode}: {raw}");

            return raw;
        }

        // ── Parse Gemini response ─────────────────────────────────
        private static string ParseGeminiResponse(string raw)
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;

                if (root.TryGetProperty("error", out var error))
                {
                    var msg = error.TryGetProperty("message", out var m)
                        ? m.GetString() : "Unknown Gemini error";
                    throw new InvalidOperationException($"Gemini trả về lỗi: {msg}");
                }

                if (!root.TryGetProperty("candidates", out var candidates))
                    throw new InvalidOperationException(
                        $"Gemini response không có 'candidates'. Raw: {raw[..Math.Min(500, raw.Length)]}");

                if (candidates.GetArrayLength() == 0)
                    throw new InvalidOperationException("Gemini trả về candidates rỗng");

                var candidate = candidates[0];

                if (candidate.TryGetProperty("finishReason", out var fr))
                {
                    var reason = fr.GetString();
                    if (reason == "MAX_TOKENS")
                        throw new InvalidOperationException(
                            "Đề thi quá dài, AI bị cắt giữa chừng. Vui lòng upload từng ảnh riêng lẻ.");
                    if (reason == "SAFETY")
                        throw new InvalidOperationException("Gemini từ chối xử lý ảnh này vì lý do safety.");
                    if (reason == "RECITATION")
                        throw new InvalidOperationException("Gemini từ chối vì nội dung có bản quyền.");
                }

                if (!candidate.TryGetProperty("content", out var content))
                    throw new InvalidOperationException("Candidate không có 'content'");
                if (!content.TryGetProperty("parts", out var partsEl))
                    throw new InvalidOperationException("Content không có 'parts'");
                if (partsEl.GetArrayLength() == 0)
                    throw new InvalidOperationException("Parts rỗng");

                var text = partsEl[0].TryGetProperty("text", out var textEl)
                    ? textEl.GetString() ?? "" : "";

                if (string.IsNullOrWhiteSpace(text))
                    throw new InvalidOperationException("Gemini trả về text rỗng");

                var clean = text.Replace("```json", "").Replace("```", "").Trim();

                try
                {
                    using var testDoc = JsonDocument.Parse(clean);
                    return clean;
                }
                catch (JsonException ex)
                {
                    var repaired = TryRepairTruncatedJson(clean);
                    if (repaired != null) return repaired;

                    throw new InvalidOperationException(
                        $"JSON từ AI không hợp lệ: {ex.Message}. " +
                        $"Raw (500 chars): {clean[..Math.Min(500, clean.Length)]}");
                }
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Không thể parse Gemini response: {ex.Message}. " +
                    $"Raw (500 chars): {raw[..Math.Min(500, raw.Length)]}");
            }
        }

        // ── Repair JSON bị cắt ────────────────────────────────────
        private static string? TryRepairTruncatedJson(string truncated)
        {
            try
            {
                var depth = 0;
                var inString = false;
                var escape = false;
                var lastValidEnd = -1;

                for (int i = 0; i < truncated.Length; i++)
                {
                    char c = truncated[i];
                    if (escape) { escape = false; continue; }
                    if (c == '\\' && inString) { escape = true; continue; }
                    if (c == '"') { inString = !inString; continue; }
                    if (inString) continue;

                    if (c == '{' || c == '[') depth++;
                    if (c == '}' || c == ']')
                    {
                        depth--;
                        if (depth == 1) lastValidEnd = i;
                    }
                }

                if (lastValidEnd <= 0) return null;

                var repaired = truncated[..(lastValidEnd + 1)].TrimEnd().TrimEnd(',') + "]}";
                using var doc = JsonDocument.Parse(repaired);
                return repaired;
            }
            catch { return null; }
        }
    }
}