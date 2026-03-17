using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;

namespace App.Api.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class TestAiController : ControllerBase
    {
        [HttpPost("extract")]
        public async Task<IActionResult> ExtractImage([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Chưa upload ảnh");

            // 1. Chuyển file ảnh sang Base64
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var base64Image = Convert.ToBase64String(ms.ToArray());

            // 2. Setup URL chuẩn theo cURL của bạn (Không nối key vào đây nữa)
            string endpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent";
            string apiKey = "AIzaSyDKA-yumIjHrsP52GjyE6rQaWiAVDHISqU";

            // 3. Prompt yêu cầu trả về JSON có cả bài đọc + trắc nghiệm
            string prompt = @"You are an expert data extractor system for IELTS exam papers. 
                I will provide you with an image of an IELTS reading test. 
                Your task is to extract the main reading passage and all the multiple-choice questions.
                Strictly return ONLY a valid JSON object. Do not output any markdown formatting like ```json.
                JSON format requirements:
                {
                  ""passageTitle"": ""Title of the passage if available, otherwise null"",
                  ""passageContent"": ""The full text of the reading passage here"",
                  ""questions"": [
                    {
                      ""questionText"": ""The extracted question text here"",
                      ""options"": {
                        ""A"": ""First option"",
                        ""B"": ""Second option"",
                        ""C"": ""Third option"",
                        ""D"": ""Fourth option or null if not present""
                      }
                    }
                  ]
                }";

            // 4. Build Body chứa text prompt VÀ data ảnh (giống hệt cấu trúc -d của curl)
            var requestBody = new
            {
                contents = new[]
                {
                new {
                    parts = new object[]
                    {
                        new { text = prompt },
                        new { inline_data = new { mime_type = file.ContentType, data = base64Image } }
                    }
                }
            }
            };

            // 5. Khởi tạo Request, gắn Header giống -H của curl
            using var client = new HttpClient();
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint);

            // Đây chính là điểm ăn tiền từ lệnh curl của bạn
            requestMessage.Headers.Add("X-goog-api-key", apiKey);

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            requestMessage.Content = content;

            // 6. Bắn!
            var response = await client.SendAsync(requestMessage);
            var responseString = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                // 1. Parse cái JSON khổng lồ của Google
                using JsonDocument doc = JsonDocument.Parse(responseString);

                // 2. Đi sâu vào trong để móc đúng cục text chứa dữ liệu mình cần
                string extractedJsonString = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                // 3. Trả về đúng cái JSON cấu trúc Đề thi + Câu hỏi cho ReactJS xử lý
                return Ok(extractedJsonString);
            }

            return BadRequest($"Lỗi từ Google API: {responseString}");
        }
    }
}
