using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
namespace App.Api.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class TestAiController : ControllerBase
    {
        [HttpPost("grade-writing")]
        public async Task<IActionResult> GradeWriting([FromBody] WritingRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Question) || string.IsNullOrWhiteSpace(req.Answer))
                return BadRequest("Thiếu question hoặc answer");

            string endpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent";
            string apiKey = "AIzaSyDKA-yumIjHrsP52GjyE6rQaWiAVDHISqU";

            // 🔥 Prompt chấm Writing
            string prompt = $@"
You are a TOEIC Writing examiner.

Score the answer based on 4 criteria:
- grammar (0-10)
- vocabulary (0-10)
- coherence (0-10)
- task achievement (0-10)

Rules:
- If the answer is too short (< 20 words), reduce task_achievement significantly
- Final score = average of all 4 criteria (rounded)

Return STRICT JSON only. No markdown. No explanation.

Format:
{{
  ""score"": number,
  ""grammar"": number,
  ""vocabulary"": number,
  ""coherence"": number,
  ""task_achievement"": number,
  ""feedback"": ""text"",
  ""improvement"": ""text""
}}

Question:
{req.Question}

Answer:
{req.Answer}
";

            var requestBody = new
            {
                contents = new[]
                {
                    new {
                        parts = new object[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            using var client = new HttpClient();
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint);

            requestMessage.Headers.Add("X-goog-api-key", apiKey);

            requestMessage.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.SendAsync(requestMessage);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using JsonDocument doc = JsonDocument.Parse(responseString);

                string result = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                // 🔥 clean nếu bị dính markdown
                result = result.Replace("```json", "").Replace("```", "").Trim();

                return Ok(result);
            }

            return BadRequest($"Gemini API error: {responseString}");
        }

        public class WritingRequest
        {
            public string Question { get; set; } = "";
            public string Answer { get; set; } = "";
        }
    }
}
