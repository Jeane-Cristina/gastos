using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using GastosApi.Data;
using GastosApi.Dtos;

namespace GastosApi.Services;

public class WhatsAppService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly AppDbContext _context;
    private readonly IExpenseService _expenseService;

    public WhatsAppService(HttpClient httpClient, IConfiguration config, AppDbContext context, IExpenseService expenseService)
    {
        _httpClient = httpClient;
        _config = config;
        _context = context;
        _expenseService = expenseService;
    }

    public async Task ProcessIncomingMessageAsync(string fromPhoneNumber, string messageText)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.WhatsAppNumber == fromPhoneNumber);

        if (user == null)
        {
            await SendMessageAsync(fromPhoneNumber, "Esse número não está vinculado a nenhuma conta. Cadastre seu número no app primeiro.");
            return;
        }

        var extracted = await ExtractExpenseFromTextAsync(messageText);

        if (extracted == null)
        {
            await SendMessageAsync(fromPhoneNumber, "Não consegui identificar um gasto nessa mensagem. Tente algo como: 'gastei 50 no mercado'.");
            return;
        }

        var expense = await _expenseService.CreateAsync(user.Id, extracted);

        await SendMessageAsync(fromPhoneNumber,
            $"✅ Registrado: {expense.Description} — R$ {expense.Amount:F2} ({expense.Category})");
    }

    private async Task<ExpenseDto?> ExtractExpenseFromTextAsync(string messageText)
    {
        var prompt = $@"Extraia os dados de uma despesa a partir desta mensagem. Responda APENAS com um JSON válido, sem texto adicional, no formato:
{{""description"": ""..."", ""amount"": 0.0, ""category"": ""...""}}

Se não conseguir identificar uma despesa clara na mensagem, responda: {{""error"": ""nao_identificado""}}

Mensagem: ""{messageText}""";

        var apiKey = _config["Gemini:ApiKey"];
        var requestBody = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={apiKey}";
        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content);
        if (!response.IsSuccessStatusCode) return null;

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);
        var rawText = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

        if (string.IsNullOrWhiteSpace(rawText)) return null;

        var cleaned = rawText.Trim().Replace("```json", "").Replace("```", "").Trim();

        try
        {
            using var parsed = JsonDocument.Parse(cleaned);
            if (parsed.RootElement.TryGetProperty("error", out _)) return null;

            return new ExpenseDto
            {
                Description = parsed.RootElement.GetProperty("description").GetString() ?? "Despesa via WhatsApp",
                Amount = parsed.RootElement.GetProperty("amount").GetDecimal(),
                Category = parsed.RootElement.GetProperty("category").GetString() ?? "Não categorizado",
                Date = DateTime.UtcNow
            };
        }
        catch
        {
            return null;
        }
    }

    private async Task SendMessageAsync(string toPhoneNumber, string text)
    {
        var accessToken = _config["WhatsApp:AccessToken"];
        var phoneNumberId = _config["WhatsApp:PhoneNumberId"];
        var url = $"https://graph.facebook.com/v21.0/{phoneNumberId}/messages";

        var requestBody = new
        {
            messaging_product = "whatsapp",
            to = toPhoneNumber,
            type = "text",
            text = new { body = text }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        await _httpClient.SendAsync(request);
    }
}