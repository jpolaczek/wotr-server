using System.Collections.Generic;

namespace Wotr.Contracts
{
    // Public past choices and resource obligations, not a serialized Lords save.
    // Private companion versions remain exclusively in the owning player's view.
    public sealed class LordsRuleFactsView
    {
        public bool WitchKingReplaced { get; set; }
        public bool EvilKnowledgeUsed { get; set; }
        public bool SmeagolLeft { get; set; }
        public bool GollumEntered { get; set; }
        public bool LoreOfferedThisTurn { get; set; }
        public Dictionary<Side, int> RolledDiceCounts { get; set; } = new Dictionary<Side, int>();
        public List<Side> SpecialDieChoiceOrder { get; set; } = new List<Side>();
        public List<string> CaptainDice { get; set; } = new List<string>();
        public List<string> PaidWillOfWestIds { get; set; } = new List<string>();
        public CouncilRuleFactsView? Council { get; set; }
        // Present only after the action's consequences have completed and Vilya
        // offers to keep a paid die. False denotes a non-Fellowship action.
        public bool? CompletedActionWasFellowshipMove { get; set; }
    }

    public sealed class CouncilRuleFactsView
    {
        public bool HomelandsChosen { get; set; }
        public bool FellowshipMovedThisTurn { get; set; }
        public string FellowshipBoxDieId { get; set; } = "";
    }
}
