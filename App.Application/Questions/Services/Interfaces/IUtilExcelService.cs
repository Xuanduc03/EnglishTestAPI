using App.Application.Questions.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Questions.Services.Interfaces
{
    // Service hỗ trợ các hàm
   public interface IUtilExcelService
    {
        string CreateAnswerSetSignature(List<AnswerPreviewDto> answers);

        string StripHtml(string html);

        double CalculateSimilarity(string s1, string s2);

        int LevenshteinDistance(string s1, string s2);

        int GetExpectedQuestionCount(string partName);
    }
}
