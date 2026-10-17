using Wotr.Contracts;

namespace Wotr.Domain;

// These persistence models are internal server data, never endpoint response DTOs.
public sealed class MatchState
{
    [System.Text.Json.Serialization.JsonRequired]
    public int SchemaVersion { get; set; } = StateCodec.CurrentVersion;
    [System.Text.Json.Serialization.JsonRequired]
    public string RulesVersion { get; set; } = Protocol.RulesVersion;
    [System.Text.Json.Serialization.JsonRequired]
    public string ContentVersion { get; set; } = Protocol.ContentVersion;
    public string Id { get; set; } = "";
    public long Revision { get; set; }
    public long DecisionSequence { get; set; }
    public int Turn { get; set; }
    public GamePhase Phase { get; set; } = GamePhase.Setup;
    public Side Actor { get; set; }
    public Side? Winner { get; set; }
    public VictoryCondition? Victory { get; set; }
    public MatchConfiguration Configuration { get; set; } = new();
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public LordsState? Lords { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public KingsState? Kings { get; set; }
    public List<PlayerState> Players { get; set; } = [];
    public List<UnitState> Units { get; set; } = [];
    public List<CharacterState> Characters { get; set; } = [];
    public bool MinionEverEntered { get; set; }
    public List<NationState> Nations { get; set; } = [];
    public List<PoliticalReaction> PoliticalReactions { get; set; } = [];
    public Dictionary<string, Side?> Control { get; set; } = [];
    public List<DieState> Dice { get; set; } = [];
    public List<RingState> Rings { get; set; } = [];
    public FellowshipState Fellowship { get; set; } = new();
    public List<TileState> HuntTiles { get; set; } = [];
    public PendingDecision? Pending { get; set; }
    public DiscardContinuation AfterDiscard { get; set; }
    public ResolutionState Resolution { get; set; } = new();
    public BattleView? LastBattleRound { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public HuntReportView? HuntReport { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public long HuntReportSequence { get; set; }
    public List<GameEvent> Events { get; set; } = [];
    [System.Text.Json.Serialization.JsonIgnore]
    internal HistoryCapture? HistoryCapture { get; set; }
    public List<ActiveEffectState> ActiveEffects { get; set; } = [];
    public List<RandomSample> RandomSamples { get; set; } = [];
    public bool MandatoryHuntAllocation { get; set; }
    public PlayerState Player(Side side) => Players.Single(p => p.Side == side);
    public void Open(DecisionKind kind, Side owner, IEnumerable<CommandKind> commands, int count = 0)
        => Pending = new()
        {
            Id = $"{Id}:d{++DecisionSequence}",
            Kind = kind,
            Owner = owner,
            Commands = commands.ToList(),
            RequiredCount = count
        };
}
