using System.Collections.Generic;

namespace Wotr.Contracts
{
    // Ordinary player information at a reviewed phase/action window.
    // Null means that this version does not describe the current continuation.
    // No save frames, private opposing choices, deck order or entropy belong here.
    public sealed class ActionBoundaryView
    {
        public int Version { get; set; } = 19;
        public bool FellowshipDeclaredThisTurn { get; set; }
        public bool MinionEverEntered { get; set; }
        public bool MandatoryHuntAllocation { get; set; }
        public List<string> Companions { get; set; } = new List<string>();
        public List<ActionCharacterView> Characters { get; set; } = new List<ActionCharacterView>();
        public List<ActionPlayerView> Players { get; set; } = new List<ActionPlayerView>();
        public List<HuntTileZoneView> HuntTiles { get; set; } = new List<HuntTileZoneView>();
        public List<string> PaidDieIds { get; set; } = new List<string>();
        public List<PendingPoliticsView> PendingPolitics { get; set; } = new List<PendingPoliticsView>();
        public FellowshipMoveView? FellowshipMove { get; set; }
        public CardHuntView? CardHunt { get; set; }
        public FellowshipDeclarationView? FellowshipDeclaration { get; set; }
        public BattleContextView? Battle { get; set; }
        public LureChoiceView? LureChoice { get; set; }
        public CompanionJourneyView? CompanionJourney { get; set; }
        public FellowshipGuideView? FellowshipGuide { get; set; }
        public LordsRuleFactsView? Lords { get; set; }
        public KingsRuleFactsView? Kings { get; set; }
    }

    // Public choices and consequences already reached in a reviewed Hunt context.
    // The current roll, tile, damage and choice remain in HuntView/DecisionView.
    // Absence never means that an arbitrary Hunt has this return context.
    public class HuntProgressView
    {
        public List<string> GuideWindowsOffered { get; set; } = new List<string>();
        public bool CasualtyConsequencesPending { get; set; }
        public bool VoluntaryReveal { get; set; }
        public string DepartureRegionId { get; set; } = "";
        public int DepartureProgress { get; set; }
        public List<string> DepartingCompanions { get; set; } = new List<string>();
        public bool PlacementStarted { get; set; }
    }

    public sealed class FellowshipMoveView : HuntProgressView
    {
        public FellowshipStrongholdView? StrongholdHunts { get; set; }
    }

    // A declaration already committed in the Fellowship phase. Its public route
    // may cause a Balrog Hunt and, if revealed, additional Stronghold Hunts.
    // Completion returns to the ordinary Fellowship phase; no saved token is sent.
    public sealed class FellowshipDeclarationView : HuntProgressView
    {
        public List<string> Route { get; set; } = new List<string>();
        public List<string> RemainingStrongholds { get; set; } = new List<string>();
    }

    // Semantic return fact for a direct paid attack. PlayerView.Battle supplies
    // the public combat state; this does not serialize its resolution frame.
    public sealed class BattleContextView
    {
        public bool CompletesPaidAction { get; set; }
        public bool SpecialAttackLoss { get; set; }
    }

    // Public consequences of the committed reveal route after a paid move.
    // MovementHunt has finished resolving its tile; its payment is still open.
    public sealed class FellowshipStrongholdView
    {
        public HuntView MovementHunt { get; set; } = new HuntView();
        public List<string> Route { get; set; } = new List<string>();
        public List<string> RemainingStrongholds { get; set; } = new List<string>();
    }

    // Public events whose political consequences wait until the action ends.
    public sealed class PendingPoliticsView
    {
        public string Cause { get; set; } = "";
        public string NationId { get; set; } = "";
        public string RegionId { get; set; } = "";
    }

    public sealed class ActionCharacterView
    {
        public CharacterView Character { get; set; } = new CharacterView();
        public bool ConvertedArmyThisTurn { get; set; }
    }

    public sealed class ActionPlayerView
    {
        public Side Side { get; set; }
        public int ExtraDiceNextRecovery { get; set; }
        public bool RingActionPending { get; set; }
        // Own discards and publicly played/discarded opposing cards only.
        public List<string> KnownDiscards { get; set; } = new List<string>();
        public List<string> PublicDiscards { get; set; } = new List<string>();
    }

    public sealed class HuntTileZoneView
    {
        public string Location { get; set; } = "";
        public List<HuntTileFaceView> Tiles { get; set; } = new List<HuntTileFaceView>();
        // Unknown base numeric assignments are represented jointly in Kings facts.
        public int HiddenCount { get; set; }
        // Special tiles are tied to specific public cards, even when their faces
        // match. Standard duplicate tile identities carry no such distinction.
        public List<string> SpecialTileIds { get; set; } = new List<string>();
    }

    public sealed class EffectLifetimeView
    {
        public int AppliedTurn { get; set; }
        public string ScopeId { get; set; } = "";
        public List<string> RequiredCompanions { get; set; } = new List<string>();
        public bool Private { get; set; }
    }
}
