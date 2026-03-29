using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Questions.Dtos
{
    public class QuestionExportRawDto
    {
        public Guid Id { get; set; }
        public Guid? GroupId { get; set; }
        public string? Content { get; set; }
        public string Type { get; set; }
        public int OrderIndex { get; set; }
        public string? Explanation { get; set; }
        public string? MetadataJson { get; set; }

        public List<AnswerExportDto> Answers { get; set; } = new();
        public List<MediaExportDto> Media { get; set; } = new();
    }

    public class GroupExportRawDto
    {
        public Guid Id { get; set; }
        public string? Content { get; set; }
        public string? Transcript { get; set; }
        public string? Explanation { get; set; }

        public List<MediaExportDto> Media { get; set; } = new();
    }

    public class AnswerExportDto
    {
        public Guid QuestionId { get; set; }
        public string Content { get; set; }
        public bool IsCorrect { get; set; }
        public int OrderIndex { get; set; }
    }

    public class MediaExportDto
    {
        public string OwnerType { get; set; } // Question | Group
        public Guid OwnerId { get; set; }
        public string Url { get; set; }
        public string Type { get; set; }
        public int OrderIndex { get; set; }
    }
}
