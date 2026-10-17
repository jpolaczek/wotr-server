using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.CorrespondenceFixture;

namespace Wotr.Tests;

public class CorrespondenceRecoveryTests
{
    [PostgresFact]
    public async Task RestartWithoutClientsRecoversEveryWindowAndResumesItExactlyOnce()
    {
        await using var server = await Create();
        var cases = Cases();
        foreach (var item in cases)
        {
            item.State.Id = Guid.NewGuid().ToString("N");
            await server.Save(item.State);
            await Seen(server.Fp, item.State.Id, item.State.Revision);
        }
        await server.Restart();
        var fp = (await Read<MatchListView>(server.Fp, "/api/matches")).Matches;
        var sh = (await Read<MatchListView>(server.Sh, "/api/matches")).Matches;
        Assert.Equal(cases.Length, fp.Count);
        Assert.Equal(cases.Length, sh.Count);
        foreach (var item in cases)
            await Resume(server, item, fp.Single(m => m.MatchId == item.State.Id), sh.Single(m => m.MatchId == item.State.Id));
        await server.Restart();
        Assert.All((await Read<MatchListView>(server.Fp, "/api/matches")).Matches, m => Assert.True(m.HasUnseenChanges));
        Assert.Empty((await Read<MatchListView>(server.Other, "/api/matches")).Matches);
    }

    private static async Task Resume(CorrespondenceFixture server, ResumeCase item, MatchSummary fp, MatchSummary sh)
    {
        var state = item.State;
        CheckSummary(fp, state, Side.FreePeoples, server.Shadow.Username);
        CheckSummary(sh, state, Side.Shadow, server.FreePeoples.Username);
        Assert.False(fp.HasUnseenChanges);
        Assert.True(sh.HasUnseenChanges);
        Assert.Equal(state.Revision, fp.LastSeenRevision);
        Assert.Equal(-1, sh.LastSeenRevision);
        foreach (var client in new[] { server.Fp, server.Sh })
        {
            var activity = await Read<MatchActivityView>(client, $"/api/matches/{state.Id}/activity");
            Assert.Equal(state.Revision, activity.Revision);
            var view = await Read<PlayerView>(client, $"/api/matches/{state.Id}");
            Assert.Equal(state.Pending!.Owner, view.DecisionOwner);
        }
        SameState(state, (await server.Matches.Read(state.Id, server.FreePeoples.AccountId))!.State);
        var command = BattleFixture.Command(state, item.Command, item.Payload);
        var actor = state.Pending!.Owner == Side.FreePeoples ? server.Fp : server.Sh;
        var first = await Send(actor, command);
        Assert.Equal(state.Revision + 1, first.Revision);
        Assert.Equal(first.Revision, (await Send(actor, command)).Revision);
        var restored = (await server.Matches.Read(state.Id, server.FreePeoples.AccountId))!.State;
        var random = new SequenceRandom();
        var expected = LordsFixture.Engine().Execute(state, state.Pending.Owner, command, random);
        Assert.True(expected.Receipt.Accepted, expected.Receipt.Error);
        Assert.Equal(0, random.Calls);
        SameState(expected.State, restored);
    }

    private static void CheckSummary(MatchSummary summary, MatchState state, Side side, string opponent)
    {
        Assert.Equal(side, summary.You);
        Assert.Equal(opponent, summary.Opponent);
        Assert.Equal(state.Turn, summary.Turn);
        Assert.Equal(state.Phase, summary.Phase);
        Assert.Equal(state.Pending!.Owner, summary.DecisionOwner);
        Assert.Equal(state.Pending.Owner == side ? MatchStatus.YourDecision : MatchStatus.Waiting, summary.Status);
        Assert.Equal(state.Pending.Owner == side ? state.Pending.Kind : (DecisionKind?)null, summary.YourDecision);
    }

    private static ResumeCase[] Cases()
    {
        var battle = BattleFixture.State();
        var attacker = BattleFixture.Put(battle, "gondor", UnitKind.Regular, BattleFixture.Origin);
        BattleFixture.Put(battle, "sauron", UnitKind.Elite, BattleFixture.Target);
        battle.Player(Side.FreePeoples).Hand.Add(BattleFixture.FpCard);
        battle = BattleFixture.Begin(battle, [attacker.Id]);
        var hunt = HuntTests.State();
        HuntTests.Pool(hunt, HuntTests.Three);
        hunt = HuntTests.Move(hunt, new SequenceRandom(5, 0));
        return
        [
            new(battle, CommandKind.CommitCombatCard, new() { CardId = BattleFixture.FpCard }),
            new(hunt, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring }),
            new(CouncilSetupTests.Setup(), CommandKind.ChooseCompanionVersions,
                new() { Ids = [LordsCharacters.Gandalf, "lords.character.boromir"], GuideId = CharacterRules.Strider }),
            new(KingsFixture.Attempt(), CommandKind.ChooseCorruptionTile, new() { Ids = [HuntTests.Three] })
        ];
    }

    private sealed record ResumeCase(MatchState State, CommandKind Command, CommandPayload Payload);
}
