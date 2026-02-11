using App.Application.Questions.Commands;
using App.Application.Questions.Dtos;
using App.Application.Questions.Services.Interfaces;
using App.Domain.Entities;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;

namespace App.Application.Questions.Services
{
    // ExcelQuestionParserService.cs
    public class ExcelQuestionParserService : IExcelQuestionParserService
    {
        private readonly IUtilExcelService _utilService;
        // config
        private readonly string[] AllowedAudioExt = new[] { ".mp3", ".wav", ".m4a" };
        private readonly string[] AllowedImageExt = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxZipBytes = 200 * 1024 * 1024; // 200MB limit example

        public ExcelQuestionParserService(IUtilExcelService utilService)
        {
            _utilService = utilService;
        }
        // Giải nén file zip 
        public async Task ExtractZipToTempAsync(IFormFile zipFile, string destinationFolder, CancellationToken cancellation)
        {
            // Lưu tệp zip đã tải lên vào đường dẫn tạm thời trước
            var tmpZip = Path.Combine(destinationFolder, "upload.zip");
            using (var fs = new FileStream(tmpZip, FileMode.Create))
            {
                await zipFile.CopyToAsync(fs, cancellation);
            }

            using (var archive = ZipFile.OpenRead(tmpZip))
            {
                foreach (var entry in archive.Entries)
                {
                    // Prevent zip-slip
                    var entryPath = entry.FullName;
                    if (entryPath.Contains("..")) continue;

                    // Only extract files we expect: .xlsx/.xls and allowed media
                    var ext = Path.GetExtension(entryPath).ToLowerInvariant();
                    if (string.IsNullOrEmpty(ext)) continue;

                    var allowedExts = new[] { ".xlsx", ".xls", ".mp3", ".wav", ".m4a", ".jpg", ".jpeg", ".png", ".webp" };
                    if (!allowedExts.Contains(ext)) continue;

                    var targetPath = Path.Combine(destinationFolder, entryPath.Replace('/', Path.DirectorySeparatorChar));
                    var targetDir = Path.GetDirectoryName(targetPath);
                    if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                    entry.ExtractToFile(targetPath, overwrite: true);
                }
            }

            // delete tmp zip
            File.Delete(Path.Combine(destinationFolder, "upload.zip"));
        }

        // đánh index cho các file media  
        public Dictionary<string, string> IndexMediaFiles(string rootFolder)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var mediaFiles = Directory.EnumerateFiles(rootFolder, "*.*", SearchOption.AllDirectories)
                .Where(p => AllowedAudioExt.Contains(Path.GetExtension(p).ToLowerInvariant())
                         || AllowedImageExt.Contains(Path.GetExtension(p).ToLowerInvariant()));

            foreach (var f in mediaFiles)
            {
                var nameWithExt = Path.GetFileName(f);
                var nameWithoutExt = Path.GetFileNameWithoutExtension(f);

                // ✅ Lưu CẢ 2 key: có extension và không có extension
                var key1 = NormalizeFileName(nameWithExt);      // "audio1.mp3"
                var key2 = NormalizeFileName(nameWithoutExt);   // "audio1"

                if (!map.ContainsKey(key1)) map[key1] = f;
                if (!map.ContainsKey(key2)) map[key2] = f; // ✅ Key backup
            }
            return map;
        }

        // hàm đọc header 
        public  List<string> ReadHeaders(ExcelWorksheet ws)
        {
            var headers = new List<string>();
            int cols = ws.Dimension?.Columns ?? 0;
            for (int c = 1; c <= cols; c++)
                headers.Add(ws.Cells[1, c].Value?.ToString()?.Trim() ?? "");
            return headers;
        }

        // tim file excel bên trong file zip
        public string FindExcelPath(string rootFolder)
        {
            // prefer data.xlsx at root, else first .xlsx found
            var prefer = Path.Combine(rootFolder, "data.xlsx");
            if (File.Exists(prefer)) return prefer;
            var first = Directory.EnumerateFiles(rootFolder, "*.xlsx", SearchOption.AllDirectories).FirstOrDefault();
            if (first != null) return first;
            var firstxls = Directory.EnumerateFiles(rootFolder, "*.xls", SearchOption.AllDirectories).FirstOrDefault();
            return firstxls;
        }

        // Hàm đọc dữ liệu câu hỏi đơn (part 1, 2,5 )
        public QuestionPreviewDto ParseQuestionRow(ExcelWorksheet ws, int row, string partName, Guid categoryId)
        {
            var dto = new QuestionPreviewDto
            {
                RowNumber = row,
                CategoryId = categoryId
            };

            int col;

            // ===== PART 1 =====
            if (partName.Contains("Part 1", StringComparison.OrdinalIgnoreCase))
            {
                // Layout:
                // STT | Audio | Image | A | B | C | D | Correct | Explanation | Tags
                col = 2;

                dto.AudioFileName = ws.Cells[row, col++].Value?.ToString()?.Trim();
                dto.ImageFileName = ws.Cells[row, col++].Value?.ToString()?.Trim();

                for (int i = 1; i <= 4; i++)
                {
                    dto.Answers.Add(new AnswerPreviewDto
                    {
                        Content = ws.Cells[row, col++].Value?.ToString()?.Trim(),
                        OrderIndex = i
                    });
                }

                var correctRaw = ws.Cells[row, col++].Value?.ToString()?.Trim();
                ApplyCorrectAnswer(dto.Answers, correctRaw);

                dto.Explanation = ws.Cells[row, col++].Value?.ToString()?.Trim();
                dto.Tags = ParseTags(ws.Cells[row, col].Value);

                return dto;
            }

            // ===== PART 2 =====
            if (partName.Contains("Part 2", StringComparison.OrdinalIgnoreCase))
            {
                col = 2; // bắt đầu sau STT

                dto.AudioFileName = ws.Cells[row, col++].Value?.ToString()?.Trim();

                // A, B, C
                for (int i = 1; i <= 3; i++)
                {
                    dto.Answers.Add(new AnswerPreviewDto
                    {
                        Content = ws.Cells[row, col++].Value?.ToString()?.Trim(),
                        OrderIndex = i
                    });
                }

                // Correct
                var correctRaw = ws.Cells[row, col++].Value?.ToString()?.Trim();
                ApplyCorrectAnswer(dto.Answers, correctRaw);

                // Explanation
                dto.Explanation = ws.Cells[row, col++].Value?.ToString()?.Trim();

                // Tags
                dto.Tags = ParseTags(ws.Cells[row, col].Value);

                return dto;
            }
            else if (partName.Contains("Part 5", StringComparison.OrdinalIgnoreCase))
            {
                col = 2; // sau QuestionNo

                // Question content
                dto.Content = ws.Cells[row, col++].Value?.ToString()?.Trim();

                // A, B, C, D
                for (int i = 1; i <= 4; i++)
                {
                    dto.Answers.Add(new AnswerPreviewDto
                    {
                        Content = ws.Cells[row, col++].Value?.ToString()?.Trim(),
                        OrderIndex = i
                    });
                }

                // Correct (A/B/C/D)
                var correctRaw = ws.Cells[row, col++].Value?.ToString()?.Trim();
                ApplyCorrectAnswer(dto.Answers, correctRaw);

                // Explanation
                dto.Explanation = ws.Cells[row, col++].Value?.ToString()?.Trim();

                // Tags
                dto.Tags = ParseTags(ws.Cells[row, col].Value);

                return dto;
            }

            return dto;

        }

        // Func : Đọc câu hỏi nhóm
        public QuestionGroupPreviewDto ParseQuestionGroup(ExcelWorksheet ws, ref int currentRow, int maxRow, string partName, Guid categoryId)
        {
            while (currentRow <= maxRow && IsRowEmpty(ws, currentRow, ws.Dimension?.Columns ?? 0))
                currentRow++;

            if (currentRow > maxRow) return null;

            int start = currentRow;

            var group = new QuestionGroupPreviewDto
            {
                StartRow = currentRow,
                CategoryId = categoryId,
                GroupTitle = ws.Cells[currentRow, 2].Text.Trim(),
                GroupContent = ws.Cells[currentRow, 3].Text.Trim()
            };

            // Đọc path của media part 3,4,7
            if (partName.Contains("Part 3", StringComparison.OrdinalIgnoreCase) ||
                partName.Contains("Part 4", StringComparison.OrdinalIgnoreCase))
            {
                group.AudioFileName = ws.Cells[currentRow, 4].Value?.ToString()?.Trim();
            }
            if (partName.Contains("Part 7", StringComparison.OrdinalIgnoreCase))
            {
                group.ImageFileName = ws.Cells[currentRow, 4].Value?.ToString()?.Trim();
            }

            // Xác định cột đánh số câu hỏi
            int questionNumberCol;
            if (partName.Contains("Part 3") || partName.Contains("Part 4") || partName.Contains("Part 7"))
            {
                questionNumberCol = 5; // có cột media ở giữa
            }
            else
            {
                questionNumberCol = 4; // part 6
            }

            var firstQNumText = ws.Cells[currentRow, questionNumberCol].Text.Trim();

            // Nếu dòng đầu tiên có QuestionNumber → đọc câu hỏi đầu tiên NGAY DÒNG NÀY
            if (int.TryParse(firstQNumText, out var firstQNum))
            {
                var firstQ = ParseQuestionInGroup(ws, currentRow, partName, firstQNum);
                group.Questions.Add(firstQ);
            }

            currentRow++;
            // Gom thêm nhóm nếu có câu hỏi trong group 
            while (currentRow <= maxRow)
            {
                if (partName.Contains("Part 7", StringComparison.OrdinalIgnoreCase))
                {
                    var groupTitleText = ws.Cells[currentRow, 2].Text.Trim();
                    if (!string.IsNullOrEmpty(groupTitleText))
                    {
                        break; // Group mới!
                    }
                }
                else // Part 3, 4, 6
                {
                    var sttText = ws.Cells[currentRow, 1].Text.Trim();
                    if (!string.IsNullOrEmpty(sttText) && int.TryParse(sttText, out _))
                    {
                        break; // Group mới!
                    }
                }

                // Kiểm tra QuestionNumber ở cột đúng
                var qNumText = ws.Cells[currentRow, questionNumberCol].Text.Trim();

                // Không có question number → kiểm tra dòng trống
                if (!int.TryParse(qNumText, out var qNum))
                {
                    if (IsRowEmpty(ws, currentRow, ws.Dimension?.Columns ?? 0))
                    {
                        currentRow++;
                        continue;
                    }
                    break;
                }


                var q = ParseQuestionInGroup(ws, currentRow, partName, qNum);

                group.Questions.Add(q);
                currentRow++;
            }

            group.EndRow = currentRow - 1;
            return group;
        }

        // Hàm xử lý đọc câu hỏi con trong câu hỏi nhóm 
        // Input : Ws - sheetname, row - từng hàng, startCol - Cột bắt đầu, 
        // Output : Câu hỏi con
        public QuestionInGroupPreviewDto ParseQuestionInGroup(ExcelWorksheet ws, int row, string partName, int questionNumber)
        {

            var q = new QuestionInGroupPreviewDto
            {
                QuestionNumber = questionNumber
            };

            int startCol;
            if (partName.Contains("Part 3") || partName.Contains("Part 4") || partName.Contains("Part 7"))
            {
                startCol = 6;
            }
            else // Part 6, 7
            {
                startCol = 5;
            }

            int c = startCol;

            // Đọc Question Content
            q.Content = ws.Cells[row, c++].Value?.ToString()?.Trim() ?? "";

            //  Đọc 4 đáp án: A, B, C, D
            for (int i = 0; i < 4; i++)
            {
                var ansContent = ws.Cells[row, c++].Value?.ToString()?.Trim() ?? "";
                q.Answers.Add(new AnswerPreviewDto
                {
                    Content = ansContent,
                    IsCorrect = false,
                    OrderIndex = i + 1
                });
            }

            //   Đọc cột Correct (A/B/C/D)
            var correctRaw = ws.Cells[row, c++].Value?.ToString()?.Trim();
            ApplyCorrectAnswer(q.Answers, correctRaw);

            //   Đọc Explanation (nếu có - Part 6,7) hoặc Tags (Part 3,4)
            if (partName.Contains("Part 6") || partName.Contains("Part 7"))
            {
                q.Explanation = ws.Cells[row, c++].Value?.ToString()?.Trim();
                // Tags ở cột cuối
                // Có thể add nếu cần: q.Tags = ParseTags(ws.Cells[row, c].Value);
            }
            // Có thể add nếu cần: q.Tags = ParseTags(ws.Cells[row, c].Value);

            return q;
        }

        // Hàm : tìm đọc danh mục theo sheet
        public KeyValuePair<string, CategoryLookupDto> FindCategoryForSheet(string sheetName, Dictionary<string, CategoryLookupDto> categories)
        {
            string Normalize(string s) => (s ?? "").Trim().ToLowerInvariant();
            var sheetNorm = Normalize(sheetName);
            var entry = categories
                .FirstOrDefault(c => Normalize(c.Key).Contains(sheetNorm)
                                    || sheetNorm.Contains(Normalize(c.Key)));
            return entry;
        }

        public bool IsGroupPart(string sheetName)
        {
            return sheetName.Contains("Part 3") || sheetName.Contains("Part 4") ||
                   sheetName.Contains("Part 6") || sheetName.Contains("Part 7");
        }


        // hàm so sánh header nhận được và header thực tế 
        public bool HeadersMatch(List<string> actualHeaders, List<string> expectedHeaders)
        {
            string Normalize(string s) => (s ?? "").Trim().ToLowerInvariant().Replace(" ", "").Replace("_", "");
            var normActual = actualHeaders.Select(Normalize).ToList();
            var normExpected = expectedHeaders.Select(Normalize).ToList();

            // require expected headers appear in order (not necessarily contiguous)
            int ai = 0;
            foreach (var eh in normExpected)
            {
                bool found = false;
                while (ai < normActual.Count)
                {
                    if (normActual[ai] == eh) { found = true; ai++; break; }
                    ai++;
                }
                if (!found) return false;
            }
            return true;
        }

        // clean directory sau khi upload file
        public void SafeDeleteDirectory(string folder)
        {
            try { if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true); }
            catch { }
        }

        // Helper methods
        public void ApplyCorrectAnswer(List<AnswerPreviewDto> answers, string correct)
        {
            if (string.IsNullOrWhiteSpace(correct)) return;
            var s = correct.Trim().ToUpperInvariant();
            int idx = s.Length > 0 && char.IsLetter(s[0]) ? s[0] - 'A' : -1;
            if (idx >= 0 && idx < answers.Count)
            {
                foreach (var a in answers) a.IsCorrect = false;
                answers[idx].IsCorrect = true;
            }
        }

        public List<string> ParseTags(object value)
        {
            return value?.ToString()?
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToList() ?? new List<string>();
        }


        // Validate Header cho câu hỏi đơn (Part 1, 2, 5)
        public List<string> GetExpectedHeaders(string partName)
        {
            var headers = new List<string> { };

            if (partName.Contains("Part 1", StringComparison.OrdinalIgnoreCase))
            {
                headers.AddRange(new[] {
                    "STT", "AudioFileName","ImageFileName", "A", "B", "C", "D", "Correct", "Tags"
                });
            }
            else if (partName.Contains("Part 2", StringComparison.OrdinalIgnoreCase))
            {
                headers.AddRange(new[]
                {
                    "STT", "AudioFileName", "A", "B", "C", "Correct", "Tags"
                });
            }
            else if (partName.Contains("Part 5", StringComparison.OrdinalIgnoreCase))
            {
                headers.AddRange(new[]
                {
                    "STT", "QuestionContent", "A", "B", "C", "D", "Correct", "Tags"
                });
            }

            return headers;
        }

        // validate header cho câu hỏi nhóm (part 3,4,6,7)
        public List<string> GetGroupExpectedHeaders(string partName)
        {
            var headers = new List<string> { };

            if (partName.Contains("Part 3", StringComparison.OrdinalIgnoreCase) ||
                partName.Contains("Part 4", StringComparison.OrdinalIgnoreCase))
            {
                return new() { "STT", "GroupTitle", "GroupContent", "AudioFileName", "QuestionNumber", "QuestionContent", "A", "B", "C", "D", "Correct", "Tags" };

            }
            else if (partName.Contains("Part 6", StringComparison.OrdinalIgnoreCase) ||
                partName.Contains("Part 7", StringComparison.OrdinalIgnoreCase))
            {
                headers.AddRange(new[]
                {
                    "STT", "GroupTitle","GroupContent", "QuestionNumber", "QuestionContent", "A", "B", "C", "D", "Correct", "Explanation", "Tags"
                });
            }

            return headers;
        }

        //  skip dòng trống focus vào nội dung
        public bool IsRowEmpty(ExcelWorksheet ws, int row, int colCount)
        {
            for (int col = 2; col <= colCount; col++)
            {
                if (!string.IsNullOrWhiteSpace(ws.Cells[row, col].Value?.ToString()))
                    return false;
            }
            return true;
        }


        #region Validate Full Part
        // ---- VALIDATION for single and group (unified, using ImportErrorDto list) ----
        public void ValidateQuestionCombined(QuestionPreviewDto dto, string partName, HashSet<string> contentSet, HashSet<string> answerSignatureSet, List<ExistingQuestionLite> existingQuestions, List<ExistingAnswerSetLite> existingAnswerSets)
        {

            // Media
            if ((partName.Contains("Part 1") || partName.Contains("Part 2")) && string.IsNullOrEmpty(dto.AudioFileName))
            {
                dto.HasError = true;
                dto.Errors.Add(new ImportErrorDto { Code = "MISSING_AUDIO", Message = $"{partName} phải có file Audio", Row = dto.RowNumber, Column = "AudioFileName" });
            }
            if (partName.Contains("Part 1") && string.IsNullOrEmpty(dto.ImageFileName))
            {
                dto.HasError = true;
                dto.Errors.Add(new ImportErrorDto { Code = "MISSING_IMAGE", Message = "Part 1 phải có file Image", Row = dto.RowNumber, Column = "ImageFileName" });
            }

            // Answers count
            int expected = partName.Contains("Part 2") ? 3 : 4;
            if (dto.Answers.Count != expected)
            {
                dto.HasError = true;
                dto.Errors.Add(new ImportErrorDto { Code = "ANSWER_COUNT", Message = $"{partName} phải có chính xác {expected} đáp án", Row = dto.RowNumber, Column = "Answers" });
            }

            // correct answer
            var correctCount = dto.Answers.Count(a => a.IsCorrect);
            if (correctCount != 1)
            {
                dto.HasError = true;
                dto.Errors.Add(new ImportErrorDto { Code = "CORRECT_COUNT", Message = $"Phải có đúng 1 đáp án đúng (Tìm thấy {correctCount})", Row = dto.RowNumber, Column = "IsCorrect" });
            }

            // empty answers
            var empty = dto.Answers.Where(a => string.IsNullOrWhiteSpace(a.Content)).ToList();
            if (empty.Any())
            {
                dto.HasError = true;
                dto.Errors.Add(new ImportErrorDto { Code = "EMPTY_ANSWER", Message = $"Có {empty.Count} đáp án bị bỏ trống", Row = dto.RowNumber, Column = "Answers" });
            }

            // duplicate within file (content)
            if (!string.IsNullOrWhiteSpace(dto.Content))
            {
                var clean = _utilService.StripHtml(dto.Content).Trim().ToLower();
                if (contentSet.Contains(clean))
                {
                    dto.HasError = true;
                    dto.IsDuplicate = true;
                    dto.Errors.Add(new ImportErrorDto { Code = "DUPLICATE_IN_FILE", Message = "Nội dung câu hỏi bị trùng trong file Excel", Row = dto.RowNumber });
                }
                else contentSet.Add(clean);
            }

            // duplicate answer set within file
            var sig = _utilService.CreateAnswerSetSignature(dto.Answers);
            if (answerSignatureSet.Contains(sig))
            {
                dto.HasError = true;
                dto.Errors.Add(new ImportErrorDto { Code = "DUP_ANSWERS_IN_FILE", Message = "Bộ đáp án bị trùng trong file Excel", Row = dto.RowNumber });
            }
            else answerSignatureSet.Add(sig);

            // similarity with existing questions
            if (!string.IsNullOrWhiteSpace(dto.Content))
            {
                var clean = _utilService.StripHtml(dto.Content).Trim().ToLower();
                var sim = existingQuestions
                    .Where(q => q.CategoryId == dto.CategoryId)
                    .Select(q => new { q.Id, Clean = _utilService.StripHtml(q.Content ?? "").Trim().ToLower() })
                    .Where(x => x.Clean.Length >= 10)
                    .FirstOrDefault(x =>
                    {
                        if (Math.Abs(clean.Length - x.Clean.Length) > clean.Length * 0.2) return false;
                        return _utilService.CalculateSimilarity(clean, x.Clean) > 0.85;
                    });

                if (sim != null)
                {
                    var similarity = _utilService.CalculateSimilarity(clean, sim.Clean);
                    dto.HasError = true;
                    dto.IsDuplicate = true;
                    dto.Errors.Add(new ImportErrorDto { Code = "SIMILARITY_DB", Message = $"Nội dung trùng {similarity:P0} với câu hỏi ID: {sim.Id}", Row = dto.RowNumber });
                }
            }

            // duplicate answer set vs existing
            if (dto.Answers.Any())
            {
                var newSig = _utilService.CreateAnswerSetSignature(dto.Answers);
                var dup = existingAnswerSets.Where(q => q.CategoryId == dto.CategoryId).FirstOrDefault(q =>
                {
                    var existingSig = string.Join("|", q.Answers.Select(a => _utilService.StripHtml(a ?? "").Trim().ToLower()));
                    return existingSig == newSig;
                });
                if (dup != null)
                {
                    dto.HasError = true;
                    dto.IsDuplicate = true;
                    dto.Errors.Add(new ImportErrorDto { Code = "DUP_ANSWERS_DB", Message = $"Bộ đáp án giống hệt câu hỏi ID: {dup.Id}", Row = dto.RowNumber });
                }
            }
        }

        public void ValidateQuestionGroupCombined(QuestionGroupPreviewDto group, string partName, HashSet<string> groupContentSet, List<Domain.Entities.QuestionGroup> existingGroups)
        {
            if (string.IsNullOrWhiteSpace(group.GroupContent))
            {
                group.HasError = true;
                group.Errors.Add(new ImportErrorDto { Code = "MISSING_GROUP_CONTENT", Message = "Nội dung nhóm (Passage/Conversation) không được để trống" });
            }

            if ((partName.Contains("Part 3", StringComparison.OrdinalIgnoreCase) || partName.Contains("Part 4", StringComparison.OrdinalIgnoreCase))
                && string.IsNullOrEmpty(group.AudioFileName))
            {
                group.HasError = true;
                group.Errors.Add(new ImportErrorDto { Code = "MISSING_AUDIO", Message = $"{partName} phải có file Audio" });
            }

            if (group.Questions.Count == 0)
            {
                group.HasError = true;
                group.Errors.Add(new ImportErrorDto { Code = "NO_QUESTIONS", Message = "Nhóm phải có ít nhất 1 câu hỏi" });
            }

            if (partName.Contains("Part 7", StringComparison.OrdinalIgnoreCase))
            {
                if (group.Questions.Count < 2 || group.Questions.Count > 5)
                {
                    group.HasError = true;
                    group.Errors.Add(new ImportErrorDto { Code = "GROUP_SIZE_INVALID", Message = $"{partName} phải có 2-5 câu hỏi/nhóm (Tìm thấy {group.Questions.Count})" });
                }
            }
            else
            {
                int expected = _utilService.GetExpectedQuestionCount(partName);
                if (group.Questions.Count != expected)
                {
                    group.HasError = true;
                    group.Errors.Add(new ImportErrorDto { Code = "GROUP_SIZE_MISMATCH", Message = $"{partName} mỗi nhóm phải có {expected} câu hỏi (Tìm thấy {group.Questions.Count})" });
                }
            }

            for (int i = 0; i < group.Questions.Count; i++)
            {
                var q = group.Questions[i];
                ValidateQuestionInGroupCombined(q, i + 1);
                if (q.HasError) group.HasError = true;
            }

            if (!string.IsNullOrWhiteSpace(group.GroupContent))
            {
                var clean = _utilService.StripHtml(group.GroupContent).Trim().ToLower();
                if (groupContentSet.Contains(clean))
                {
                    group.HasError = true;
                    group.Errors.Add(new ImportErrorDto { Code = "DUP_GROUP_IN_FILE", Message = "Nội dung nhóm bị trùng trong file Excel" });
                }
                else groupContentSet.Add(clean);

                var similar = existingGroups
                    .Where(g => g.CategoryId == group.CategoryId)
                    .Select(g => new { g.Id, Clean = _utilService.StripHtml(g.Content ?? "").Trim().ToLower() })
                    .Where(x => x.Clean.Length >= 20)
                    .FirstOrDefault(x =>
                    {
                        if (Math.Abs(clean.Length - x.Clean.Length) > clean.Length * 0.2) return false;
                        return _utilService.CalculateSimilarity(clean, x.Clean) > 0.85;
                    });

                if (similar != null)
                {
                    var similarity = _utilService.  CalculateSimilarity(clean, similar.Clean);
                    group.HasError = true;
                    group.Errors.Add(new ImportErrorDto { Code = "SIMILARITY_GROUP_DB", Message = $"Nội dung nhóm trùng {similarity:P0} với nhóm ID: {similar.Id}" });
                }
            }
        }

        public void ValidateQuestionInGroupCombined(QuestionInGroupPreviewDto question, int expectedNumber)
        {
            if (question.QuestionNumber != expectedNumber)
            {
                question.HasError = true;
                question.Errors.Add(new ImportErrorDto { Code = "WRONG_QUESTION_NUMBER", Message = $"Số thứ tự câu hỏi phải là {expectedNumber} (Tìm thấy {question.QuestionNumber})", Row = null, Column = "QuestionNumber" });
            }

            if (string.IsNullOrWhiteSpace(question.Content))
            {
                question.HasError = true;
                question.Errors.Add(new ImportErrorDto { Code = "MISSING_CONTENT", Message = "Nội dung câu hỏi không được để trống", Column = "QuestionContent" });
            }

            if (question.Answers.Count != 4)
            {
                question.HasError = true;
                question.Errors.Add(new ImportErrorDto { Code = "ANSWER_COUNT", Message = $"Câu hỏi phải có 4 đáp án (Tìm thấy {question.Answers.Count})" });
            }

            var correctCount = question.Answers.Count(a => a.IsCorrect);
            if (correctCount != 1)
            {
                question.HasError = true;
                question.Errors.Add(new ImportErrorDto { Code = "CORRECT_COUNT", Message = $"Phải có đúng 1 đáp án đúng (Tìm thấy {correctCount})" });
            }

            var empty = question.Answers.Where(a => string.IsNullOrWhiteSpace(a.Content)).ToList();
            if (empty.Any())
            {
                question.HasError = true;
                question.Errors.Add(new ImportErrorDto { Code = "EMPTY_ANSWER", Message = $"Có {empty.Count} đáp án bị bỏ trống" });
            }
        }
        #endregion

        public string NormalizeFileName(string name) => (name ?? "").Trim().ToLowerInvariant();
    }
}
