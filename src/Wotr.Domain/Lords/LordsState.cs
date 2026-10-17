using Wotr.Contracts;

namespace Wotr.Domain;

public sealed class LordsState
{
    public List<string> CaptainDice { get; set; } = [];
    public int LoreOfferedTurn { get; set; }
    public CouncilState? Council { get; set; }
    public bool WitchKingReplaced { get; set; }
    public bool EvilKnowledgeUsed { get; set; }
    public bool SmeagolLeft { get; set; }
    public bool GollumEntered { get; set; }
    public Dictionary<string, string> RevealedCompanionVersions { get; set; } = [];
    public Dictionary<Side, int> RolledCounts { get; set; } = [];
    public List<Side> ChoiceOrder { get; set; } = [];
    public List<string> PaidWillOfWestIds { get; set; } = [];
    public LordsActionCompletion? ActionCompletion { get; set; }
}

public sealed class LordsActionCompletion
{
    public bool FellowshipMove { get; set; }
}

public sealed class SpecialDieState
{
    public bool Chosen { get; set; }
    public bool RemoveSymbol { get; set; }
    public bool PermanentlyRemoved { get; set; }
}
