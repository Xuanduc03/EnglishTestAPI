using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.DTOs.Vocabulary
{
    public class CreateVocabularyDto
    {
        public string Word { get; set; }               // Từ vựng
        public string PartOfSpeech { get; set; }       // Loại từ (adj, v, n,...)
        public string Phonetic { get; set; }           // Phiên âm
        public string Meaning { get; set; }            // Nghĩa của từ
        public int OrderIndex { get; set; }            // STT (có thể dùng để sắp xếp)
        public string? AudioUrl { get; set; }
        public string? ImageUrl { get; set; }
        public IFormFile? AudioFile { get; set; }      // Phát âm
        public IFormFile? ImageFile { get; set; }      // Hình ảnh minh họa
        public string? Example { get; set; }           // Câu ví dụ
        public string? ExampleMeaning { get; set; }    // Nghĩa câu ví dụ
        public string? Level { get; set; }             // A1, A2, B1...
        public Guid? CategoryId { get; set; }          // Topic (Business, Travel...)
    }
}
