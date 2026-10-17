namespace Wotr.Domain;

public sealed record SovereignDefinition(string Identity, string Title, string Nation, string Home, int Level, int Resistance)
{
    public string Id => $"kings.sovereign.{Identity}";
    public string AwakenedCard => $"kings.character.{Identity}.awakened";
    public string CorruptedCard => $"kings.character.{Identity}.corrupted";
}

public static class SovereignCatalog
{
    public const string Brand = "kings.sovereign.brand";
    public const string Dain = "kings.sovereign.dain";
    public const string Denethor = "kings.sovereign.denethor";
    public const string Theoden = "kings.sovereign.theoden";
    public const string Thranduil = "kings.sovereign.thranduil";
    public const string RulerSource = "ruler";

    public static IReadOnlyDictionary<string, SovereignDefinition> All { get; } = new SovereignDefinition[]
    {
        new("brand", "Brand", "north", "dale", 1, 2),
        new("dain", "Dáin Ironfoot", "dwarves", "erebor", 1, 4),
        new("denethor", "Denethor", "gondor", "minas-tirith", 1, 3),
        new("theoden", "Théoden", "rohan", "edoras", 2, 3),
        new("thranduil", "Thranduil", "elves", "woodland-realm", 2, 4)
    }.ToDictionary(d => d.Id, StringComparer.Ordinal);
}
