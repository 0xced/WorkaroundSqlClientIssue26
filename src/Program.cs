using System;
using System.Linq;
using Database;
using efmssql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Spectre.Console;

var sqlContainer = await AnsiConsole.Status()
    .Spinner(Spinner.Known.OrangePulse)
    .StartAsync("Starting SQL Server container", _ => ChinookContainer.StartAsync(AnsiConsole.WriteLine));

AnsiConsole.WriteLine($"🛢 SQL Server database available on {sqlContainer.ConnectionString}");

var useWorkaround = !args.Contains("--no-workaround");
if (useWorkaround)
{
    AnsiConsole.WriteLine($"✳️ Using issue #26 workaround ({nameof(FixSqlClientIssue26ExecutionStrategy)})");
}

var cancellationMessage = GetCancellationMessage(args);
AnsiConsole.WriteLine($"✋ Cancelling on {cancellationMessage}");

var optionsBuilder = new DbContextOptionsBuilder<ChinookContext>()
    .LogTo(AnsiConsole.WriteLine, [CoreEventId.QueryCanceled, CoreEventId.QueryIterationFailed])
    .UseSqlServer(sqlContainer.ConnectionString, useWorkaround ? sql => sql.WorkaroundSqlClientIssue26() : null);

await using var context = new ChinookContext(optionsBuilder.Options);
AnsiConsole.WriteLine("➡️ await context.Database.EnsureCreatedAsync()");
await context.Database.EnsureCreatedAsync();

using var cancellationEventListener = new CancellationEventListener(cancellationMessage);
var cancellationToken = cancellationEventListener.CancellationToken;

try
{
    AnsiConsole.WriteLine("➡️ context.Tracks.CountAsync(cancellationToken)");
    var count = await context.Tracks.CountAsync(cancellationToken);
    AnsiConsole.WriteLine($"✅ {count} tracks");
    return 0;
}
catch (OperationCanceledException exception) when (exception.CancellationToken == cancellationToken)
{
    AnsiConsole.WriteLine($"⏹️ (wrapped: {(exception.InnerException != null ? "yes" : "no")}) {exception.Message}");
    AnsiConsole.WriteException(exception.InnerException ?? exception, ExceptionFormats.ShortenPaths | ExceptionFormats.ShortenTypes);
    return 130;
}
catch (Exception exception)
{
    AnsiConsole.WriteLine("❌ An unexpected exception occured");
    AnsiConsole.WriteException(exception, ExceptionFormats.ShortenPaths | ExceptionFormats.ShortenTypes);
    return 1;
}

static string GetCancellationMessage(string[] args)
{
    if (args.Length > 0)
    {
        switch (args[0])
        {
            case "TaskCanceledException":
                return "SqlConnection.InternalOpenAsync"; // TaskCanceledException exception with default cancellation token => wrapped: yes
            case "SqlException":
                return "SniTcpHandle.ReceiveAsync"; // SqlException: A severe error occurred on the current command. The results, if any, should be discarded. Operation cancelled by user. => wrapped: yes
            case "InvalidOperationException":
                return "SqlCommand.WriteBeginExecuteEvent"; // InvalidOperationException: Operation cancelled by user. => wrapped: yes
        }
    }

    return "TdsParserStateObjectManaged.ReadAsyncCallback"; // TaskCanceledException exception with proper cancellation token => wrapped: no
}