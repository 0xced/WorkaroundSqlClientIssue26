using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Testcontainers.MsSql;

namespace efmssql;

public sealed class ChinookContainer : IAsyncDisposable
{
    private static readonly HttpClient HttpClient = new();

    private readonly MsSqlContainer _container;

    private ChinookContainer(MsSqlContainer container, string connectionString)
    {
        _container = container;
        ConnectionString = connectionString;
    }

    public static async Task<ChinookContainer> StartAsync(Action<string>? logWriter = null)
    {
        var sqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithName("efmssql")
            .WithLogger(new TestcontainersLogger(logWriter))
            .WithReuse(true)
            .Build();

        await sqlContainer.StartAsync();

        var chinookScript = await HttpClient.GetStringAsync("https://github.com/lerocha/chinook-database/releases/download/v1.4.5/Chinook_SqlServer_AutoIncrementPKs.sql");
        var result = await sqlContainer.ExecScriptAsync(chinookScript);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Execution of Chinook_SqlServer_AutoIncrementPKs.sql failed ({result.ExitCode})\n{result.Stdout}\n{result.Stderr}");
        }

        var connectionStringBuilder = new SqlConnectionStringBuilder(sqlContainer.GetConnectionString()) { InitialCatalog = "Chinook_AutoIncrement" };

        return new ChinookContainer(sqlContainer, connectionStringBuilder.ConnectionString);
    }

    public string ConnectionString { get; }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();

    private class TestcontainersLogger(Action<string>? write) : ILogger
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => write?.Invoke($"🐳 {formatter(state, exception)}");

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }
}