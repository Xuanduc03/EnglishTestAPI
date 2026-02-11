using App.Application.Categories.Queries;
using App.Application.DTOs;
using App.Application.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Exams.Queries
{
    //DTO: ExamDetailDto (include Sections + Questions)
    //API: GET /api/exams/{id

    public record GetExamDetailQuery(Guid examId) : IRequest<ExamDetailDto>;

    public class GetExamDetailQueryHandler : IRequestHandler<GetExamDetailQuery, ExamDetailDto>
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;

        public GetExamDetailQueryHandler(IAppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ExamDetailDto> Handle(GetExamDetailQuery request, CancellationToken cancellation)
        {
            var examDto = await _context.Exams
        .AsNoTracking()
        .Where(x => x.Id == request.examId)
        .ProjectTo<ExamDetailDto>(_mapper.ConfigurationProvider)
        .FirstOrDefaultAsync(cancellation);

            if (examDto == null)
                throw new KeyNotFoundException($"Không tìm thấy đề thi");

            examDto.Sections = examDto.Sections.OrderBy(s => s.OrderIndex).ToList();

            foreach (var section in examDto.Sections)
                section.Questions = section.Questions.OrderBy(q => q.OrderIndex).ToList();

            return examDto;

        }
    }


}
