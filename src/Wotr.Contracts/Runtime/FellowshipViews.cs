using System.Collections.Generic;

namespace Wotr.Contracts
{
    public sealed class FellowshipPreview
    {
        public string RegionId { get; set; } = "";
        public int Progress { get; set; }
        public int? MordorStep { get; set; }
        public bool Hidden { get; set; }
        public int Healed { get; set; }
        public List<string> AdditionalHuntRegions { get; set; } = new List<string>();
    }

    public sealed class CompanionMoveChoice
    {
        public List<string> Ids { get; set; } = new List<string>();
        public List<string> Route { get; set; } = new List<string>();
    }

    public sealed class TravelOption
    {
        public string Id { get; set; } = "";
        public string Origin { get; set; } = "";
        public int Range { get; set; }
        public bool Flight { get; set; }
        public int SmallGroupRange { get; set; }
        public List<string> SmallGroupPartners { get; set; } = new List<string>();
        public List<string> RequiredGroup { get; set; } = new List<string>();
        public List<string> Targets { get; set; } = new List<string>();
        public List<string> StopRegions { get; set; } = new List<string>();
        public List<string> BlockedRegions { get; set; } = new List<string>();
    }
}
