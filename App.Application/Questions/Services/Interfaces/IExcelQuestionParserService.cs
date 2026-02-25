using App.Application.Questions.Commands;
using App.Application.Questions.Dtos;
using App.Domain.Entities;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;


namespace App.Application.Questions.Services.Interfaces
{
    // IExcelQuestionParserService.cs
    //  Service : đọc và validate toàn bộ 
    public interface IExcelQuestionParserService
    {
        /// <summary>
        /// Extract ZIP to temp folder
        /// </summary>
        Task ExtractZipToTempAsync(IFormFile zipFile, string destinationFolder, CancellationToken ct);

        /// <summary>
        /// Index all media files (audio/image) from folder
        /// </summary>
        Dictionary<string, string> IndexMediaFiles(string rootFolder);

        /// <summary>
        /// Find Excel file path in folder
        /// </summary>
        string FindExcelPath(string rootFolder);

        /// <summary>
        /// Parse single question row (Part 1, 2, 5)
        /// </summary>
        QuestionPreviewDto ParseQuestionRow(
            ExcelWorksheet ws,
            int row,
            string partName,
            Guid categoryId
        );

        /// <summary>
        /// Parse question group (Part 3, 4, 6, 7)
        /// </summary>
        QuestionGroupPreviewDto ParseQuestionGroup(
            ExcelWorksheet ws,
            ref int currentRow,
            int maxRow,
            string partName,
            Guid categoryId
        );

        /// <summary>
        /// Parse question in group
        /// </summary>
        QuestionInGroupPreviewDto ParseQuestionInGroup(
            ExcelWorksheet ws,
            int row,
            string partName,
            int questionNumber
        );

        /// <summary>
        /// Find category for sheet name
        /// </summary>
        KeyValuePair<string, CategoryLookupDto> FindCategoryForSheet(
         string sheetName,
         Dictionary<string, CategoryLookupDto> categories);

        /// <summary>
        /// Check if part is group part
        /// </summary>
        bool IsGroupPart(string sheetName);

        /// <summary>
        /// Check if row is empty
        /// </summary>
        bool IsRowEmpty(ExcelWorksheet ws, int row, int colCount);

        /// <summary>
        /// Cleanup temp folder
        /// </summary>
        void SafeDeleteDirectory(string folder);


        // read heder 
        List<string> ReadHeaders(ExcelWorksheet ws);

        // so sánh header
        bool HeadersMatch(List<string> actualHeaders, List<string> expectedHeaders);

        // check correct answer
        void ApplyCorrectAnswer(List<AnswerPreviewDto> answers, string correct);

        // validate header cho câu hỏi đơn
        List<string> GetExpectedHeaders(string partName);

        // validate header cho câu hỏi nhóm
        List<string> GetGroupExpectedHeaders(string partName);


        // chuẩn hóa filename về lower 
        string NormalizeFileName(string name);


        void ValidateQuestionInGroupCombined(QuestionInGroupPreviewDto question, int expectedNumber);


        void ValidateQuestionGroupCombined(QuestionGroupPreviewDto group, string partName, HashSet<string> groupContentSet, List<QuestionGroupsDto> existingGroups);

        void ValidateQuestionCombined(QuestionPreviewDto dto, string partName, HashSet<string> contentSet, HashSet<string> answerSignatureSet, List<ExistingQuestionLite> existingQuestions, List<ExistingAnswerSetLite> existingAnswerSets);
    }
}
