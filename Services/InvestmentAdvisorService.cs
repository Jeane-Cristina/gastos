using System.Text;
using System.Text.Json;
using GastosApi.Models;

namespace GastosApi.Services;

public class InvestmentAdvisorService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public InvestmentAdvisorService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<string> GetSuggestionsAsync(List<Investment> investments, FinancialProfile? profile)
    {
        var prompt = BuildPrompt(investments, profile);
        var apiKey = _config["Gemini:ApiKey"];

        var requestBody = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } }
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
            .GetString() ?? "Não foi possível gerar sugestões desta vez.";
    }

    private static string BuildPrompt(List<Investment> investments, FinancialProfile? profile)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Você é um assistente educacional sobre investimentos pessoais. IMPORTANTE: você não tem acesso a dados de mercado em tempo real (cotações, taxa Selic atual, etc). Suas sugestões devem ser baseadas em princípios gerais de diversificação e alinhamento a objetivos, deixando claro que não substituem um profissional certificado nem dados de mercado atualizados.");
        sb.AppendLine();

        if (profile != null)
        {
            sb.AppendLine($"Objetivo de curto prazo: {profile.ShortTermGoal}");
            sb.AppendLine($"Objetivo de médio prazo: {profile.MediumTermGoal}");
            sb.AppendLine($"Objetivo de longo prazo: {profile.LongTermGoal}");
            sb.AppendLine();
        }

        if (investments.Any())
        {
            sb.AppendLine("Investimentos atuais da pessoa:");
            foreach (var inv in investments)
            {
                var gain = inv.CurrentValue - inv.AmountInvested;
                sb.AppendLine($"  {inv.Name} ({inv.Type}, perfil {inv.RiskProfile}): investido R$ {inv.AmountInvested}, valor atual R$ {inv.CurrentValue} (ganho/perda: R$ {gain}), rentabilidade anual estimada informada pela pessoa: {inv.AnnualReturnRate}%");
            }
        }
        else
        {
            sb.AppendLine("A pessoa ainda não tem nenhum investimento cadastrado.");
        }

        sb.AppendLine();
        sb.AppendLine("Estruture sua resposta EXATAMENTE neste formato:");
        sb.AppendLine();
        sb.AppendLine("ANÁLISE DA CARTEIRA ATUAL:");
        sb.AppendLine("[avalie diversificação e alinhamento com os objetivos informados, ou a ausência de carteira]");
        sb.AppendLine();
        sb.AppendLine("SUGESTÕES DE MELHORIA:");
        sb.AppendLine("[1-3 sugestões gerais e educacionais, sem recomendar produto financeiro específico de uma instituição]");
        sb.AppendLine();
        sb.AppendLine("RESULTADO PROVÁVEL (ESTIMATIVA EDUCACIONAL):");
        sb.AppendLine("[com base nas taxas informadas pela própria pessoa, projete de forma simples e aproximada onde a carteira poderia estar em 1 e 5 anos, deixando claro que é só estimativa, não garantia]");

        return sb.ToString();
    }
}