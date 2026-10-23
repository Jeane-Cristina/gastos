using System.Net;
using GastosApi.Services;
using Xunit;

namespace GastosApi.Tests;

public class GeminiHttpTests
{
    private sealed class SequenceHandler : HttpMessageHandler
    {
        private readonly Queue<HttpStatusCode> _statuses;
        public int Calls { get; private set; }

        public SequenceHandler(params HttpStatusCode[] statuses) => _statuses = new Queue<HttpStatusCode>(statuses);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            // repete o último status quando a fila acaba
            var status = _statuses.Count > 1 ? _statuses.Dequeue() : _statuses.Peek();
            return Task.FromResult(new HttpResponseMessage(status));
        }
    }

    private static Task<HttpResponseMessage> Post(SequenceHandler handler) =>
        GeminiHttp.PostWithRetryAsync(new HttpClient(handler), "https://example.test/gemini", () => new StringContent("{}"));

    [Fact]
    public async Task RetriesAfterTemporaryFailureAndReturnsSuccess()
    {
        var handler = new SequenceHandler(HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);

        var response = await Post(handler);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task DoesNotRetryClientErrors()
    {
        var handler = new SequenceHandler(HttpStatusCode.BadRequest);

        var response = await Post(handler);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task GivesUpAfterThreeAttempts()
    {
        var handler = new SequenceHandler(HttpStatusCode.ServiceUnavailable);

        var response = await Post(handler);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(3, handler.Calls);
    }

    [Fact]
    public void BackoffDoublesBetweenAttempts()
    {
        Assert.Equal(TimeSpan.FromSeconds(1), GeminiHttp.DelayBeforeRetry(1));
        Assert.Equal(TimeSpan.FromSeconds(2), GeminiHttp.DelayBeforeRetry(2));
    }
}
