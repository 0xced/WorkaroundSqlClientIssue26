Work around Microsoft SqlClient issue #26 for EF Core

[![NuGet](https://img.shields.io/nuget/v/WorkaroundSqlClientIssue26.svg?label=NuGet&logo=NuGet)](https://www.nuget.org/packages/WorkaroundSqlClientIssue26/) [![Continuous Integration](https://img.shields.io/github/actions/workflow/status/0xced/WorkaroundSqlClientIssue26/continuous-integration.yml?branch=main&label=Continuous%20Integration&logo=GitHub)](https://github.com/0xced/WorkaroundSqlClientIssue26/actions/workflows/continuous-integration.yml) [![Coverage](https://img.shields.io/codecov/c/github/0xced/WorkaroundSqlClientIssue26?label=Coverage&logo=Codecov&logoColor=f5f5f5)](https://codecov.io/gh/0xced/WorkaroundSqlClientIssue26)

Microsoft's SqlClient has a long standing issue (#26): [Cancelling an async SqlClient operation throws SqlException, not TaskCanceledException](https://github.com/dotnet/SqlClient/issues/26)

It is problematic because whenever an SQL command is canceled, an `InvalidOperationException`, a `SqlException` or a  `TaskCanceledException` will be randomly thrown . This is also a showstopper when using [Blazor virtualization](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/virtualization#item-provider-delegate) where an `OperationCanceledException` is [expected to signal cancelation](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Components/Web/src/Virtualization/Virtualize.cs#L441-L444).

This project provides a workaround that leverages EF Core [execution strategies](https://learn.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency). When using this workaround, querying the db context will consistently throw an `OperationCanceledException` upon cancelation.

```c#
try
{
    await dbContext.Tracks.CountAsync(cancellationToken);
}
catch (OperationCanceledException exception) when (exception.CancellationToken == cancellationToken)
{
    // Alwas caught with the workaround applied. Not caught when using SqlClient default behavior.
}
```

## Getting started

Add the [WorkaroundSqlClientIssue26](https://www.nuget.org/packages/WorkaroundSqlClientIssue26/) NuGet package to your project using the NuGet Package Manager or run the following command:

```sh
dotnet add package WorkaroundSqlClientIssue26
```

Install the workaround when configuring the db context options.

```c#
var options = new DbContextOptionsBuilder<MyDbContext>()
    .UseSqlServer(connectionString, sql => sql.WorkAroundSqlClientIssue26())
    .Options;
```

This workaround is also compatible with a [custom execution strategy](https://learn.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency#custom-execution-strategy) such as retry on failure.

```c#
var options = new DbContextOptionsBuilder<MyDbContext>()
    .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure().WorkAroundSqlClientIssue26())
    .Options;
```

## Frequently Asked Questions

* What are the supported EF Core versions?
  * EF Core 8, 9 and 10 are supported. Future versions should work too.

* What happens if the original issue gets fixed?
  * If SqlClient issue #26 is fixed, this workaround will turn into a no-op.
