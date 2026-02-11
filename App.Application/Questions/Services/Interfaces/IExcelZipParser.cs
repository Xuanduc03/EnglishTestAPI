using App.Application.Questions.Dtos;
using Microsoft.AspNetCore.Http;


namespace App.Application.Questions.Services.Interfaces
{
    // Đọc và triển khai parse
    public interface IExcelZipParser
    {
        Task<ExcelZipParseResult> ParseAsync(
        IFormFile zipFile,
        ExcelZipParseContext context,
        CancellationToken cancellationToken);
    }
}
