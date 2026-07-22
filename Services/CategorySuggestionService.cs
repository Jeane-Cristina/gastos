using Microsoft.EntityFrameworkCore;
using GastosApi.Data;

namespace GastosApi.Services;

public class CategorySuggestionService
{
    private readonly AppDbContext _context;

    public CategorySuggestionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Dtos.CategorySuggestionResult>> SuggestAsync(int userId, List<string> descriptions)
    {
        var history = await _context.Expenses
            .Where(e => e.UserId == userId)
            .Select(e => new { e.Description, e.Category })
            .ToListAsync();

        var results = new List<Dtos.CategorySuggestionResult>();

        foreach (var desc in descriptions)
        {
            var match = history
                .Where(h => desc.Contains(h.Description, StringComparison.OrdinalIgnoreCase)
                         || h.Description.Contains(desc, StringComparison.OrdinalIgnoreCase))
                .GroupBy(h => h.Category)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            results.Add(new Dtos.CategorySuggestionResult
            {
                Description = desc,
                SuggestedCategory = match?.Key
            });
        }

        return results;
    }
}