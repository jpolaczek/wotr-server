namespace Wotr.Domain;

public enum FellowshipProcedureKind { Reveal, Guide, Separation, Declaration }
public enum FellowshipReturn { Decision, Parent, FinishAction, EndPhase }

public sealed class FellowshipProcedure
{
    public FellowshipProcedureKind Kind { get; set; }
    public FellowshipReturn Return { get; set; }
    public string SourceId { get; set; } = "";
    public List<string> Route { get; set; } = [];
    public List<string> AdditionalHuntRegions { get; set; } = [];
    public bool AwaitingHunts { get; set; }
    public bool RevealEffectsPending { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public bool FireAndShadowPending { get; set; }
    public List<string> FixedCompanions { get; set; } = [];
    public string SeparationOrigin { get; set; } = "";
    public int SeparationRange { get; set; }
    public int SeparationBonus { get; set; }
    public CompanionJourney? Journey { get; set; }
    public bool ChooseGuideAfterPlacement { get; set; }
}

public sealed class CompanionJourney
{
    public bool AllowSeparation { get; set; } = true;
    public bool AllowNoMovement { get; set; }
    public int MaximumGroups { get; set; } = 1;
    public int? FixedLevel { get; set; }
    public int Bonus { get; set; }
    public bool AllowBesiegedDestination { get; set; }
}
