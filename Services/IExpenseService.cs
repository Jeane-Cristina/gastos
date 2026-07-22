using GastosApi.Dtos;
using GastosApi.Models;

namespace GastosApi.Services;

public interface IExpenseService
{
    Task<List<Expense>> GetAllAsync(int userId, int? month, int? year, string? category, int? week);
    Task<Expense> CreateAsync(int userId, ExpenseDto dto);
    Task<bool> UpdateAsync(int userId, int id, ExpenseDto dto);
    Task<bool> DeleteAsync(int userId, int id);
    Task<List<CategorySummaryDto>> GetSummaryAsync(int userId);
}