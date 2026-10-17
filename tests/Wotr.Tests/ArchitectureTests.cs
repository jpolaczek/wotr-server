using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public class ArchitectureTests
{
    [Fact]
    public void DomainAndContractsDoNotDependOnRenderingNetworkingOrStorage()
    {
        foreach (var assembly in new[] { typeof(DomainAssembly).Assembly, typeof(Protocol).Assembly })
            foreach (var reference in assembly.GetReferencedAssemblies())
                Assert.DoesNotContain(new[] { "Unity", "Npgsql", "Microsoft.AspNetCore", "Wotr.Server" },
                    forbidden => reference.Name!.StartsWith(forbidden, StringComparison.Ordinal));
    }
}
