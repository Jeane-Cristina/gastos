using System.Net;
using System.Threading.RateLimiting;
using GastosApi.Services;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace GastosApi.Tests;

public class RateLimitPolicyTests
{
    private static DefaultHttpContext ContextFor(string? ip)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = ip == null ? null : IPAddress.Parse(ip);
        return context;
    }

    [Fact]
    public void LoginLimit_IsPerClient_OneIpBeingBlockedDoesNotBlockAnother()
    {
        using var limiter = PartitionedRateLimiter.Create<HttpContext, string>(RateLimitPolicies.LoginPartition);
        var attacker = ContextFor("203.0.113.10");
        var otherUser = ContextFor("203.0.113.11");

        for (var i = 0; i < RateLimitPolicies.LoginPermitsPerMinute; i++)
        {
            Assert.True(limiter.AttemptAcquire(attacker).IsAcquired);
        }

        Assert.False(limiter.AttemptAcquire(attacker).IsAcquired);
        Assert.True(limiter.AttemptAcquire(otherUser).IsAcquired);
    }

    [Fact]
    public void ClientKey_TreatsMappedIPv4AndPlainIPv4AsTheSameClient()
    {
        var plain = RateLimitPolicies.ClientKey(ContextFor("203.0.113.10"));
        var mapped = RateLimitPolicies.ClientKey(ContextFor("::ffff:203.0.113.10"));

        Assert.Equal(plain, mapped);
    }

    [Fact]
    public void ClientKey_WithoutIp_FallsBackToUnknown()
    {
        Assert.Equal("unknown", RateLimitPolicies.ClientKey(ContextFor(null)));
    }

    [Fact]
    public void DemoLimit_IsPerClient()
    {
        using var limiter = PartitionedRateLimiter.Create<HttpContext, string>(RateLimitPolicies.DemoPartition);
        var first = ContextFor("198.51.100.1");
        var second = ContextFor("198.51.100.2");

        for (var i = 0; i < RateLimitPolicies.DemoPermitsPerMinute; i++)
        {
            Assert.True(limiter.AttemptAcquire(first).IsAcquired);
        }

        Assert.False(limiter.AttemptAcquire(first).IsAcquired);
        Assert.True(limiter.AttemptAcquire(second).IsAcquired);
    }
}
