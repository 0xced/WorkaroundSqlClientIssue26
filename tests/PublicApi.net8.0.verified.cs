[assembly: System.CLSCompliant(false)]
[assembly: System.Reflection.AssemblyMetadata("RepositoryUrl", "https://github.com/0xced/efmssql")]
[assembly: System.Runtime.Versioning.TargetFramework(".NETCoreApp,Version=v8.0", FrameworkDisplayName=".NET 8.0")]
namespace Microsoft.EntityFrameworkCore
{
    public static class SqlServerDbContextOptionsBuilderWorkaroundSqlClientIssue26Extensions
    {
        public static void WorkAroundSqlClientIssue26(this Microsoft.EntityFrameworkCore.Infrastructure.SqlServerDbContextOptionsBuilder builder) { }
    }
}