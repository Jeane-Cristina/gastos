using System.Net;
using GastosApi.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GastosApi.Tests.Integration;

/// <summary>
/// Sobe a API inteira (Program.cs, middlewares, autenticação JWT, rate limit, controllers) dentro do
/// processo de teste. A única troca é o banco: PostgreSQL vira SQLite em memória.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    // Variáveis de ambiente têm prioridade sobre o appsettings.json e já existem quando o Program.cs lê a configuração.
    static ApiFactory()
    {
        Environment.SetEnvironmentVariable("Jwt__Key", "chave-de-teste-com-pelo-menos-32-caracteres-ok");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "GastosApi");
        Environment.SetEnvironmentVariable("Jwt__ExpireMinutes", "60");
        Environment.SetEnvironmentVariable("Resend__ApiToken", "token-de-teste");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Host=nao-usado-nos-testes");
    }

    public ApiFactory()
    {
        // a conexão em memória precisa ficar aberta, senão o banco some
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // remove tudo que o AddDbContext(UseNpgsql) registrou para o AppDbContext
            var toRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                            || (d.ServiceType.IsGenericType && d.ServiceType.GenericTypeArguments.Contains(typeof(AppDbContext))))
                .ToList();
            foreach (var descriptor in toRemove) services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));

            // deixa cada teste "chegar" de um IP diferente (cabeçalho X-Test-Ip), para o rate limit por IP não misturar testes
            services.AddSingleton<IStartupFilter, TestClientIpStartupFilter>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}

/// <summary>Define o IP do cliente a partir do cabeçalho X-Test-Ip, antes do middleware de rate limit.</summary>
public class TestClientIpStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
        app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                if (context.Request.Headers.TryGetValue("X-Test-Ip", out var header) && IPAddress.TryParse(header.ToString(), out var address))
                {
                    context.Connection.RemoteIpAddress = address;
                }

                await nextMiddleware();
            });

            next(app);
        };
}
