using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace WorkaroundSqlClientIssue26.Tests;

public sealed class RecordRetryingStrategy(ExecutionStrategyDependencies dependencies) : SqlServerRetryingExecutionStrategy(dependencies)
{
    public readonly List<bool> ShouldRetry = [];

    protected override bool ShouldRetryOn(Exception exception)
    {
        var result = base.ShouldRetryOn(exception);
        ShouldRetry.Add(result);
        return result;
    }
}