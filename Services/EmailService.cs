using Resend;

namespace GastosApi.Services;

public class EmailService
{
    private readonly IResend _resend;
    private readonly IConfiguration _config;

    public EmailService(IResend resend, IConfiguration config)
    {
        _resend = resend;
        _config = config;
    }

    public async Task SendPasswordResetEmailAsync(string username, string token)
    {
        var frontendUrl = _config["Frontend:Url"] ?? "http://localhost:5173";
        var resetLink = $"{frontendUrl}/reset-password?token={token}";

        var message = new EmailMessage
        {
            From = "Gastos <onboarding@resend.dev>", // domínio de teste do Resend; troque por domínio próprio depois se quiser
            Subject = "Redefinição de senha — Gastos",
            HtmlBody = $@"
                <p>Olá, {username}!</p>
                <p>Clique no link abaixo para redefinir sua senha. Este link expira em 15 minutos.</p>
                <p><a href='{resetLink}'>Redefinir senha</a></p>
                <p>Se você não solicitou isso, pode ignorar este e-mail.</p>"
        };
        message.To.Add(username); // ajuste se username não for o próprio e-mail

        await _resend.EmailSendAsync(message);
    }
}