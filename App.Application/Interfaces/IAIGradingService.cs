using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Interfaces
{
    /// <summary>
    /// interface: implement grading ai score writing and reading skill
    /// </summary>
    public interface IAIGradingService
    {
        Task<AIGradingResult> GradeWritingAsync(
           string questionContent,
           string studentAnswer,
           string? rubricJson,
           QuestionPromptType promptType,
           CancellationToken ct = default);

        Task<AIGradingResult> GradeSpeakingAsync(
           string questionContent,
           string audioUrl,
           string? rubricJson,
           CancellationToken ct = default);
    }

    public class AIGradingResult
    {
        public bool Success { get; set; }
        public double Score { get; set; }           // 0-10
        public string Feedback { get; set; } = "";  // Nhận xét tổng
        public string ScoreDetailJson { get; set; } = "{}"; // {"grammar":7,"vocabulary":6,...}
        public string? ErrorMessage { get; set; }
    }

    public enum QuestionPromptType
    {
        Writing,
        Speaking
    }
}
