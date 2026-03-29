using App.Application.Interfaces;
using App.Application.Questions.Dtos;
using ClosedXML.Excel;

namespace App.Application.Services
{


    public class ExcelService : IExcelService
    {
        public byte[] GenerateExcel(
            List<GroupExportRawDto> groups,
            List<QuestionExportRawDto> questions,
            List<AnswerExportDto> answers,
            List<MediaExportDto> media)
        {
            using var workbook = new XLWorkbook();

            // ================= GROUPS =================
            var wsGroups = workbook.Worksheets.Add("Groups");
            wsGroups.Cell(1, 1).Value = "GroupId";
            wsGroups.Cell(1, 2).Value = "Content";
            wsGroups.Cell(1, 3).Value = "Transcript";
            wsGroups.Cell(1, 4).Value = "Explanation";

            for (int i = 0; i < groups.Count; i++)
            {
                var row = i + 2;
                var g = groups[i];

                wsGroups.Cell(row, 1).Value = g.Id.ToString();
                wsGroups.Cell(row, 2).Value = g.Content;
                wsGroups.Cell(row, 3).Value = g.Transcript;
                wsGroups.Cell(row, 4).Value = g.Explanation;
            }

            // ================= QUESTIONS =================
            var wsQ = workbook.Worksheets.Add("Questions");
            wsQ.Cell(1, 1).Value = "QuestionId";
            wsQ.Cell(1, 2).Value = "GroupId";
            wsQ.Cell(1, 3).Value = "Content";
            wsQ.Cell(1, 4).Value = "Type";
            wsQ.Cell(1, 5).Value = "Order";
            wsQ.Cell(1, 6).Value = "Explanation";
            wsQ.Cell(1, 7).Value = "MetadataJson";

            for (int i = 0; i < questions.Count; i++)
            {
                var row = i + 2;
                var q = questions[i];

                wsQ.Cell(row, 1).Value = q.Id.ToString();
                wsQ.Cell(row, 2).Value = q.GroupId?.ToString();
                wsQ.Cell(row, 3).Value = q.Content;
                wsQ.Cell(row, 4).Value = q.Type;
                wsQ.Cell(row, 5).Value = q.OrderIndex;
                wsQ.Cell(row, 6).Value = q.Explanation;
                wsQ.Cell(row, 7).Value = q.MetadataJson;
            }

            // ================= ANSWERS =================
            var wsA = workbook.Worksheets.Add("Answers");
            wsA.Cell(1, 1).Value = "QuestionId";
            wsA.Cell(1, 2).Value = "Content";
            wsA.Cell(1, 3).Value = "IsCorrect";
            wsA.Cell(1, 4).Value = "Order";

            for (int i = 0; i < answers.Count; i++)
            {
                var row = i + 2;
                var a = answers[i];

                wsA.Cell(row, 1).Value = a.QuestionId.ToString();
                wsA.Cell(row, 2).Value = a.Content;
                wsA.Cell(row, 3).Value = a.IsCorrect;
                wsA.Cell(row, 4).Value = a.OrderIndex;
            }

            // ================= MEDIA =================
            var wsM = workbook.Worksheets.Add("Media");
            wsM.Cell(1, 1).Value = "OwnerType";
            wsM.Cell(1, 2).Value = "OwnerId";
            wsM.Cell(1, 3).Value = "Url";
            wsM.Cell(1, 4).Value = "Type";
            wsM.Cell(1, 5).Value = "Order";

            for (int i = 0; i < media.Count; i++)
            {
                var row = i + 2;
                var m = media[i];

                wsM.Cell(row, 1).Value = m.OwnerType;
                wsM.Cell(row, 2).Value = m.OwnerId.ToString();
                wsM.Cell(row, 3).Value = m.Url;
                wsM.Cell(row, 4).Value = m.Type;
                wsM.Cell(row, 5).Value = m.OrderIndex;
            }


            // preview như đề thi
            var wsExam = workbook.Worksheets.Add("ExamView");
            wsExam.Column(1).Width = 120;
            wsExam.Style.Alignment.WrapText = true;
            int currentRow = 1;

            foreach (var group in groups)
            {
                // ===== PASSAGE =====
                wsExam.Cell(currentRow, 1).Value = "PASSAGE:";
                wsExam.Cell(currentRow, 1).Style.Font.Bold = true;
                currentRow++;

                wsExam.Cell(currentRow, 1).Value = FormatContent(group.Content);
                currentRow += 2;

                // ===== QUESTIONS =====
                var groupQuestions = questions
                    .Where(q => q.GroupId == group.Id)
                    .OrderBy(q => q.OrderIndex)
                    .ToList();

                int questionNumber = 1;

                foreach (var q in groupQuestions)
                {
                    wsExam.Cell(currentRow, 1).Value =
                        $"{questionNumber}. {FormatContent(q.Content)}";
                    currentRow++;

                    var qAnswers = answers
                        .Where(a => a.QuestionId == q.Id)
                        .OrderBy(a => a.OrderIndex)
                        .ToList();

                    char option = 'A';

                    foreach (var a in qAnswers)
                    {
                        wsExam.Cell(currentRow, 1).Value =
                            $"   {option}. {a.Content}";
                        currentRow++;
                        option++;
                    }

                    currentRow++; // spacing
                    questionNumber++;
                }

                currentRow += 2; // cách giữa group
            }

            // ================= EXPORT =================
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }


        // helper format content for export
        private string FormatContent(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            return content
                // xuống dòng HTML
                .Replace("<br>", "\n")
                .Replace("<br/>", "\n")
                .Replace("<br />", "\n")
                .Replace("</p>", "\n")

                // remove tag đơn giản
                .Replace("<p>", "")
                .Replace("<strong>", "")
                .Replace("</strong>", "")
                .Replace("<em>", "")
                .Replace("</em>", "")

                // clean spacing
                .Replace("&nbsp;", " ")
                .Trim();
        }
    }
}
