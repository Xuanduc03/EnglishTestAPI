using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Interfaces
{
    /// <summary>
    ///  interface chức năng quét ảnh và render ra kết quả
    /// </summary>
    public interface IGeminiService
    {
     
            // 1 ảnh
            Task<string> ExtractExamAsync(
                string base64Image,
                string mimeType,
                string examType,
                CancellationToken cancellationToken = default);

            // Nhiều ảnh trong 1 request (Gemini hỗ trợ multi-image)
            Task<string> ExtractExamMultipleAsync(
                List<(string Base64, string MimeType)> images,
                string examType,
                CancellationToken cancellationToken = default);

        Task<string> ExtractQuestionsWithPassageAsync(
            List<(string Base64, string MimeType)> questionImages,
            string passageContent,
            string examType,
            CancellationToken cancellationToken = default);
    }
}
