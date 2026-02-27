using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.DTOs
{
    public class VocabularyWordDto
    {
        public Guid Id { get; set; }
        public string Word { get; set; }
        public string PartOfSpeech { get; set; }
        public string Phonetic { get; set; }
        public string Meaning { get; set; }
    }
    public class FlashcardDto
    {
        public Guid Id { get; set; }
        public string Word { get; set; }
        public string PartOfSpeech { get; set; }
        public string Phonetic { get; set; }
        public string Meaning { get; set; }
        public string UserStatus { get; set; } // learning, known, mastered (có thể null nếu chưa học)
    }
}
