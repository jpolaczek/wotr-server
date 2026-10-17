using System.Collections.Generic;

namespace Wotr.Contracts
{
    public enum CharacterRosterLifecycle
    {
        Unknown,
        NotEntered,
        OnBoard,
        InFellowship,
        AwaitingPlacement,
        AwaitingReturn,
        Eliminated,
        EliminatedWithReplacement
    }

    public enum CharacterEntryMode
    {
        Unknown,
        FirstEntry,
        Transformation,
        ReturnAsNewVersion
    }

    // Own-side inspection only. These descriptions never replace command validation.
    public sealed class CharacterRosterView
    {
        public string FigureId { get; set; } = "";
        public string Identity { get; set; } = "";
        public Side Side { get; set; }
        public string CurrentDefinitionId { get; set; } = "";
        public string Title { get; set; } = "";
        public CharacterRosterLifecycle Lifecycle { get; set; }
        public string RegionId { get; set; } = "";
        public bool Garrison { get; set; }
        public int? Level { get; set; }
        public int? MovementLevel { get; set; }
        public int? CombatLevel { get; set; }
        public int? Leadership { get; set; }
        public string NationIcon { get; set; } = "";
        public bool UnlimitedMovement { get; set; }
        public List<CharacterEntryVersionView> FutureVersions { get; set; } = new List<CharacterEntryVersionView>();
    }

    public sealed class CharacterEntryVersionView
    {
        public string DefinitionId { get; set; } = "";
        public string Title { get; set; } = "";
        public CharacterEntryMode Mode { get; set; }
        public int ExtraDiceNextRecovery { get; set; }
        public ActionAvailabilityView Entry { get; set; } = new ActionAvailabilityView();
        public ActionAvailabilityView? CouncilEntry { get; set; }
        public List<string> CouncilTokens { get; set; } = new List<string>();
    }
}
