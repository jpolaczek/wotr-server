using Wotr.Contracts;

namespace Wotr.Domain;

// These persistence models are internal server data, never endpoint response DTOs.
public sealed class FellowshipState
{
    public string RegionId { get; set; } = "rivendell";
    public int Progress { get; set; }
    public int Corruption { get; set; }
    public bool Hidden { get; set; } = true;
    public int? MordorStep { get; set; }
    public int LastDeclarationTurn { get; set; } = -1;
    public int LastMordorAttemptTurn { get; set; } = -1;
    public string Guide { get; set; } = "base.character.gandalf-grey";
    public List<string> Companions { get; set; } = ["base.character.gandalf-grey", "base.character.strider", "base.character.boromir", "base.character.gimli", "base.character.legolas", "base.character.merry", "base.character.pippin"];
}
