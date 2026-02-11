
using App.Application.Interfaces;
using MediatR;

namespace App.Application.Exams.Commands
{
    // B3: Thêm câu hỏi vào section ( thêm câu hỏi vào phần thi)
    // Input: SectionId, QuestionIds[], Points[], OrderIndexes[]
    public class AddQuestionsToSectionCommand : IRequest<List<Guid>>
    {
        public Guid ExamId { get; set; }
        public Guid SectionId { get; set; }
        public Guid CategoryId { get; set; }
        public List<Guid> QuestionIds { get; set; } 
        public decimal DefaultPoint { get; set; } = 1.0m;
    }
   


}
