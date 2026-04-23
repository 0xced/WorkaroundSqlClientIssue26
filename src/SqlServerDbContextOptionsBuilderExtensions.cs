using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;

namespace efmssql;

public static class SqlServerDbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Installs an execution strategy that wraps any exception that occurs when cancellation is requested into an <see cref="OperationCanceledException"/>.
    /// This works around <see href="https://github.com/dotnet/SqlClient/issues/26">SqlClient issue #26: Cancelling an async SqlClient operation throws SqlException, not TaskCanceledException</see>.
    /// </summary>
    /// <remarks>
    /// Unlike other <see cref="SqlServerDbContextOptionsBuilder"/> methods, this one can't be chained. This is on purpose, as it properly handles if another execution strategy was already configured.
    /// For example, calling <c>builder.EnableRetryOnFailure().WorkaroundSqlClientIssue26()</c> will properly install both the retrying strategy and the workaround strategy.
    /// </remarks>
    [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Required to wrap a possibly already configured execution strategy")]
    public static void WorkaroundSqlClientIssue26(this SqlServerDbContextOptionsBuilder builder)
    {
        var optionsBuilder = ((IRelationalDbContextOptionsBuilderInfrastructure)builder).OptionsBuilder;
        var executionStrategyFactory = optionsBuilder.Options.FindExtension<SqlServerOptionsExtension>()?.ExecutionStrategyFactory;
        builder.ExecutionStrategy(dependencies => new FixSqlClientIssue26ExecutionStrategy(dependencies, executionStrategyFactory));
    }
}
