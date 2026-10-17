namespace Wotr.Domain;

public enum HuntStage { Draw, Table, Guide, Damage, Corruption, Reveal, Done, Replacement, DetermineDamage, BeforeDraw, BeforeRoll, Roll, ShadowReplacement }
public enum HuntReturn { Parent, FinishMove, Stronghold }
public enum HuntTileRule { All, IgnoreEyesAndFellowship, IgnoreEyes }
public enum HuntDamageRule { Normal, CorruptionOnly, RandomCompanionFirst }
public enum HuntPurpose { Hunt, CompanionSeparation }

public sealed class HuntProcedure
{
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int ReportIndex { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int DiceReduction { get; set; }
    public string SourceId { get; set; } = "";
    public HuntStage Stage { get; set; }
    public HuntReturn Return { get; set; }
    public bool Movement { get; set; }
    public bool Rolled { get; set; }
    public bool Mordor { get; set; }
    public HuntTileRule TileRule { get; set; }
    public HuntDamageRule DamageRule { get; set; }
    public HuntPurpose Purpose { get; set; }
    public bool DeclarationHunt { get; set; }
    public bool IgnoreRevealIcon { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public bool FireAndShadow { get; set; }
    public bool CasualtyEffectsPending { get; set; }
    public Wotr.Contracts.Side DrawnBy { get; set; } = Wotr.Contracts.Side.Shadow;
    public int Bonus { get; set; }
    public int RollBonus { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int RerollBonus { get; set; }
    public int EyeDamage { get; set; }
    public List<HuntDie> Dice { get; set; } = [];
    public List<string> RerollSources { get; set; } = [];
    public string TileId { get; set; } = "";
    public int? TileDie { get; set; }
    public int Damage { get; set; }
    public int Healing { get; set; }
    public bool Reveal { get; set; }
    public bool Stop { get; set; }
    public bool CasualtyTaken { get; set; }
    public bool VoluntaryReveal { get; set; }
    public string DepartureOrigin { get; set; } = "";
    public int DepartureProgress { get; set; }
    public List<string> DepartingCompanions { get; set; } = [];
    public bool DeparturePlaced { get; set; }
    public List<string> OfferedGuides { get; set; } = [];
}

public sealed class HuntDie
{
    public string Id { get; set; } = "";
    public int First { get; set; }
    public int Result { get; set; }
    public bool Rerolled { get; set; }
}
