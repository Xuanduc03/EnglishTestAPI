using App.Application.Interfaces;
using App.Application.Questions.Commands;
using App.Application.Questions.Dtos;
using App.Application.Questions.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;


namespace App.Application.Questions.Services
{
    public class ExcelZipParseContext
    {
        public Dictionary<string, CategoryLookupDto> Categories { get; init; }

        public List<ExistingQuestionLite> ExistingQuestions { get; init; } = new();
        public List<ExistingAnswerSetLite> ExistingAnswerSets { get; init; } = new();
        public List<QuestionGroupsDto> ExistingGroups { get; init; } = new();
        public HashSet<string> MissingMediaFiles { get; } =
        new(StringComparer.OrdinalIgnoreCase);

        public bool ValidateAgainstDatabase { get; init; } = true;
    }

    public class ExcelZipParserService : IExcelZipParser
    {
        private readonly IExcelQuestionParserService _excelService;
        public ExcelZipParserService(IExcelQuestionParserService excelService)
        {
            _excelService = excelService;
        }

        public async Task<ExcelZipParseResult> ParseAsync(
        IFormFile zipFile,
        ExcelZipParseContext context,
        CancellationToken cancellationToken)
        {
            var result = new ExcelZipParseResult();

            //1. extract zip file to temp folder
            var tempFolder = Path.Combine(Path.GetTempPath(), "import_zip_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempFolder);

            try
            {
                if (zipFile == null || zipFile.Length == 0)
                    throw new ArgumentException("Excel file is required.");

                await _excelService.ExtractZipToTempAsync(zipFile, tempFolder, cancellationToken);

                var zipExt = Path.GetExtension(zipFile.FileName).ToLowerInvariant();
                if (zipExt != ".zip")
                    throw new ArgumentException("File upload phải là .zip");

                // 2. đánh index media cho toàn bộ file 
                var mediaIndexPath = _excelService.IndexMediaFiles(tempFolder);

                //3. Tìm tệp Excel - ưu tiên tệp data.xlsx ở thư mục gốc hoặc sổ làm việc đầu tiên.
                var excelPath = _excelService.FindExcelPath(tempFolder);
                if (excelPath == null)
                    throw new ArgumentException("Không tìm thấy file excel (data.xlsx) trong file zip");

                //4. Phân tích tệp Excel và xác thực các tham chiếu.
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var packageStream = File.OpenRead(excelPath);
                using var package = new ExcelPackage(packageStream);

                foreach (var ws in package.Workbook.Worksheets)
                {
                    var sheet = ParseWorksheet(ws, context, mediaIndexPath);
                    result.Sheets.Add(sheet);
                }

                result.MissingMediaFiles = context.MissingMediaFiles.ToList();

                result.MediaIndex = mediaIndexPath.ToDictionary(
                   kv => kv.Key,                       // key: normalized fileName
                   kv => File.ReadAllBytes(kv.Value),  // value: byte[] đọc từ disk
                   StringComparer.OrdinalIgnoreCase
               );

                return result;
            }
            finally
            {
                _excelService.SafeDeleteDirectory(tempFolder);
            }
        }


        private SheetPreviewSummary ParseWorksheet(
            ExcelWorksheet ws,
            ExcelZipParseContext context,
            Dictionary<string, string> mediaIndex)
        {
            
            // Chọn logic thống nhất: phát hiện nhóm so với cá thể và tái sử dụng các phương pháp phân tích cú pháp + xác thực của bạn
            var sheetName = ws.Name?.Trim() ?? "";
            var sheetSummary = new SheetPreviewSummary { SheetName = sheetName };

            // Tìm danh mục phù hợp (tái sử dụng quá trình chuẩn hóa trước đó)
            var catEntry = _excelService.FindCategoryForSheet(sheetName, context.Categories);
            if (catEntry.Value == null)
            {
                sheetSummary.Error = $"Không tìm thấy Category cho sheet '{sheetName}'";
                return sheetSummary;
            }

            // Lấy id và tên của sheet sau khi tìm
            sheetSummary.CategoryId = catEntry.Value.Id;
            sheetSummary.CategoryName = catEntry.Value.Name;

            // Kiểm tra tiêu đề 
            var expected = _excelService.IsGroupPart(sheetName) ? _excelService.GetGroupExpectedHeaders(sheetName) : _excelService.GetExpectedHeaders(sheetName);
            var actualHeaders = _excelService.ReadHeaders(ws);
            if (!_excelService.HeadersMatch(actualHeaders, expected))
            {
                sheetSummary.Error = "Tiêu đề cột không đúng hoặc thiếu";
                return sheetSummary;
            }

            // Bây giờ, đối với mỗi hàng/nhóm, phân tích cú pháp
            // -> nhưng đồng thời xác thực xem phương tiện được tham chiếu có tồn tại trong mediaIndex hay không.
            if (_excelService.IsGroupPart(sheetName))
            {
                // phân tích nhóm (sử dụng lại ParseQuestionGroup nhưng cập nhật để chấp nhận mediaIndex)
                int rowCount = ws.Dimension?.Rows ?? 0;
                int currentRow = 2;
                var groupContentSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // phát hiện trùng nội dung trong file Excel
                while (currentRow <= rowCount)
                {
                    while (currentRow <= rowCount && _excelService.IsRowEmpty(ws, currentRow, ws.Dimension?.Columns ?? 0)) currentRow++;
                    if (currentRow > rowCount) break;

                    var groupDto = _excelService.ParseQuestionGroup(ws, ref currentRow, rowCount, sheetName, catEntry.Value.Id);
                    if (groupDto == null) continue;

                    //validate media 
                    if (!string.IsNullOrWhiteSpace(groupDto.AudioFileName))
                    {
                        if (!mediaIndex.ContainsKey(_excelService.NormalizeFileName(groupDto.AudioFileName)))
                        {
                            groupDto.Errors.Add(new ImportErrorDto
                            {
                                Code = "AUDIO_MISSING",
                                Message = $"Audio file '{groupDto.AudioFileName}' không tồn tại trong ZIP",
                                Column = "AudioFileName",
                                Severity = "error"
                            });
                            groupDto.HasError = true;
                            context.MissingMediaFiles.Add(groupDto.AudioFileName);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(groupDto.ImageFileName))
                    {
                        if (!mediaIndex.ContainsKey(_excelService.NormalizeFileName(groupDto.ImageFileName)))
                        {
                            groupDto.Errors.Add(new ImportErrorDto
                            {
                                Code = "IMAGE_MISSING",
                                Message = $"Image file '{groupDto.AudioFileName}' không tồn tại trong ZIP",
                                Column = "ImageFileName",
                                Severity = "error"
                            });
                            groupDto.HasError = true;
                            context.MissingMediaFiles.Add(groupDto.ImageFileName);
                        }
                    }

                    // validate question group 
                    _excelService.ValidateQuestionGroupCombined(groupDto, sheetName, groupContentSet, context.ExistingGroups);
                    sheetSummary.Items.Add(groupDto);
                    sheetSummary.TotalQuestionsOrGroups++;
                }
            }
            else
            {
                int rowCount = ws.Dimension?.Rows ?? 0;
                var contentSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var answerSignatureSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (int row = 2; row <= rowCount; row++)
                {
                    if (_excelService.IsRowEmpty(ws, row, ws.Dimension?.Columns ?? 0)) { continue; }
                    var qDto = _excelService.ParseQuestionRow(ws, row, sheetName, catEntry.Value.Id);

                    // check media existence
                    if (!string.IsNullOrWhiteSpace(qDto.AudioFileName)
                        && !mediaIndex.ContainsKey(_excelService.NormalizeFileName(qDto.AudioFileName)))
                    {
                        qDto.HasError = true;
                        qDto.Errors.Add(new ImportErrorDto
                        {
                            Code = "AUDIO_MISSING",
                            Message = $"Audio file '{qDto.AudioFileName}' không tồn tại trong file ZIP",
                            Row = qDto.RowNumber,
                            Column = "AudioFileName",
                            Severity = "error"
                        });

                        context.MissingMediaFiles.Add(qDto.AudioFileName);
                    }

                    if (!string.IsNullOrWhiteSpace(qDto.ImageFileName)
                        && !mediaIndex.ContainsKey(_excelService.NormalizeFileName(qDto.ImageFileName)))
                    {
                        qDto.HasError = true;
                        qDto.Errors.Add(new ImportErrorDto
                        {
                            Code = "IMAGE_MISSING",
                            Message = $"Image file '{qDto.ImageFileName}' không tồn tại trong file ZIP",
                            Row = qDto.RowNumber,
                            Column = "ImageFileName",
                            Severity = "error"
                        });

                        context.MissingMediaFiles.Add(qDto.ImageFileName);
                    }

                    if (context.ValidateAgainstDatabase)
                    {
                        _excelService.ValidateQuestionCombined(qDto, sheetName, contentSet, answerSignatureSet, context.ExistingQuestions, context.ExistingAnswerSets);
                    }
                    sheetSummary.Items.Add(qDto);
                    sheetSummary.TotalQuestionsOrGroups++;
                }
            }
            return sheetSummary;
        }

    }
}
