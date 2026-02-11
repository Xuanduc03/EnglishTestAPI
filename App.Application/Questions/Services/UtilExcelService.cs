using App.Application.Questions.Dtos;
using App.Application.Questions.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace App.Application.Questions.Services
{
    public class UtilExcelService : IUtilExcelService
    {
        #region  Hepler

        public string CreateAnswerSetSignature(List<AnswerPreviewDto> answers)
        {
            return string.Join("|", answers.OrderBy(a => a.OrderIndex).Select(a => StripHtml(a.Content).Trim().ToLower()));
        }

        public string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;
            return Regex.Replace(html, "<.*?>", string.Empty);
        }

        public double CalculateSimilarity(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0;
            if (s1 == s2) return 1.0;
            var distance = LevenshteinDistance(s1, s2);
            var max = Math.Max(s1.Length, s2.Length);
            return 1.0 - ((double)distance / max);
        }

        public int LevenshteinDistance(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1)) return s2?.Length ?? 0;
            if (string.IsNullOrEmpty(s2)) return s1.Length;
            var m = s1.Length;
            var n = s2.Length;
            var matrix = new int[m + 1, n + 1];
            for (int i = 0; i <= m; i++) matrix[i, 0] = i;
            for (int j = 0; j <= n; j++) matrix[0, j] = j;
            for (int i = 1; i <= m; i++)
            {
                for (int j = 1; j <= n; j++)
                {
                    var cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                    matrix[i, j] = Math.Min(Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1), matrix[i - 1, j - 1] + cost);
                }
            }
            return matrix[m, n];
        }

        public int GetExpectedQuestionCount(string partName)
        {
            if (partName.Contains("Part 3", StringComparison.OrdinalIgnoreCase)) return 3;
            if (partName.Contains("Part 4", StringComparison.OrdinalIgnoreCase)) return 3;
            if (partName.Contains("Part 6", StringComparison.OrdinalIgnoreCase)) return 4;
            // Part 7 handled separately (2-5)
            return 3;
        }
        #endregion

    }
}
