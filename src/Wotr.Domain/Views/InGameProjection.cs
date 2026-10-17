using Wotr.Contracts;

namespace Wotr.Domain;

public static class InGameProjection
{
    public static List<ActionAvailabilityView> Availability(MatchState state, Side viewer, IReadOnlyList<RulerTokenActionOption> rulerActions) =>
        ResourceAvailabilityProjection.For(state, viewer, rulerActions)
            .Concat(DieInspection.For(state, viewer)).Concat(SovereignAvailabilityProjection.For(state, viewer))
            .Concat(RecruitmentAvailabilityProjection.For(state, viewer)).ToList();

    public static List<MilitaryVictoryView> Victory(MatchState state) => Enum.GetValues<Side>().Select(side =>
    {
        var regions = MilitaryVictory.ScoringRegions(state, side).Select(region => new VictoryRegionView
        {
            RegionId = region.Id, Name = region.Name, Settlement = region.Settlement ?? "", Points = region.VictoryPoints
        }).ToList();
        var points = regions.Sum(r => r.Points);
        return new MilitaryVictoryView
        {
            Side = side, Regions = regions, Points = points, Threshold = MilitaryVictory.Threshold(side),
            ThresholdReached = points >= MilitaryVictory.Threshold(side),
            CheckedAt = "At turn end (phase 6), after Ring victories; Shadow is checked first.",
            Source = "BASE p.44; scenario setup ownership, before temporary Kings overlays."
        };
    }).ToList();
}
