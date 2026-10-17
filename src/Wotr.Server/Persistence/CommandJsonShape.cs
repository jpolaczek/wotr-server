using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Wotr.Domain.Content;

namespace Wotr.Server;

internal sealed class CommandJsonShape
{
    private static readonly ConcurrentDictionary<Type, CommandJsonShape> Cache = new();
    private readonly JsonElement defaults;
    public IReadOnlyDictionary<string, Type> Fields { get; }

    private CommandJsonShape(Type type)
    {
        defaults = JsonSerializer.SerializeToElement(Activator.CreateInstance(type), type, BoardCatalog.Json);
        Fields = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead && p.CanWrite)
            .ToDictionary(p => JsonNamingPolicy.CamelCase.ConvertName(p.Name), p => p.PropertyType);
    }

    public static CommandJsonShape For(Type type) => Cache.GetOrAdd(type, t => new(t));

    public bool IsDefault(JsonProperty property) => defaults.TryGetProperty(property.Name, out var expected)
        && JsonElement.DeepEquals(property.Value, expected);
}
