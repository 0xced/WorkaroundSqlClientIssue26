using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace WorkaroundSqlClientIssue26.Tests;

public class WorkaroundSqlClientIssue26Test(ITestOutputHelper output, MsSqlFixture fixture) : IClassFixture<MsSqlFixture>
{
    [SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible", Justification = "Required for xUnit")]
    public static TheoryData<string, Type> TestData = new()
    {
        { "SqlConnection.InternalOpenAsync", typeof(TaskCanceledException) },
        { "SqlCommand.BeginExecuteReaderInternalReadStage", typeof(SqlException) },
        { "SqlCommand.WriteBeginExecuteEvent", typeof(InvalidOperationException) },
    };

    [Theory, MemberData(nameof(TestData))]
    public async Task TestWorkaround(string cancellationMessage, Type expectedInnerExceptionType)
        => await ExecuteTestAsync(cancellationMessage, expectedInnerExceptionType, sql => sql.WorkAroundSqlClientIssue26());

    [Theory, MemberData(nameof(TestData))]
    public async Task TestRetryOnFailureAndWorkaround(string cancellationMessage, Type expectedInnerExceptionType)
        => await ExecuteTestAsync(cancellationMessage, expectedInnerExceptionType, sql => sql.EnableRetryOnFailure().WorkAroundSqlClientIssue26());

    private async Task ExecuteTestAsync(string cancellationMessage, Type expectedInnerExceptionType, Action<SqlServerDbContextOptionsBuilder> sqlServerOptionsAction)
    {
        using var cancellationEventListener = new CancellationEventListener(output, cancellationMessage);
        var cancellationToken = cancellationEventListener.CancellationToken;

        await using var context = fixture.CreateChinookContext(output, sqlServerOptionsAction);

        var exception = await Record.ExceptionAsync(() => context.Tracks.CountAsync(cancellationToken));

        output.WriteLine($"Recorded exception: {exception}");
        Assert.Equal(cancellationToken, Assert.IsType<OperationCanceledException>(exception, exactMatch: false).CancellationToken);
        if (expectedInnerExceptionType != typeof(SqlException) || exception.InnerException != null)
        {
            // Getting a SqlException is not 100% reliable, sometimes we get a TaskCanceledException instead.
            Assert.IsType(expectedInnerExceptionType, exception.InnerException);
        }
    }
}