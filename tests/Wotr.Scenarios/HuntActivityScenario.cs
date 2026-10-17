using Wotr.Contracts;
using Wotr.Domain;

internal static class HuntActivityScenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["ui53-none"] = HuntInteractionScenario.Rolled(random, false, empty: true),
        ["ui53-miss"] = HuntInteractionScenario.Rolled(random, false),
        ["ui53-cancelled"] = Cancelled(random),
        ["ui53-reaction"] = Reaction(random),
        ["ui53-history"] = History(random)
    };

    private static MatchState Prepared(IRandomSource random)
    {
        var state = FellowshipScenario.Actions(random);
        state.Fellowship.RegionId = "hollin";
        state.Fellowship.Progress = 0;
        var die = state.Dice.First(d => d.Owner == Side.Shadow);
        die.Location = DieLocation.Hunt;
        die.HuntOrigin = "allocated";
        foreach (var tile in state.HuntTiles)
            tile.Location = tile.Id == "base.hunt.standard.1.0.1" ? "pool" : "removed";
        return state;
    }

    private static MatchState Cancelled(IRandomSource random)
    {
        var state = Prepared(random);
        Table(state, FreePeoplesCharacterCards02.Staff);
        state = Move(state, 5);
        return ExpansionUiFixture.Execute(state, CommandKind.RespondEffect,
            new() { ReactionId = FreePeoplesCharacterCards02.StaffReaction });
    }

    private static MatchState Reaction(IRandomSource random)
    {
        var state = Prepared(random);
        Table(state, ShadowCharacterCards03.Crebain);
        state = Move(state);
        state = ExpansionUiFixture.Execute(state, CommandKind.RespondEffect,
            new() { ReactionId = ShadowCharacterCards03.CrebainReaction }, 5, 0);
        if (state.Pending?.Kind != DecisionKind.HuntDamage)
            throw new InvalidOperationException("Hunt activity reaction fixture must await Free Peoples damage.");
        return state;
    }

    private static MatchState History(IRandomSource random)
    {
        var state = HuntInteractionScenario.Rolled(random, false);
        state = ExpansionUiFixture.Execute(state, CommandKind.DiscardDie,
            new() { DieId = ExpansionUiFixture.Die(state, Side.Shadow).Id });
        foreach (var tile in state.HuntTiles)
            tile.Location = tile.Id == "base.hunt.standard.1.0.1" ? "pool" : "removed";
        return Move(state, 5, 0);
    }

    private static MatchState Move(MatchState state, params int[] samples) => ExpansionUiFixture.Execute(state,
        CommandKind.MoveFellowship, new() { DieId = ExpansionUiFixture.Die(state, Side.FreePeoples).Id }, samples);

    private static void Table(MatchState state, string card)
    {
        CardPackScenario.Give(state, card);
        var owner = card.StartsWith("base.sh.", StringComparison.Ordinal) ? Side.Shadow : Side.FreePeoples;
        HandRules.Remove(state.Player(owner), card);
        state.Player(owner).Table.Add(card);
        state.ActiveEffects.Add(new()
        {
            Id = card, SourceId = card, Owner = owner,
            ExpiresWhen = card == ShadowCharacterCards03.Crebain ? EffectExpiry.FellowshipDeclaresSanctuary : EffectExpiry.None
        });
    }
}
