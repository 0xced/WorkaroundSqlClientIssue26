using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace efmssql;

/// <summary>
/// An execution strategy that wraps any exception that occurs when cancellation is requested into an <see cref="OperationCanceledException"/>.
/// This works around <see href="https://github.com/dotnet/SqlClient/issues/26">SqlClient issue #26: Cancelling an async SqlClient operation throws SqlException, not TaskCanceledException</see>.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public class FixSqlClientIssue26ExecutionStrategy(ExecutionStrategyDependencies dependencies) : SqlServerExecutionStrategy(dependencies)
{
    private static readonly string OperationCanceledMessage = "The operation was canceled.";

    static FixSqlClientIssue26ExecutionStrategy()
    {
        try
        {
            new CancellationToken(canceled: true).ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException exception)
        {
            OperationCanceledMessage = exception.Message;
        }
    }

    public static IExecutionStrategy Create(ExecutionStrategyDependencies dependencies) => new FixSqlClientIssue26ExecutionStrategy(dependencies);

    public override async Task<TResult> ExecuteAsync<TState, TResult>(TState state, Func<DbContext, TState, CancellationToken, Task<TResult>> operation, Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>>? verifySucceeded, CancellationToken cancellationToken)
    {
        try
        {
            return await base.ExecuteAsync(state, operation, verifySucceeded, cancellationToken);
        }
        catch (Exception exception) when (ShouldWrap(exception, cancellationToken))
        {
            throw new OperationCanceledException(OperationCanceledMessage, exception, cancellationToken);
        }
    }

    private static bool ShouldWrap(Exception exception, CancellationToken cancellationToken)
    {
        return exception switch
        {
            OperationCanceledException oce when oce.CancellationToken == cancellationToken => false,
            _ => cancellationToken.IsCancellationRequested,
        };
    }
}
