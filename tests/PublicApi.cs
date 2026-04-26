using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using PublicApiGenerator;
using VerifyXunit;
using Xunit;

namespace WorkaroundSqlClientIssue26.Tests;

public class PublicApi
{
    [Theory]
    [ClassData(typeof(TargetFrameworksTheoryData))]
    public Task ApprovePublicApi(string targetFramework)
    {
        var testAssembly = typeof(PublicApi).Assembly;
        var configuration = testAssembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration.ToLowerInvariant()
                            ?? throw new Exception($"{nameof(AssemblyConfigurationAttribute)} not found in {testAssembly.Location}");
        var assemblyPath = Path.Combine(GetRootDirectoryPath(), "artifacts", "bin", "WorkaroundSqlClientIssue26", configuration, "WorkaroundSqlClientIssue26.dll");
        var assembly = Assembly.LoadFile(assemblyPath);
        var publicApi = assembly.GeneratePublicApi(new ApiGeneratorOptions { DenyNamespacePrefixes = [] });
        return Verifier.Verify(publicApi, "cs").UseFileName($"PublicApi.{targetFramework}");
    }

    private static string GetRootDirectoryPath([CallerFilePath] string path = "") => Path.Combine(Path.GetDirectoryName(path)!, "..");

    private class TargetFrameworksTheoryData : TheoryData<string>
    {
        public TargetFrameworksTheoryData()
        {
            var csprojPath = Path.Combine(GetRootDirectoryPath(), "src", "WorkaroundSqlClientIssue26.csproj");
            var project = XDocument.Load(csprojPath);
            var targetFrameworks = project.XPathSelectElement("/Project/PropertyGroup/TargetFrameworks")?.Value.Split(';', StringSplitOptions.RemoveEmptyEntries)
                                   ?? [project.XPathSelectElement("/Project/PropertyGroup/TargetFramework")?.Value ?? throw new Exception($"TargetFramework(s) element not found in {csprojPath}")];
            AddRange(targetFrameworks);
        }
    }
}