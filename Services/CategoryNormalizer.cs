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

        // "Alimentação(Lazer)" e "Alimentação ( Lazer )" viram "Alimentação (Lazer)"
        collapsedSpaces = System.Text.RegularExpressions.Regex.Replace(collapsedSpaces, @"\s*\(\s*", " (");
        collapsedSpaces = System.Text.RegularExpressions.Regex.Replace(collapsedSpaces, @"\s*\)", ")").Trim();

        var textInfo = CultureInfo.GetCultureInfo("pt-BR").TextInfo;
        return textInfo.ToTitleCase(collapsedSpaces.ToLower(CultureInfo.GetCultureInfo("pt-BR")));
    }
}