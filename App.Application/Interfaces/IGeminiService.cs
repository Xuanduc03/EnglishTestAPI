using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Interfaces
{
    public interface IGeminiService
    {
        Task<string> ExtractExamAsync(
            string base64Image,
            string mimeType,
            string examType,
            CancellationToken cancellationToken = default);
    }
}
