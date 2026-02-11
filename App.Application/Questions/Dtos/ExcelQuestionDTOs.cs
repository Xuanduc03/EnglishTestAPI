using App.Application.Questions.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Questions.Dtos
{
    public class ExcelZipParseOptions
    {
        public bool ValidateMediaExistence { get; set; } = true;
        public bool ValidateExcelRules { get; set; } = true;

        // Import sẽ set thêm
        public List<ExistingQuestionLite> ExistingQuestions { get; set; }
        public List<ExistingAnswerSetLite> ExistingAnswerSets { get; set; }
    }


    public class ExcelZipParseResult
    {
        public string TempFolder { get; set; }
        public string ExcelPath { get; set; }

        public Dictionary<string, string> MediaIndex { get; set; } = new();
        public List<SheetPreviewSummary> Sheets { get; set; } = new();
        public List<string> MissingMediaFiles { get; set; } = new();
        public bool HasError =>
            Sheets.Any(s =>
                !string.IsNullOrEmpty(s.Error) ||
               s.Items.Any(i => ((dynamic)i).HasError));
    }

    // result + dto 
    public class PreviewQuestionResult
    {
        public string Message { get; set; }
        public int TotalProcessed { get; set; }
        public List<SheetPreviewSummary> Data { get; set; } = new();
        public List<string> MissingMediaFiles { get; set; } = new();
    }

    public class SheetPreviewSummary
    {
        public string SheetName { get; set; }
        public Guid? CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int TotalQuestionsOrGroups { get; set; }
        public string Error { get; set; } // Sheet-level error
        public List<object> Items { get; set; } = new(); // contains QuestionPrevie

    }
    public class QuestionPreviewDto
    {
        public int RowNumber { get; set; }
        public Guid CategoryId { get; set; }
        public string Content { get; set; }
        public Guid? DifficultyId { get; set; }
        public string DifficultyName { get; set; }
        public string AudioFileName { get; set; }
        public string ImageFileName { get; set; }
        public List<AnswerPreviewDto> Answers { get; set; } = new();
        public string Explanation { get; set; }
        public List<string> Tags { get; set; } = new();
        public bool HasError { get; set; }

        public bool IsDuplicate { get; set; }
        public bool IsValid { get; set; } = true;
        public List<ImportErrorDto> Errors { get; set; } = new();
    }

    public class QuestionGroupPreviewDto
    {
        public int StartRow { get; set; }
        public int EndRow { get; set; }
        public Guid CategoryId { get; set; }
        public string GroupTitle { get; set; }
        public string GroupContent { get; set; }
        public string? Explanation { get; set; }
        public string AudioFileName { get; set; }
        public string ImageFileName { get; set; }
        public List<QuestionInGroupPreviewDto> Questions { get; set; } = new();
        public bool HasError { get; set; }
        public List<ImportErrorDto> Errors { get; set; } = new();
    }

    public class QuestionInGroupPreviewDto
    {
        public int QuestionNumber { get; set; }
        public string Content { get; set; }
        public Guid? DifficultyId { get; set; }
        public string DifficultyName { get; set; }
        public List<AnswerPreviewDto> Answers { get; set; } = new();
        public string Explanation { get; set; }
        public bool HasError { get; set; }
        public List<ImportErrorDto> Errors { get; set; } = new();
    }

    public class AnswerPreviewDto
    {
        public string Content { get; set; }
        public bool IsCorrect { get; set; }
        public int OrderIndex { get; set; }
    }

    public class ImportErrorDto
    {
        public string Code { get; set; } = "";
        public string Message { get; set; } = "";
        public int? Row { get; set; }
        public string Column { get; set; }
        public string Severity { get; set; } = "error"; // error | warning
    }

    public class CategoryLookupDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }


    // Tạo dto cho tồn tại câu hỏi và đáp án 
    public class ExistingQuestionLite
    {
        public Guid Id { get; set; }
        public Guid CategoryId { get; set; }
        public string Content { get; set; }
    }

    public class ExistingAnswerSetLite
    {
        public Guid Id { get; set; }
        public Guid CategoryId { get; set; }
        public List<string> Answers { get; set; }
    }


    // Impor dto service 
    public class ImportOptions
    {
        public bool OverwriteExistingMedia { get; set; } = false;
    }

}
