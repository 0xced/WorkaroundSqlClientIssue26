using System;
using System.IO;
using System.Runtime.CompilerServices;
using EFCore.Scaffolding;
using Microsoft.Data.SqlClient;
using WorkaroundSqlClientIssue26.Tests;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

var fixture = new MsSqlFixture(new LoggerSink());
await ((IAsyncLifetime)fixture).InitializeAsync();
var settings = new ScaffolderSettings(new SqlConnectionStringBuilder(fixture.ConnectionString))
{
    OutputDirectory = GetOutputDirectory(),
    ContextName = "ChinookContext",
    GetDisplayableConnectionString = builder =>
    {
        ((SqlConnectionStringBuilder)builder).DataSource = "localhost";
        return builder.ConnectionString;
    },
};
Scaffolder.Run(settings);
return;

static DirectoryInfo GetOutputDirectory([CallerFilePath] string path = "") => new(Path.Combine(Path.GetDirectoryName(path)!, "..", "..", "tests", "Database"));

internal class LoggerSink : IMessageSink
{
    public bool OnMessage(IMessageSinkMessage message)
    {
        if (message is DiagnosticMessage diagnosticMessage)
        {
            Console.WriteLine(diagnosticMessage.Message);
        }

        return true;
    }
}