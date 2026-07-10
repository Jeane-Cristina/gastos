using GastosApi.Models;
using GastosApi.Dtos;

namespace GastosApi.Services;

public interface IExpenseService
{
    Task<List<Expense>> GetAllAsync(int? month, int? year, string? category);
    Task<Expense> CreateAsync(ExpenseDto dto);
    Task<bool> UpdateAsync(int id, ExpenseDto dto);
    Task<bool> DeleteAsync(int id);
    Task<List<CategorySummaryDto>> GetSummaryAsync();
}