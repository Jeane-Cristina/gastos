using GastosApi.Services;
using Xunit;

namespace GastosApi.Tests;

public class StoredInsightTests
{
    [Fact]
    public void WrapThenSplit_RoundTripsPeriodAndText()
    {
        var (period, text) = StoredInsight.Split(StoredInsight.Wrap("monthly", "olá\nmundo"));

        Assert.Equal("monthly", period);
        Assert.Equal("olá\nmundo", text);
    }

    [Fact]
    public void Split_WithoutPrefix_ReturnsNullPeriodAndOriginalText()
    {
        var (period, text) = StoredInsight.Split("texto antigo");

        Assert.Null(period);
        Assert.Equal("texto antigo", text);
    }
}
