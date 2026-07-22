namespace GastosApi.Dtos;

public class BulkImportDto
{
    public List<ExpenseDto> Expenses { get; set; } = new();
}