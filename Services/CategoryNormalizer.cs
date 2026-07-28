using System.Globalization;

namespace GastosApi.Services;

public static class CategoryNormalizer
{
    public static string Normalize(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return category;

        var trimmed = category.Trim();
        var collapsedSpaces = System.Text.RegularExpressions.Regex.Replace(trimmed, @"\s+", " ");

        var textInfo = CultureInfo.GetCultureInfo("pt-BR").TextInfo;
        return textInfo.ToTitleCase(collapsedSpaces.ToLower(CultureInfo.GetCultureInfo("pt-BR")));
    }
}