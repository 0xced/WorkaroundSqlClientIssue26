using System;
using Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace WorkaroundSqlClientIssue26.Tests;

public partial class MsSqlFixture
{
    public ChinookContext CreateChinookContext(ITestOutputHelper output, Action<SqlServerDbContextOptionsBuilder> sqlServerOptionsAction)
    {
        var options = new DbContextOptionsBuilder<ChinookContext>()
            .LogTo(output.WriteLine, [CoreEventId.QueryCanceled, CoreEventId.QueryIterationFailed])
            .UseSqlServer(ConnectionString, sqlServerOptionsAction)
            .Options;

        return new ChinookContext(options);
    }
}