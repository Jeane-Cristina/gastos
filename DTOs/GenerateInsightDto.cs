namespace GastosApi.Dtos;

public class GenerateInsightDto
{
    public string? Note { get; set; }
    public string Period { get; set; } = "monthly";
}