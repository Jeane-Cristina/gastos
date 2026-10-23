using GastosApi.Services;
using Xunit;

namespace GastosApi.Tests;

public class CategoryNormalizerTests
{
    [Theory]
    [InlineData("IA", "Ia")]
    [InlineData("  alimentação  ", "Alimentação")]
    [InlineData("NÃO SEI", "Não Sei")]
    [InlineData("não sei", "Não Sei")]
    public void Normalize_UsesTitleCaseAndTrimsSpaces(string input, string expected)
    {
        Assert.Equal(expected, CategoryNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData("Alimentação(Lazer)")]
    [InlineData("alimentação ( lazer )")]
    [InlineData("ALIMENTAÇÃO  (LAZER)")]
    public void Normalize_UnifiesParenthesisVariants(string input)
    {
        Assert.Equal("Alimentação (Lazer)", CategoryNormalizer.Normalize(input));
    }

    [Fact]
    public void Normalize_KeepsEmptyValuesUntouched()
    {
        Assert.Equal("", CategoryNormalizer.Normalize(""));
        Assert.Equal("   ", CategoryNormalizer.Normalize("   "));
    }
}
