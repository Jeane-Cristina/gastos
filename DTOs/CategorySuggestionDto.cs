namespace GastosApi.Dtos;

public class CategorySuggestionRequest
{
    public List<string> Descriptions { get; set; } = new();
}

public class CategorySuggestionResult
{
    public string Description { get; set; } = string.Empty;
    public string? SuggestedCategory { get; set; }
}