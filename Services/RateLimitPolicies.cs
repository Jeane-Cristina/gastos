using System.Threading.RateLimiting;

namespace GastosApi.Services;

/// <summary>
/// Limites de requisição por cliente (IP). Cada IP tem a sua própria janela, então
/// alguém errando a senha não bloqueia os outros usuários.
/// Atrás do proxy do Render o IP real vem do cabeçalho X-Forwarded-For
/// (habilitado no Dockerfile por ASPNETCORE_FORWARDEDHEADERS_ENABLED).
/// </summary>
public static class RateLimitPolicies
{
    public const string Login = "LoginPolicy";
    public const string Demo = "DemoPolicy";

    public const int LoginPermitsPerMinute = 5;
    public const int DemoPermitsPerMinute = 10;

    public static string ClientKey(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress;
        if (ip == null) return "unknown";
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        return ip.ToString();
    }

    public static RateLimitPartition<string> LoginPartition(HttpContext context) => PerClient(context, LoginPermitsPerMinute);

    public static RateLimitPartition<string> DemoPartition(HttpContext context) => PerClient(context, DemoPermitsPerMinute);

    private static RateLimitPartition<string> PerClient(HttpContext context, int permitsPerMinute) =>
        RateLimitPartition.GetFixedWindowLimiter(ClientKey(context), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitsPerMinute,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
}
