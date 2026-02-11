using App.Application.Questions.Commands;
using App.Application.Questions.Dtos;

namespace App.Application.Questions.Services.Interfaces
{
    // Nhận ExcelZipParseResult → upload media → map → save DB
    public interface IExcelZipImportService
    {
        Task<ImportQuestionExcelResult> ImportAsync(
            ExcelZipParseResult parseResult,
            ImportOptions options,
            CancellationToken ct);
    }
}
