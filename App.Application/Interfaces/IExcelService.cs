using App.Application.Questions.Dtos;

namespace App.Application.Interfaces
{
    public interface IExcelService
    {
        byte[] GenerateExcel(
            List<GroupExportRawDto> groups,
            List<QuestionExportRawDto> questions,
            List<AnswerExportDto> answers,
            List<MediaExportDto> media);
    }
}
