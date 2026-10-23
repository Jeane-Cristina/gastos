using GastosApi.Models;
using GastosApi.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GastosApi.Tests;

public class DemoDataServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 22, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateDemoUser_SeedsDataForTheNewUser()
    {
        using var context = TestDbContextFactory.Create();
        var service = new DemoDataService(context);

        var user = await service.CreateDemoUserAsync(Now);

        Assert.NotNull(user);
        Assert.True(DemoDataService.IsDemoUsername(user!.Username));
        Assert.True(await context.Expenses.CountAsync(e => e.UserId == user.Id) >= 5);
        Assert.Equal(3, await context.GoalSnapshots.CountAsync(s => s.UserId == user.Id));
        Assert.Equal(5, await context.CategoryBudgets.CountAsync(b => b.UserId == user.Id));
        Assert.NotEmpty(await context.Investments.Where(i => i.UserId == user.Id).ToListAsync());
        Assert.NotEmpty(await context.PurchaseGoals.Where(g => g.UserId == user.Id).ToListAsync());

        var profile = await context.FinancialProfiles.SingleAsync(p => p.UserId == user.Id);
        var (period, text) = StoredInsight.Split(profile.LastInsight);
        Assert.Equal("monthly", period);
        Assert.Contains("DETALHAMENTO DO PERÍODO", text);
    }

    [Fact]
    public async Task CreateDemoUser_GivesEachVisitorAnIsolatedUser()
    {
        using var context = TestDbContextFactory.Create();
        var service = new DemoDataService(context);

        var first = await service.CreateDemoUserAsync(Now);
        var second = await service.CreateDemoUserAsync(Now);

        Assert.NotEqual(first!.Username, second!.Username);
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task CreateDemoUser_ProducesAGoalReport()
    {
        using var context = TestDbContextFactory.Create();
        var user = await new DemoDataService(context).CreateDemoUserAsync(DateTime.UtcNow);

        var report = await new GoalService(context).GetReportAsync(user!.Id);

        Assert.NotNull(report);
    }

    [Fact]
    public async Task DeleteExpired_RemovesOldDemoUsersAndTheirData_KeepsOthers()
    {
        using var context = TestDbContextFactory.Create();
        var service = new DemoDataService(context);

        var old = await service.CreateDemoUserAsync(Now.AddHours(-25));
        var recent = await service.CreateDemoUserAsync(Now.AddHours(-1));
        var real = new User { Username = "maria@exemplo.com", PasswordHash = "x" };
        context.Users.Add(real);
        await context.SaveChangesAsync();
        context.Expenses.Add(new Expense { UserId = real.Id, Description = "Real", Amount = 10, Category = "Outros", Date = Now });
        await context.SaveChangesAsync();

        var removed = await service.DeleteExpiredAsync(Now);

        Assert.Equal(1, removed);
        Assert.False(await context.Users.AnyAsync(u => u.Id == old!.Id));
        Assert.False(await context.Expenses.AnyAsync(e => e.UserId == old!.Id));
        Assert.False(await context.FinancialProfiles.AnyAsync(p => p.UserId == old!.Id));
        Assert.True(await context.Users.AnyAsync(u => u.Id == recent!.Id));
        Assert.True(await context.Expenses.AnyAsync(e => e.UserId == recent!.Id));
        Assert.True(await context.Users.AnyAsync(u => u.Id == real.Id));
        Assert.True(await context.Expenses.AnyAsync(e => e.UserId == real.Id));
    }

    [Fact]
    public async Task CreateDemoUser_ReturnsNullAtCapacity()
    {
        using var context = TestDbContextFactory.Create();
        for (var i = 0; i < DemoDataService.MaxActiveDemoUsers; i++)
        {
            context.Users.Add(new User { Username = $"demo-{new DateTimeOffset(Now).ToUnixTimeSeconds()}-{i:x6}", PasswordHash = "x" });
        }
        await context.SaveChangesAsync();

        var user = await new DemoDataService(context).CreateDemoUserAsync(Now);

        Assert.Null(user);
    }

    [Theory]
    [InlineData("demo-1760000000-abc123", true)]
    [InlineData("demo-xyz-abc123", false)]
    [InlineData("demo-", false)]
    [InlineData("maria", false)]
    [InlineData(null, false)]
    public void ParseCreatedAt_ReadsTheTimestampFromTheUsername(string? username, bool valid)
    {
        var parsed = DemoDataService.ParseCreatedAt(username);

        Assert.Equal(valid, parsed != null);
        if (valid) Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1760000000).UtcDateTime, parsed);
    }
}
