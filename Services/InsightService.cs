using System.Text;
using System.Text.Json;
using GastosApi.Models;

namespace GastosApi.Services;

public class InsightService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public InsightService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<string> GenerateInsightAsync(ExpenseAnalytics analytics, FinancialProfile? profile, string? note)
    {
        var prompt = BuildPrompt(analytics, profile, note);
        var apiKey = _config["Gemini:ApiKey"];

        var requestBody = new
        {
            contents = new[]
            {
            new { parts = new[] { new { text = prompt } } }
        }
        };

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={apiKey}";
        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);

        return doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? "Não foi possível gerar o insight desta vez.";
    }

    private static string BuildPrompt(ExpenseAnalytics analytics, FinancialProfile? profile, string? note)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Você é um assistente financeiro pessoal, direto e prático. Analise os dados abaixo e escreva uma recomendação curta (máximo 4 parágrafos), em português, com tom acolhedor mas objetivo.");
        sb.AppendLine();

        if (profile != null)
        {
            sb.AppendLine($"Renda mensal: R$ {profile.MonthlyIncome}");
            sb.AppendLine($"Meta de economia mensal: R$ {profile.SavingsGoal}");
            sb.AppendLine($"Objetivo de curto prazo: {profile.ShortTermGoal}");
            sb.AppendLine($"Objetivo de médio prazo: {profile.MediumTermGoal}");
            sb.AppendLine($"Objetivo de longo prazo: {profile.LongTermGoal}");
            sb.AppendLine();
        }

        sb.AppendLine($"Total gasto nos últimos 3 meses: R$ {analytics.TotalLast3Months}");
        sb.AppendLine("Gasto por semana do mês (1 = dias 1-7, 2 = dias 8-14, etc):");
        foreach (var w in analytics.SpendingByWeekOfMonth)
            sb.AppendLine($"  Semana {w.Week}: R$ {w.Total}");

        sb.AppendLine("Gasto por categoria:");
        foreach (var c in analytics.SpendingByCategory)
            sb.AppendLine($"  {c.Category}: R$ {c.Total}");

        if (!string.IsNullOrWhiteSpace(note))
        {
            sb.AppendLine();
            sb.AppendLine($"Observação da pessoa sobre esta semana: {note}");
        }

        sb.AppendLine();
        sb.AppendLine("Identifique padrões (ex: semana do mês com gasto mais alto) e dê 1-2 sugestões práticas e específicas, conectadas aos objetivos informados.");
        sb.AppendLine();
        sb.AppendLine("Estruture sua resposta EXATAMENTE neste formato, com esses três títulos:");
        sb.AppendLine();
        sb.AppendLine("PADRÃO IDENTIFICADO:");
        sb.AppendLine("[1-2 frases sobre o padrão de gasto mais relevante encontrado]");
        sb.AppendLine();
        sb.AppendLine("PONTOS DE ATENÇÃO:");
        sb.AppendLine("[1-2 frases sobre onde há espaço de economia]");
        sb.AppendLine();
        sb.AppendLine("SUGESTÃO PRÁTICA:");
        sb.AppendLine("[1 ação concreta e específica, conectada aos objetivos informados]");

        return sb.ToString();
    }
}