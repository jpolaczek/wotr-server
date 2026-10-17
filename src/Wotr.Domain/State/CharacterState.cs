using Wotr.Contracts;

namespace Wotr.Domain;

// Id identifies the physical figure; DefinitionId identifies its current card/version.
public sealed class CharacterState
{
    public string Id { get; set; } = "";
    public string DefinitionId { get; set; } = "";
    public Side Side { get; set; }
    public string Location { get; set; } = "reserve";
    public bool Garrison { get; set; }
    public int Level { get; set; }
    public int Leadership { get; set; }
    public string NationIcon { get; set; } = "";
    public int LastArmyConversionTurn { get; set; } = -1;
}
