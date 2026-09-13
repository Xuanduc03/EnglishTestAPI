

namespace App.Domain.Domain.Exams
{
    /// <summary>
    /// Các loại câu hỏi (TOEIC, IELTS)
    /// </summary>
    public enum QuestionType
    {
        #region TOEIC LR
        SingleChoice = 1,
        MultipleChoice = 2,
        IncompleteSentence = 3,
        TextCompletion = 4,
        #endregion

        #region TOEIC Speaking
        ReadAloud = 10,
        PictureDescription = 11,
        SpeakingResponse = 12,
        InformationResponse = 13,
        ProposeSolution = 14,
        OpinionResponse = 15,
        #endregion

        #region TOEIC Writing
        WritePictureSentence = 20,
        EmailResponse = 21,
        OpinionEssay = 22,
        #endregion

        #region IELTS Reading
        Matching = 30,
        MatchingHeading = 31,
        TrueFalseNotGiven = 32,
        YesNoNotGiven = 33,
        ShortAnswer = 34,
        NoteCompletion = 35,
        FormCompletion = 36,
        TableCompletion = 37,
        SummaryCompletion = 38,
        SentenceCompletion = 39,
        MapLabeling = 40
        #endregion
    }
}
