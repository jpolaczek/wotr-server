using System.Collections.Generic;

namespace Wotr.Contracts
{
    // Ordinary player information at a supported base/Fate action window.
    // Null means that this version does not describe the current continuation.
    // No save frames, private opposing choices, deck order or entropy belong here.
    public sealed class ActionBoundaryView
    {
        public int Version { get; set; } = 5;
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
    }

    // Public progress of a Hunt caused by the paid Fellowship movement action.
    // The current roll, tile, damage and choice remain in HuntView/DecisionView.
    // Absence never means that an arbitrary Hunt has this return context.
    public sealed class FellowshipMoveView
    {
        public List<string> GuideWindowsOffered { get; set; } = new List<string>();
        public bool CasualtyConsequencesPending { get; set; }
        public bool VoluntaryReveal { get; set; }
        public string DepartureRegionId { get; set; } = "";
        public int DepartureProgress { get; set; }
        public List<string> DepartingCompanions { get; set; } = new List<string>();
        public bool PlacementStarted { get; set; }
        public FellowshipStrongholdView? StrongholdHunts { get; set; }
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
