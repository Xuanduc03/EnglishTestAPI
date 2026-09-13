namespace App.Domain.Domain.Exams;

/// <summary>
/// Các loại Prompt dành riêng cho AI Grading (Writing/Speaking)
/// </summary>
public enum PromptType
{
    #region IELTS Writing
    IeltsWritingTask1Graph = 1,
    IeltsWritingTask1Letter = 2,
    IeltsWritingTask1Process = 3,
    IeltsWritingTask1Map = 4,
    IeltsWritingTask2Opinion = 5,
    IeltsWritingTask2Discussion = 6,
    IeltsWritingTask2Problem = 7,
    IeltsWritingTask2Advantage = 8,
    #endregion

    #region IELTS Speaking
    IeltsSpeakingPart1 = 9,
    IeltsSpeakingPart2 = 10,
    IeltsSpeakingPart3 = 11,
    #endregion

    #region TOEIC Writing & Speaking
    ToeicPictureDescription = 12,
    ToeicWritingEmailResponse = 13,
    ToeicWritingOpinionEssay = 14,
    ToeicSpeakingReadAloud = 15,
    ToeicSpeakingDescribePicture = 16,
    ToeicSpeakingQuestions = 17,
    ToeicSpeakingProposeSolution = 18,
    ToeicSpeakingOpinion = 19
    #endregion
}