using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace efmssql;

[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Required to workaround SqlClient issue #26")]
internal class FixSqlClientIssue26ExecutionStrategy(ExecutionStrategyDependencies dependencies, Func<ExecutionStrategyDependencies, IExecutionStrategy>? executionStrategyFactory)
    : SqlServerExecutionStrategy(dependencies)
{
    private readonly IExecutionStrategy? _executionStrategy = executionStrategyFactory?.Invoke(dependencies);

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

    public override async Task<TResult> ExecuteAsync<TState, TResult>(TState state, Func<DbContext, TState, CancellationToken, Task<TResult>> operation, Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>>? verifySucceeded, CancellationToken cancellationToken)
    {
        try
        {
            if (_executionStrategy != null)
            {
                return await _executionStrategy.ExecuteAsync(state, operation, verifySucceeded, cancellationToken);
            }

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
