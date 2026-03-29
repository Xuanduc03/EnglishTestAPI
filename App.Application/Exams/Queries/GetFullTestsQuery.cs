using App.Application.DTOs;
using App.Application.Interfaces;
using App.Domain.Entities;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Exams.Queries
{
    /// <summary>
    /// Query: lấy danh sách các bài thi full test 
    /// (ExamCategory.FullTest) đã được xuất bản (ExamStatus.Published)
    /// </summary>
    public class GetFullTestsQuery : IRequest<List<ExamSummaryDto>>
    {
        // THÊM THAM SỐ NÀY (Nullable để nếu FE không truyền thì lấy tất cả)
        public ExamType? Type { get; set; }

        // Có thể thêm tham số phân trang sau nếu cần
        // public int PageIndex { get; set; } = 1;
        // public int PageSize { get; set; } = 20;
    }

    public class GetFullTestsQueryHandler : IRequestHandler<GetFullTestsQuery, List<ExamSummaryDto>>
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;

        public GetFullTestsQueryHandler(IAppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<ExamSummaryDto>> Handle(GetFullTestsQuery request, CancellationToken cancellationToken)
        {
            // 1. Lấy danh sách exam thỏa mãn điều kiện và LỌC THEO TYPE (nếu có)
            var query = _context.Exams
                .Where(x => !x.IsDeleted
                    && x.Category == ExamCategory.FullTest
                    && x.Status == ExamStatus.Published
                    && x.IsActive);

            // NẾU FE CÓ TRUYỀN LÊN ExamType (VD: Type = ExamType.IELTS) THÌ LỌC TIẾP
            if (request.Type.HasValue)
            {
                query = query.Where(x => x.Type == request.Type.Value);
            }

            var exams = await query
                .OrderByDescending(x => x.CreatedAt)
                .ProjectTo<ExamSummaryDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            if (!exams.Any()) return exams;

            var examIds = exams.Select(e => e.Id).ToList();

            // 2. Đếm số lượng attempt
            // ⚠️ FIX BUG CHÍ MẠNG: Bạn phải gom cụm OR (InProgress || Submitted) vào trong ngoặc tròn ( )
            // Nếu không có ngoặc, nó sẽ đếm TẤT CẢ các bài Submitted trong cả hệ thống (bất chấp ExamId là gì)
            var inProgressCounts = await _context.ExamAttempts
                .Where(a => examIds.Contains(a.ExamId)
                         && (a.Status == ExamAttemptStatus.InProgress || a.Status == ExamAttemptStatus.Submitted))
                .GroupBy(a => a.ExamId)
                .Select(g => new { ExamId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var countDict = inProgressCounts.ToDictionary(x => x.ExamId, x => x.Count);

            // 3. Gán giá trị vào DTO
            foreach (var exam in exams)
            {
                exam.ActiveUserCount = countDict.GetValueOrDefault(exam.Id, 0);
            }

            return exams;
        }
    }
}