using App.Application.DTOs;
using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace App.Application.Categories.Queries
{

    public record GetCategorySelectQuery(string? CodeType, string? ExamType = null)
        : IRequest<List<CategorySelectDto>>;

    public class GetCategorySelectQueryHandler
        : IRequestHandler<GetCategorySelectQuery, List<CategorySelectDto>>
    {
        private readonly IAppDbContext _context;
        public GetCategorySelectQueryHandler(IAppDbContext context) => _context = context;

        public async Task<List<CategorySelectDto>> Handle(
            GetCategorySelectQuery request, CancellationToken cancellation)
        {
            var query = _context.Categories.AsNoTracking()
                .Where(c => !c.IsDeleted && c.IsActive);

            if (!string.IsNullOrEmpty(request.CodeType))
                query = query.Where(c => c.CodeType == request.CodeType.ToUpper());

            // ── Filter theo examType ──────────────────────────────
            if (!string.IsNullOrEmpty(request.ExamType))
            {
                var examUpper = request.ExamType.ToUpperInvariant();

                // Map examType → allowed Name prefixes
                var allowedNames = GetAllowedNames(examUpper);

                if (allowedNames.Any())
                    query = query.Where(c => allowedNames.Contains(c.Name));
            }

            return await query
                .OrderBy(c => c.CodeType)
                .ThenBy(c => c.Name)
                .Select(c => new CategorySelectDto
                {
                    label = $"{c.Name} ({c.CodeType})",
                    value = c.Id
                })
                .ToListAsync(cancellation);
        }

        private static List<string> GetAllowedNames(string examType) => examType switch
        {
            var e when e.Contains("IELTS") && e.Contains("LISTENING")
                => ["Section 1", "Section 2", "Section 3", "Section 4"],

            var e when e.Contains("IELTS") && e.Contains("READING")
                => ["Passage 1", "Passage 2", "Passage 3"],

            var e when e.Contains("IELTS")
                => ["Section 1",
                    "Section 2",
                    "Section 3",
                    "Section 4",
                    "Passage 1",
                    "Passage 2",
                    "Passage 3"],

            var e when e.Contains("TOEIC") && e.Contains("LISTENING")
                => ["Part 1", "Part 2", "Part 3", "Part 4"],

            var e when e.Contains("TOEIC") && e.Contains("READING")
                => ["Part 5", "Part 6", "Part 7"],

            var e when e.Contains("TOEIC")
                => ["Part 1",
                    "Part 2",
                    "Part 3",
                    "Part 4",
                    "Part 5",
                    "Part 6",
                    "Part 7"],

            var e when e.Contains("KET") && e.Contains("LISTENING")
                => ["Listening Part 1",
                    "Listening Part 2",
                    "Listening Part 3",
                    "Listening Part 4",
                    "Listening Part 5"],

            var e when e.Contains("KET")
                => ["Reading Part 1",
                    "Reading Part 2",
                    "Reading Part 3",
                    "Reading Part 4",
                    "Reading Part 5",
                    "Reading & Writing",
                    "Writing Part 6",
                    "Writing Part 7"],

            _ => []
        };
    }
}
