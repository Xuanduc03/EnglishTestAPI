using App.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Questions.Dtos
{
    public static class QuestionTypeHelper
    {
        private static readonly HashSet<QuestionTypeEnum> McqTypes = new()
        {
            QuestionTypeEnum.SingleChoice,
            QuestionTypeEnum.MultipleChoice,
            QuestionTypeEnum.TrueFalseNotGiven,
            QuestionTypeEnum.YesNoNotGiven,
            QuestionTypeEnum.MatchingHeading,
            QuestionTypeEnum.MatchingInformation,
            QuestionTypeEnum.Matching,
            QuestionTypeEnum.MatchingSentenceEnds,
        };

        private static readonly HashSet<QuestionTypeEnum> FillInTypes = new()
        {
            QuestionTypeEnum.FillBlank,
            QuestionTypeEnum.ShortAnswer,
            QuestionTypeEnum.NoteCompletion,
            QuestionTypeEnum.FormCompletion,
            QuestionTypeEnum.TableCompletion,
            QuestionTypeEnum.SummaryCompletion,
            QuestionTypeEnum.SentenceCompletion,
            QuestionTypeEnum.MapLabeling,
        };

        public static bool IsMcq(QuestionTypeEnum type) => McqTypes.Contains(type);
        public static bool IsFillIn(QuestionTypeEnum type) => FillInTypes.Contains(type);

        public static bool NeedsAnswerEntity(QuestionTypeEnum type)
            => IsMcq(type) || IsFillIn(type);

        public static int GetExpectedAnswerCount(string partCode, QuestionTypeEnum type) =>
            (partCode.ToUpper(), type) switch
            {
                ("PART 2", _) => 3,
                (_, QuestionTypeEnum.TrueFalseNotGiven) => 3,
                (_, QuestionTypeEnum.YesNoNotGiven) => 3,
                _ => 4,
            };
    }
}
