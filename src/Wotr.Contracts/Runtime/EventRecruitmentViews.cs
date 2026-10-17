using System.Collections.Generic;

namespace Wotr.Contracts
{
    public sealed class EventRecruitmentOption
    {
        public int MaximumRegions { get; set; }
        public int DistinctNations { get; set; }
        public List<RecruitmentLimit> Limits { get; set; } = new List<RecruitmentLimit>();
    }

    public sealed class RecruitmentLimit
    {
        public string Nation { get; set; } = "";
        public string RegionId { get; set; } = "";
        public List<UnitKind> Kinds { get; set; } = new List<UnitKind>();
        public int Maximum { get; set; }
    }
}
