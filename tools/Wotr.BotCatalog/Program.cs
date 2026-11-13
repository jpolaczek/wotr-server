using System.Reflection;
using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

if (args.Length != 1)
    throw new ArgumentException("Usage: Wotr.BotCatalog <output.json>");

var flags = typeof(MatchConfiguration).GetProperties().OrderBy(p => p.Name, StringComparer.Ordinal).ToArray();
if (flags.Any(p => p.PropertyType != typeof(bool) && p.PropertyType != typeof(bool?)))
    throw new InvalidOperationException("Configuration gained a non-boolean option; extend the exhaustive exporter.");
var configurations = new SortedDictionary<string, object>(StringComparer.Ordinal);
var inputs = new List<object>();
foreach (var input in Enumerate(flags, 0, new MatchConfiguration()))
{
    var normalized = ConfigurationRules.Normalize(input);
    var key = string.Join("", flags.Select(p => (bool)p.GetValue(normalized)! ? "1" : "0"));
    var conflict = ConfigurationRules.Conflict(input);
    inputs.Add(new { flags = Values(input), configuration = key, conflict });
    if (conflict is null)
        configurations[key] = new
        {
            id = key,
            flags = Values(normalized),
            activeCards = CardCatalog.All.Keys.Where(id => CardCatalog.Enabled(normalized, id)).Order(StringComparer.Ordinal)
        };
}

var shapes = new SortedDictionary<string, object>(StringComparer.Ordinal);
Describe(typeof(GameCommand));
var export = new
{
    schemaVersion = 1,
    decisions = Enum.GetNames<DecisionKind>(),
    commands = Enum.GetNames<CommandKind>(),
    phases = Enum.GetNames<GamePhase>(),
    victories = Enum.GetNames<VictoryCondition>(),
    costs = Enum.GetNames<DieFace>(),
    shapes,
    configurationFlags = flags.Select(p => p.Name),
    configurationInputs = inputs,
    configurations = configurations.Values,
    cards = CardCatalog.All.Values.OrderBy(c => c.Id, StringComparer.Ordinal)
        .Select(c => new { c.Id, side = c.Side.ToString(), c.Deck, c.Replaces }),
    characters = CharacterCatalog.All.Values.OrderBy(c => c.Id, StringComparer.Ordinal)
        .Select(c => new { c.Id, side = c.Side.ToString(), c.Abilities })
};
File.WriteAllText(args[0], JsonSerializer.Serialize(export, new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
}) + "\n");

Dictionary<string, object?> Values(MatchConfiguration config) => flags.ToDictionary(p => p.Name, p => p.GetValue(config));

IEnumerable<MatchConfiguration> Enumerate(PropertyInfo[] properties, int index, MatchConfiguration current)
{
    if (index == properties.Length)
    {
        yield return current;
        yield break;
    }
    var property = properties[index];
    foreach (var value in property.PropertyType == typeof(bool?) ? new object?[] { null, false, true } : new object?[] { false, true })
    {
        property.SetValue(current, value);
        foreach (var result in Enumerate(properties, index + 1, current))
            yield return result;
    }
}

void Describe(Type type)
{
    type = Nullable.GetUnderlyingType(type) ?? type;
    if (type.IsGenericType)
    {
        foreach (var argument in type.GetGenericArguments())
            Describe(argument);
        return;
    }
    if (type.Assembly != typeof(GameCommand).Assembly || shapes.ContainsKey(type.Name))
        return;
    if (type.IsEnum)
    {
        shapes[type.Name] = Enum.GetNames(type);
        return;
    }
    var properties = type.GetProperties().OrderBy(p => p.Name, StringComparer.Ordinal).ToArray();
    shapes[type.Name] = properties.ToDictionary(p => p.Name, p => TypeName(p.PropertyType));
    foreach (var property in properties)
        Describe(property.PropertyType);
}

string TypeName(Type type)
{
    if (Nullable.GetUnderlyingType(type) is { } underlying)
        return TypeName(underlying) + "?";
    if (type.IsGenericType)
        return type.Name.Split('`')[0] + "<" + string.Join(",", type.GetGenericArguments().Select(TypeName)) + ">";
    return type.Name;
}
