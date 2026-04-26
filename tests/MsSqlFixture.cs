using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Testcontainers.Xunit;
using Xunit.Sdk;

namespace WorkaroundSqlClientIssue26.Tests;

public partial class MsSqlFixture(IMessageSink messageSink) : ContainerFixture<MsSqlBuilder, MsSqlContainer>(messageSink)
{
    private static readonly HttpClient HttpClient = new();

    public string ConnectionString => new SqlConnectionStringBuilder(Container.GetConnectionString()) { InitialCatalog = "Chinook_AutoIncrement" }.ConnectionString;

    protected override MsSqlBuilder Configure() => new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-latest").WithReuse(true).WithName("WorkaroundSqlClientIssue26.Tests");

    protected override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        var chinookScript = await HttpClient.GetStringAsync("https://github.com/lerocha/chinook-database/releases/download/v1.4.5/Chinook_SqlServer_AutoIncrementPKs.sql");
        var result = await Container.ExecScriptAsync(chinookScript);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Execution of Chinook_SqlServer_AutoIncrementPKs.sql failed ({result.ExitCode})\n{result.Stdout}\n{result.Stderr}");
        }
    }

    // Do not stop the container for faster testing iterations
    protected override ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;
}