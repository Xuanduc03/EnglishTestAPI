

using App.Application.DTOs;
using App.Application.Interfaces;
using App.Application.Share;
using App.Domain.Entities;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Exams.Queries
{
    //DTO: ExamListItemDto
    // API: GET /api/exams
    public record GetExamQuery : BaseGetAllQuery<ExamSummaryDto>
    {
        public string? status;
        public string? title;
        public string? code;
        public bool? isActive;
        public string? keyword;

    }

    public class GetExamQueryHandler : BaseQueryHandler<GetExamQuery, Exam, ExamSummaryDto>
    {
        public GetExamQueryHandler(IAppDbContext context, IMapper mapper) : base(context, mapper) { }

        // Ghi đè hàm query
        protected override IQueryable<Exam> BuildQuery(IQueryable<Exam> query, GetExamQuery request)
        {
            query = query.Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(request.title))
            {
                query = query.Where(x => x.Title.ToLower() == request.title.ToLower());
            }

            if(!string.IsNullOrWhiteSpace(request.code))
            {
                query = query.Where(x => x.Code.ToLower()  == request.code.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(request.keyword))
            {
                var key = request.keyword.Trim();
                query = query.Where(x =>
                    EF.Functions.Like(x.Title, $"%{key}%") ||
                    EF.Functions.Like(x.Code, $"%{key}%")
                );
            }

            if (request.isActive.HasValue)
            {
               query = query.Where(x => x.IsActive == request.isActive.Value);
            }

            return query;


        }
    }
}
