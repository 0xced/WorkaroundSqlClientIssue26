using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Microsoft.EntityFrameworkCore.Storage;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore;

/// <summary>
/// Provides extension methods for <see cref="SqlServerDbContextOptionsBuilder"/>.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Required to workaround SqlClient issue #26")]
public static class SqlServerDbContextOptionsBuilderWorkaroundSqlClientIssue26Extensions
{
    /// <summary>
    /// Installs an execution strategy that wraps any exception that occurs when cancellation is requested into an <see cref="OperationCanceledException"/>.
    /// This works around <see href="https://github.com/dotnet/SqlClient/issues/26">SqlClient issue #26: Cancelling an async SqlClient operation throws SqlException, not TaskCanceledException</see>.
    /// </summary>
    /// <remarks>
    /// Unlike other <see cref="SqlServerDbContextOptionsBuilder"/> methods, this one can't be chained. This is on purpose, as it properly handles if another execution strategy was already configured.
    /// For example, calling <c>builder.EnableRetryOnFailure().WorkAroundSqlClientIssue26()</c> will properly install both the retrying strategy and the workaround strategy.
    /// </remarks>
    public static void WorkAroundSqlClientIssue26(this SqlServerDbContextOptionsBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var optionsBuilder = ((IRelationalDbContextOptionsBuilderInfrastructure)builder).OptionsBuilder;
        var executionStrategyFactory = optionsBuilder.Options.GetExtension<SqlServerOptionsExtension>().ExecutionStrategyFactory;
        builder.ExecutionStrategy(dependencies => new FixSqlClientIssue26ExecutionStrategy(dependencies, executionStrategyFactory));
    }

    private sealed class FixSqlClientIssue26ExecutionStrategy(ExecutionStrategyDependencies dependencies, Func<ExecutionStrategyDependencies, IExecutionStrategy>? executionStrategyFactory)
        : SqlServerExecutionStrategy(dependencies)
    {
        private readonly IExecutionStrategy? _executionStrategy = executionStrategyFactory?.Invoke(dependencies);

        private static readonly string OperationCanceledMessage = new OperationCanceledException().Message;

        public override async Task<TResult> ExecuteAsync<TState, TResult>(TState state, Func<DbContext, TState, CancellationToken, Task<TResult>> operation,
            Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>>? verifySucceeded, CancellationToken cancellationToken)
        {
            try
            {
                if (_executionStrategy != null)
                {
                    return await _executionStrategy.ExecuteAsync(state, operation, verifySucceeded, cancellationToken).ConfigureAwait(false);
                }

                return await base.ExecuteAsync(state, operation, verifySucceeded, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when ((exception is not OperationCanceledException oce || oce.CancellationToken != cancellationToken) && cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(OperationCanceledMessage, exception, cancellationToken);
            }
        }
    }
}
