using System.Reflection;
using System.Runtime.Loader;

namespace Wotr.Server;

internal sealed class BotRuntimeLoadContext(string assemblyPath) : AssemblyLoadContext(isCollectible: true)
{
    private readonly AssemblyDependencyResolver resolver = new(assemblyPath);

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var path = resolver.ResolveAssemblyToPath(assemblyName);
        if (path is not null)
            return LoadFromAssemblyPath(path);
        // A missing private rule dependency must never bind to the live host's
        // Contracts/Domain/Server assemblies via the default load context.
        if (assemblyName.Name?.StartsWith("Wotr.", StringComparison.Ordinal) is true)
            throw new FileNotFoundException("Runtime dependency is missing.");
        return null;
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName) =>
        throw new NotSupportedException("Native runtime dependencies are not supported.");
}
