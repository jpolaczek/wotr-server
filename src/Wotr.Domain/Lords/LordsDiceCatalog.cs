using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record SpecialDieFace(DieFace Face, bool Remove = false);
public sealed record SpecialDieDefinition(string Id, Side Owner, string CharacterId, IReadOnlyList<SpecialDieFace> Faces);

public static class LordsDiceCatalog
{
    public const string Vilya = "lords.die.vilya";
    public const string Nenya = "lords.die.nenya";
    public const string Narya = "lords.die.narya";
    public const string Balrog = "lords.die.balrog";
    public const string Gothmog = "lords.die.gothmog";
    public const string KeeperGandalf = "lords.character.gandalf-grey";
    public const string ChiefWitchKing = "lords.character.witch-king";
    public const string KeeperEye = "keeper-eye";
    public const string LesserEye = "lesser-eye";

    // LORDS pp9/11, all six printed faces in left-to-right order.
    public static IReadOnlyDictionary<string, SpecialDieDefinition> Definitions { get; } = new SpecialDieDefinition[]
    {
        new(Vilya, Side.FreePeoples, "lords.character.elrond",
            [new(DieFace.Army, true), new(DieFace.Muster), new(DieFace.Event), new(DieFace.CardDraw), new(DieFace.CardDraw), new(DieFace.Eye, true)]),
        new(Nenya, Side.FreePeoples, "lords.character.galadriel",
            [new(DieFace.Character, true), new(DieFace.Muster), new(DieFace.Event), new(DieFace.CardDraw), new(DieFace.CardDraw), new(DieFace.Eye, true)]),
        new(Narya, Side.FreePeoples, KeeperGandalf,
            [new(DieFace.Character), new(DieFace.Muster), new(DieFace.Event), new(DieFace.CardDraw), new(DieFace.Army), new(DieFace.Eye)]),
        new(Balrog, Side.Shadow, "lords.character.balrog",
            [new(DieFace.Character, true), new(DieFace.UseBalrog), new(DieFace.UseBalrog), new(DieFace.Muster), new(DieFace.CardDraw), new(DieFace.Eye, true)]),
        new(Gothmog, Side.Shadow, "lords.character.gothmog",
            [new(DieFace.Army, true), new(DieFace.Army), new(DieFace.Muster), new(DieFace.Muster), new(DieFace.CardDraw), new(DieFace.Eye, true)])
    }.ToDictionary(d => d.Id);
}
