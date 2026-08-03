using Microsoft.AspNetCore.Mvc;
using GastosApi.Services;

namespace GastosApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WhatsAppController : ControllerBase
{
    private readonly ILogger<WhatsAppController> _logger;
    private readonly WhatsAppService _whatsAppService;
    private readonly IConfiguration _config;

    public WhatsAppController(ILogger<WhatsAppController> logger, WhatsAppService whatsAppService, IConfiguration config)
    {
        _logger = logger;
        _whatsAppService = whatsAppService;
        _config = config;
    }

    [HttpGet("webhook")]
    public IActionResult VerifyWebhook(
        [FromQuery(Name = "hub.mode")] string mode,
        [FromQuery(Name = "hub.verify_token")] string token,
        [FromQuery(Name = "hub.challenge")] string challenge)
    {
        var expectedToken = _config["WhatsApp:VerifyToken"];

        if (mode == "subscribe" && token == expectedToken)
        {
            _logger.LogInformation("Webhook do WhatsApp verificado com sucesso.");
            return Ok(challenge);
        }

        _logger.LogWarning("Tentativa de verificação de webhook falhou.");
        return Forbid();
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> ReceiveMessage()
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            var messageInfo = ExtractMessage(doc);

            if (messageInfo == null)
            {
                return Ok();
            }

            _logger.LogInformation("Mensagem recebida de {From}: {Text}", messageInfo.Value.From, messageInfo.Value.Text);

            await _whatsAppService.ProcessIncomingMessageAsync(messageInfo.Value.From, messageInfo.Value.Text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar mensagem do WhatsApp.");
        }

        return Ok();
    }

    private static (string From, string Text)? ExtractMessage(System.Text.Json.JsonDocument doc)
    {
        try
        {
            var entry = doc.RootElement.GetProperty("entry")[0];
            var change = entry.GetProperty("changes")[0];
            var value = change.GetProperty("value");

            if (!value.TryGetProperty("messages", out var messages))
                return null;

            var message = messages[0];
            var from = message.GetProperty("from").GetString()!;
            var text = message.GetProperty("text").GetProperty("body").GetString()!;

            return (from, text);
        }
        catch
        {
            return null;
        }
    }
}