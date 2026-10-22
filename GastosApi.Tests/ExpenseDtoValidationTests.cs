using System.ComponentModel.DataAnnotations;
using GastosApi.Dtos;
using Xunit;

namespace GastosApi.Tests;

public class ExpenseDtoValidationTests
{
    private static List<ValidationResult> Validate(ExpenseDto dto)
    {
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, context, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Dto_ComDadosValidos_NaoDeveGerarErros()
    {
        var dto = new ExpenseDto
        {
            Description = "Mercado",
            Amount = 50,
            Category = "Alimentação",
            Date = DateTime.Now
        };

        var erros = Validate(dto);

        Assert.Empty(erros);
    }

    [Fact]
    public void Dto_ComValorNegativo_DeveGerarErro()
    {
        var dto = new ExpenseDto
        {
            Description = "Mercado",
            Amount = -10,
            Category = "Alimentação",
            Date = DateTime.Now
        };

        var erros = Validate(dto);

        Assert.Contains(erros, e => e.ErrorMessage!.Contains("maior que zero"));
    }

    [Fact]
    public void Dto_ComDescricaoVazia_DeveGerarErro()
    {
        var dto = new ExpenseDto
        {
            Description = "",
            Amount = 50,
            Category = "Alimentação",
            Date = DateTime.Now
        };

        var erros = Validate(dto);

        Assert.Contains(erros, e => e.ErrorMessage!.Contains("obrigatória"));
    }
}