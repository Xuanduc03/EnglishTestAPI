namespace App.Domain.Entities;

// Preserve the values used by existing API clients and question-bank data.
public enum QuestionTypeEnum
{
    SingleChoice = 1,
    MultipleChoice = 2,
    FillBlank = 3,
    Matching = 4,
    MatchingHeading = 5,
    MatchingInformation = 6,
    MatchingSentenceEnds = 7,
    TrueFalseNotGiven = 8,
    YesNoNotGiven = 9,
    ShortAnswer = 10,
    NoteCompletion = 11,
    FormCompletion = 12,
    TableCompletion = 13,
    SummaryCompletion = 14,
    SentenceCompletion = 15,
    MapLabeling = 16
}

public enum PromptTypeEnum
{
    IeltsWritingTask1Graph = 1,
    IeltsWritingTask1Letter = 2,
    IeltsWritingTask1Process = 3,
    IeltsWritingTask1Map = 4,
    IeltsWritingTask2Opinion = 5,
    IeltsWritingTask2Discussion = 6,
    IeltsWritingTask2Problem = 7,
    IeltsWritingTask2Advantage = 8,
    IeltsSpeakingPart1 = 9,
    IeltsSpeakingPart2 = 10,
    IeltsSpeakingPart3 = 11,
    ToeicPictureDescription = 12,
    ToeicWritingEmailResponse = 13,
    ToeicWritingOpinionEssay = 14,
    ToeicSpeakingReadAloud = 15,
    ToeicSpeakingDescribePicture = 16,
    ToeicSpeakingQuestions = 17,
    ToeicSpeakingProposeSolution = 18,
    ToeicSpeakingOpinion = 19
}
