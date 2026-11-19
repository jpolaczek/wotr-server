using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

// UI-547 (owner, 2026-11-18: "if we recruit a unit and there is only one possible region, we do not place it automatically,
// unless the effect by definition has no choice"): a recruitment region the card lets its player choose is asked even when
// only one is legal now.
internal static class RecruitRegionFixture
{
    // The pending choice is that region alone; the payload that chooses it.
    public static CommandPayload Only(MatchState state, string region)
    {
        Assert.Equal(DecisionKind.EffectChoice, state.Pending!.Kind);
        Assert.Equal("Region", state.Pending.Presentation);
        Assert.Equal([region], state.Pending.PrivateChoices);
        return new() { Ids = [region] };
    }
}
