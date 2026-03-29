using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Exams.Queries
{
    // ========================================================
    // 1. DTO (Data Transfer Object) - Nhẹ nhàng cho trang Home
    // ========================================================
    public class ExamHomeDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Code { get; set; }
        public int Duration { get; set; }
        public decimal TotalScore { get; set; }

        // Trả về số nguyên hoặc Enum để Frontend tự map ra giao diện
        public ExamType Type { get; set; }
        public ExamCategory Category { get; set; }
        public ExamLevel Level { get; set; }

        public string? Tags { get; set; }
        public int AttemptCount { get; set; } // Số lượt đã thi
    }

    // ========================================================
    // 2. KẾT QUẢ PHÂN TRANG (Dùng chung cho cả Project)
    // ========================================================
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    // ========================================================
    // 3. QUERY REQUEST (Đầu vào từ API)
    // ========================================================
    public record GetHomeExamsQuery : IRequest<PagedResult<ExamHomeDto>>
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 8; // Mặc định lấy 8 đề ra trang chủ

        // Frontend có thể truyền Type = 1 (TOEIC) hoặc 2 (IELTS) để lọc
        public ExamType? Type { get; set; }
    }

    // ========================================================
    // 4. HANDLER (Xử lý logic Database)
    // ========================================================
    public class GetHomeExamsQueryHandler : IRequestHandler<GetHomeExamsQuery, PagedResult<ExamHomeDto>>
    {
        private readonly IAppDbContext _context;

        public GetHomeExamsQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<ExamHomeDto>> Handle(GetHomeExamsQuery request, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            // Bước 1: Khởi tạo Query với các màng lọc Bảo mật TỐI QUAN TRỌNG
            var query = _context.Exams
                .AsNoTracking()
                .Where(e => e.IsActive)
                .Where(e => e.Status == ExamStatus.Published)
                .Where(e => !e.StartDate.HasValue || e.StartDate <= now)
                .Where(e => !e.EndDate.HasValue || e.EndDate >= now);

            // Bước 2: Lọc theo Loại đề thi (Nếu Frontend có truyền lên)
            if (request.Type.HasValue)
            {
                query = query.Where(e => e.Type == request.Type.Value);
            }

            // Bước 3: Đếm tổng số record thỏa mãn (Để làm phân trang)
            var totalCount = await query.CountAsync(cancellationToken);

            // Bước 4: Lấy dữ liệu phân trang và Map sang DTO
            var items = await query
                // Giả sử BaseEntity của bạn có trường CreatedAt (Ngày tạo). 
                // Nếu không có, bạn có thể đổi thành: OrderByDescending(e => e.Id)
                .OrderByDescending(e => e.Id)

                // Thuật toán Phân trang
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)

                // Map sang DTO (EF Core sẽ tự động tối ưu câu lệnh SELECT trong SQL)
                .Select(e => new ExamHomeDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    Code = e.Code,
                    Duration = e.Duration,
                    TotalScore = e.TotalScore,
                    Type = e.Type,
                    Category = e.Category,
                    Level = e.Level,
                    Tags = e.Tags,
                    // Điểm ăn tiền: Tự động đếm số lượt thi từ bảng Attempts
                    AttemptCount = e.Attempts.Count(a => !a.IsDeleted)
                })
                .ToListAsync(cancellationToken);

            // Bước 5: Trả về kết quả
            return new PagedResult<ExamHomeDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize
            };
        }
    }
}