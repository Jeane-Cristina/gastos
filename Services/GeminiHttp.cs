using System.Net;

namespace GastosApi.Services;

/// <summary>
/// POST para a API do Gemini com nova tentativa em falhas temporárias (429, 5xx e erros de rede).
/// Espera 1 s e depois 2 s entre as tentativas (backoff exponencial).
/// </summary>
public static class GeminiHttp
{
    private static bool IsTransient(HttpStatusCode status) =>
        status == HttpStatusCode.TooManyRequests ||
        status == HttpStatusCode.InternalServerError ||
        status == HttpStatusCode.BadGateway ||
        status == HttpStatusCode.ServiceUnavailable ||
        status == HttpStatusCode.GatewayTimeout;

    public static TimeSpan DelayBeforeRetry(int attempt) => TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));

    public static async Task<HttpResponseMessage> PostWithRetryAsync(
        HttpClient client,
        string url,
        Func<HttpContent> contentFactory,
        int maxAttempts = 3)
    {
        HttpResponseMessage? response = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            response?.Dispose();

            try
            {
                response = await client.PostAsync(url, contentFactory());
            }
            catch (HttpRequestException) when (attempt < maxAttempts)
            {
                response = null;
                await Task.Delay(DelayBeforeRetry(attempt));
                continue;
            }

            if (response.IsSuccessStatusCode || !IsTransient(response.StatusCode) || attempt == maxAttempts)
                return response;

            await Task.Delay(DelayBeforeRetry(attempt));
        }

        return response!;
    }
}
