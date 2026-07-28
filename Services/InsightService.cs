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

    public async Task<string> GenerateInsightAsync(ExpenseAnalytics analytics, FinancialProfile? profile, string? note, List<PurchaseGoal> purchaseGoals)
    {
        var prompt = BuildPrompt(analytics, profile, note, purchaseGoals);
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

    public async Task<string> AnswerQuestionAsync(string question, ExpenseAnalytics analytics, FinancialProfile? profile)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Você é um assistente financeiro. Responda a pergunta abaixo de forma direta e curta (máximo 3 frases), com base SOMENTE nos dados fornecidos. Se a pergunta não puder ser respondida com esses dados, diga isso claramente em vez de inventar.");
        sb.AppendLine();
        sb.AppendLine($"Total gasto nos últimos 3 meses: R$ {analytics.TotalLast3Months}");
        sb.AppendLine("Gasto por categoria:");
        foreach (var c in analytics.SpendingByCategory)
            sb.AppendLine($"  {c.Category}: R$ {c.Total}");

        if (profile != null)
        {
            sb.AppendLine($"Meta de economia mensal: R$ {profile.SavingsGoal}");
            sb.AppendLine($"Renda mensal: R$ {profile.MonthlyIncome}");
        }

        sb.AppendLine();
        sb.AppendLine($"Pergunta: {question}");

        var apiKey = _config["Gemini:ApiKey"];
        var requestBody = new { contents = new[] { new { parts = new[] { new { text = sb.ToString() } } } } };
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={apiKey}";
        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);

        return doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString()
            ?? "Não consegui responder isso agora.";
    }

    private static string BuildPrompt(ExpenseAnalytics analytics, FinancialProfile? profile, string? note, List<PurchaseGoal> purchaseGoals)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Você é um assistente financeiro pessoal, direto e prático. Analise os dados abaixo e escreva uma recomendação em português, com tom acolhedor mas objetivo.");
        sb.AppendLine();

        if (profile != null)
        {
            sb.AppendLine($"Renda mensal: R$ {profile.MonthlyIncome}");
            sb.AppendLine($"Já guardado até agora: R$ {profile.CurrentSavings}");
            sb.AppendLine($"Meta de economia mensal: R$ {profile.SavingsGoal}");
            sb.AppendLine($"Meta de economia anual: R$ {profile.AnnualSavingsGoal}");
            sb.AppendLine($"Objetivo de curto prazo: {profile.ShortTermGoal}");
            sb.AppendLine($"Objetivo de médio prazo: {profile.MediumTermGoal}");
            sb.AppendLine($"Objetivo de longo prazo: {profile.LongTermGoal}");
            sb.AppendLine();
        }

        sb.AppendLine($"Total gasto nos últimos 3 meses: R$ {analytics.TotalLast3Months}");
        sb.AppendLine();
        sb.AppendLine("Detalhamento por semana do mês (1 = dias 1-7, 2 = dias 8-14, etc), com a categoria que mais pesou em cada semana:");
        foreach (var w in analytics.SpendingByWeekOfMonth)
        {
            var topCat = analytics.WeeklyTopCategories.FirstOrDefault(t => t.Week == w.Week);
            sb.AppendLine($"  Semana {w.Week}: total R$ {w.Total}" +
                (topCat != null ? $" — maior gasto em '{topCat.TopCategory}' (R$ {topCat.TopCategoryAmount})" : ""));
        }

        sb.AppendLine();
        sb.AppendLine("Gasto total por categoria (últimos 3 meses):");
        foreach (var c in analytics.SpendingByCategory)
            sb.AppendLine($"  {c.Category}: R$ {c.Total}");

        if (purchaseGoals.Any())
        {
            sb.AppendLine();
            sb.AppendLine("Lista de compras desejadas/necessárias da pessoa:");
            foreach (var g in purchaseGoals)
                sb.AppendLine($"  [{(g.IsEssential ? "Necessidade" : "Desejo")}, prioridade {g.Priority}] {g.Description} — R$ {g.EstimatedCost}");
        }

        if (!string.IsNullOrWhiteSpace(note))
        {
            sb.AppendLine();
            sb.AppendLine($"Observação da pessoa sobre esta semana: {note}");
        }

        sb.AppendLine();
        sb.AppendLine("Identifique padrões e dê sugestões práticas e específicas, conectadas aos objetivos informados.");
        sb.AppendLine();
        sb.AppendLine("Estruture sua resposta EXATAMENTE neste formato, com esses quatro títulos:");
        sb.AppendLine();
        sb.AppendLine("DETALHAMENTO SEMANAL:");
        sb.AppendLine("[analise cada semana e a categoria que mais pesou nela, apontando a semana mais crítica]");
        sb.AppendLine();
        sb.AppendLine("ONDE ECONOMIZAR:");
        sb.AppendLine("[1-2 categorias/semanas específicas onde cortar geraria mais impacto pra aumentar o valor guardado]");
        sb.AppendLine();
        sb.AppendLine("ESTRATÉGIA PARA SUAS COMPRAS:");
        sb.AppendLine("[se houver itens na lista de compras, sugira uma ordem/prazo realista pra realizá-los sem comprometer as metas; se não houver itens, omita esta seção]");
        sb.AppendLine();
        sb.AppendLine("SUGESTÃO PRÁTICA:");
        sb.AppendLine("[1 ação concreta pra essa semana]");

        return sb.ToString();
    }
    public async Task<string> GenerateJointInsightAsync(List<JointExpense> expenses, List<ContributionSummary> contributions, JointGoal? goal)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Você é um assistente financeiro para casais/duplas que dividem uma conta conjunta. Analise os dados e escreva uma recomendação em português, equilibrada entre as duas pessoas, sem parecer estar culpando ninguém.");
        sb.AppendLine();

        sb.AppendLine("Contribuição de cada pessoa:");
        foreach (var c in contributions)
            sb.AppendLine($"  {c.Username}: R$ {c.TotalPaid} ({c.Percent:F0}%)");

        if (goal != null)
        {
            sb.AppendLine();
            sb.AppendLine($"Limite mensal combinado: R$ {goal.MonthlySpendingLimit}");
            sb.AppendLine($"Meta de economia combinada: R$ {goal.MonthlySavingsGoal}");
        }

        var totalSpent = expenses.Sum(e => e.Amount);
        sb.AppendLine();
        sb.AppendLine($"Total gasto na conta conjunta: R$ {totalSpent}");

        sb.AppendLine();
        sb.AppendLine("Estruture sua resposta EXATAMENTE neste formato:");
        sb.AppendLine();
        sb.AppendLine("EQUILÍBRIO DA CONTA:");
        sb.AppendLine("[comente se a divisão está equilibrada ou não, sem julgamento, só constatação]");
        sb.AppendLine();
        sb.AppendLine("SUGESTÃO PRÁTICA:");
        sb.AppendLine("[1 ação concreta pros dois, conectada ao limite/meta combinados]");

        // reaproveita a mesma chamada HTTP pra Gemini já existente em GenerateInsightAsync —
        // extraia esse trecho pra um método privado compartilhado se quiser evitar duplicação
        var apiKey = _config["Gemini:ApiKey"];
        var requestBody = new { contents = new[] { new { parts = new[] { new { text = sb.ToString() } } } } };
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={apiKey}";
        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);

        return doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString()
            ?? "Não foi possível gerar o insight desta vez.";
    }
}