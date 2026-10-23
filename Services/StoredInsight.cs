namespace GastosApi.Services;

/// <summary>
/// O período do insight fica guardado no próprio texto salvo ("[[period:monthly]]" + quebra de linha),
/// assim o cache respeita o período escolhido sem precisar de migration no banco.
/// </summary>
public static class StoredInsight
{
    private const string PeriodMarker = "[[period:";

    public static string NormalizePeriod(string? period) =>
        (period ?? "").Trim().ToLowerInvariant() switch
        {
            "weekly" => "weekly",
            "quarterly" => "quarterly",
            _ => "monthly"
        };

    public static string Wrap(string period, string text) => $"{PeriodMarker}{period}]]\n{text}";

    public static (string? Period, string Text) Split(string? stored)
    {
        if (string.IsNullOrEmpty(stored)) return (null, string.Empty);
        if (!stored.StartsWith(PeriodMarker, StringComparison.Ordinal)) return (null, stored);

        var end = stored.IndexOf("]]", StringComparison.Ordinal);
        if (end < 0) return (null, stored);

        var period = stored.Substring(PeriodMarker.Length, end - PeriodMarker.Length);
        return (period, stored.Substring(end + 2).TrimStart('\r', '\n'));
    }
}
