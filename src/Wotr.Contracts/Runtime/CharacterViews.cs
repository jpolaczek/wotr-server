using System.Collections.Generic;

namespace Wotr.Contracts
{
    public sealed class CharacterActionOption
    {
        public string CharacterId { get; set; } = "";
        public string AbilityId { get; set; } = "";
        public string Title { get; set; } = "";
        public string Detail { get; set; } = "";
        public List<string> Dice { get; set; } = new List<string>();
        public List<string> CouncilTokens { get; set; } = new List<string>();
        public bool RequiresCouncilToken { get; set; }
        public List<string> Targets { get; set; } = new List<string>();
        public int TargetCount { get; set; } = 1;
        public List<RecruitmentOption> Recruitment { get; set; } = new List<RecruitmentOption>();
        public RulerRecruitmentOption? RulerRecruitment { get; set; }
    }
}
