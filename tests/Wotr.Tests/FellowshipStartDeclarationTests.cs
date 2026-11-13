using System.Text.Json;
using System.Text.Json.Nodes;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

// EXP-19: the before/after proof that a declaration at the very start of a game changes nothing, in every legal
// configuration with the Council's and Lords' setup choices; what keeps the offer on any other board; and the command,
// which stays legal for an older client or a saved draft (docs/rules/T15-FELLOWSHIP.md).
public class FellowshipStartDeclarationTests
{
    private const string Rivendell = "rivendell";
    private const string DeclaredWindow = "fellowship.declared";
    private const int Base = 0;
    private const int Lords = 1;

    // What any accepted command leaves behind, plus the once-a-turn mark, which only CanDeclare reads.
    private static readonly string[] StateBookkeeping =
        ["revision", "decisionSequence", "pending.id", "fellowship.lastDeclarationTurn", "events"];

    // The CPU's action boundary repeats fellowship.lastDeclarationTurn for this turn (bookkeeping like the state's own).
    private static readonly string[] ViewBookkeeping = ["revision", "decision.id", "history", "actionBoundary.fellowshipDeclaredThisTurn"];

    private static readonly GameEngine Engine = GameRuntime.Engine(EffectCatalog.All);
    private static readonly Projection Views = GameRuntime.Projection(EffectCatalog.All);

    public static IEnumerable<object[]> FirstPhases() =>
        from flags in Enumerable.Range(0, 512).Where(ExpansionConfigurations.Allowed)
        let configuration = ExpansionConfigurations.FromFlags(flags)
        from departures in configuration.Council ? new[] { 0, 1, 5 } : [0]
        from keeper in configuration.Lords ? new[] { false, true } : [false]
        select new object[] { flags, departures, keeper };

    [Theory]
    [MemberData(nameof(FirstPhases))]
    public void TheFirstFellowshipPhaseDoesNotOfferADeclarationThatChangesOnlyBookkeeping(int flags, int departures, bool keeper)
    {
        var before = FirstFellowshipPhase(flags, departures, keeper);
        Assert.True(before.Nations.Single(n => n.Id == "elves").Active, "The Elves start active, so Rivendell activates nothing.");
        Assert.Equal(departures, before.Characters.Count(c => CouncilCompanions.Homelands(c).Contains(c.Location)));
        CheckNoOffer(before);
        CheckEmptyPreview(before);
        var after = Declare(StateCodec.Copy(before));
        CheckOnlyBookkeeping(before, after);
        CheckTheGameGoesOnTheSame(before, after);
    }

    [Theory]
    [InlineData(Rivendell, "elves")]
    [InlineData("minas-tirith", "gondor")]
    public void AnInactiveSettlementNationKeepsTheOffer(string region, string nation)
    {
        var state = Phase(Base, region);
        state.Nations.Single(n => n.Id == nation).Active = false;
        CheckOffered(state);
        Assert.True(Declare(state).Nations.Single(n => n.Id == nation).Active);
    }

    [Fact]
    public void ProgressKeepsTheOffer()
    {
        var state = Phase(Base);
        state.Fellowship.Progress = 1;
        CheckOffered(state);
        var after = Do(state, CommandKind.DeclareFellowship, new() { Route = [Rivendell, "fords-of-bruinen"] });
        Assert.Equal("fords-of-bruinen", after.Fellowship.RegionId);
        Assert.Equal(0, after.Fellowship.Progress);
    }

    // Corruption anywhere keeps the offer: only a test board stands outside Rivendell with Corruption at turn 1.
    [Theory]
    [InlineData(Rivendell, 1)]
    [InlineData("minas-morgul", 2)]
    public void CorruptionKeepsTheOffer(string region, int remaining)
    {
        var state = Phase(Base, region);
        state.Fellowship.Corruption = 2;
        CheckOffered(state);
        Assert.Equal(remaining, Declare(state).Fellowship.Corruption);
    }

    [Theory]
    [InlineData(FreePeoplesStrategyCards01.LastBattle, Side.FreePeoples, EffectExpiry.AragornArmyOutsideFreeNation)]
    [InlineData(ShadowCharacterCards03.Crebain, Side.Shadow, EffectExpiry.FellowshipDeclaresSanctuary)]
    [InlineData(LordsShadowCards.Captain, Side.Shadow, EffectExpiry.None)]
    public void AnEffectOnTheTableKeepsTheOffer(string card, Side owner, EffectExpiry expiry)
    {
        var state = Phase(Lords);
        state.Player(owner).Table.Add(card);
        state.ActiveEffects.Add(new() { Id = card, SourceId = card, Owner = owner, ExpiresWhen = expiry });
        CheckOffered(state);
        var after = Declare(state);
        Assert.DoesNotContain(card, after.Player(owner).Table);
        Assert.Contains(card, after.Player(owner).Discard);
    }

    [Fact]
    public void ACardWaitingForADeclarationKeepsTheOffer()
    {
        var state = Phase(Lords, "moria");
        state.Player(Side.Shadow).Table.Add(LordsShadowCards.Gates);
        CheckOffered(state);
        Assert.Equal(DeclaredWindow, Declare(state).Pending!.Window);
    }

    [Fact]
    public void TheBalrogInTheRegionKeepsTheOffer()
    {
        var state = Phase(Lords, "moria");
        LordsCharacterFixture.Put(state, LordsCharacters.Balrog, "moria");
        HuntTests.Pool(state, HuntTests.Three);
        CheckOffered(state);
        var after = Do(state, CommandKind.DeclareFellowship, new() { Route = ["moria"] }, new SequenceRandom(0));
        Assert.True(HuntRuntime.Active(after)!.FireAndShadow);
    }

    [Fact]
    public void SmeagolInTheFellowshipKeepsTheOffer()
    {
        var state = Phase(Lords);
        SmeagolRules.Enter(state);
        CheckOffered(state);
        var after = Declare(state);
        Assert.False(SmeagolRules.Present(after));
        Assert.Contains(SmeagolRules.WeShallGetIt, after.Player(Side.Shadow).Table);
    }

    [Fact]
    public void TheChiefKeepsTheOfferWhereHeCouldFollow()
    {
        var state = Phase(Lords, "hollin");
        LordsCharacterFixture.Put(state, LordsCharacters.WitchKing, "minas-morgul", CharacterRules.WitchKing);
        CheckOffered(state);
        Assert.Equal(DeclaredWindow, Declare(state).Pending!.Window);
    }

    [Fact]
    public void TheChiefCannotFollowToRivendellSoThereIsNoOffer()
    {
        var state = Phase(Lords);
        LordsCharacterFixture.Put(state, LordsCharacters.WitchKing, "minas-morgul", CharacterRules.WitchKing);
        CheckNoOffer(state);
        CheckOnlyBookkeeping(state, Declare(StateCodec.Copy(state)));
    }

    [Fact]
    public void FromTheSecondTurnTheOfferIsAsBefore()
    {
        var state = Phase(Base);
        TurnFlow.BeginTurn(state);
        Assert.Equal(2, state.Turn);
        Assert.Equal(DecisionKind.FellowshipPhase, state.Pending!.Kind);
        CheckOffered(state);
    }

    [Theory]
    [InlineData("hollin", Side.FreePeoples)]
    [InlineData("minas-tirith", Side.Shadow)]
    public void AnotherRegionWhereNothingWouldHappenHasNoOfferEither(string region, Side controller)
    {
        var state = Phase(Base, region);
        state.Control[region] = controller;
        CheckNoOffer(state);
        CheckOnlyBookkeeping(state, Declare(StateCodec.Copy(state)));
    }

    // A new reaction to a declaration must be weighed in DeclarationCanChange, as these are.
    [Fact]
    public void EveryReactionToADeclarationNeedsATableCardOrTheChief()
    {
        var reactions = EffectCatalog.All.Reactions.Values.Where(r => r.Window == DeclaredWindow).ToList();
        Assert.Contains(reactions, r => r.Id == ChiefReactions.Follow);
        Assert.All(reactions, r => Assert.True(r.RequiredTableCard is not null || r.RequiredCharacterId == LordsCharacters.WitchKing, r.Id));
    }

    [Fact]
    public void EveryScenarioThatAsksForADeclarationStillOffersIt()
    {
        var asking = TestScenarioCatalog.All.Where(s => s.Description.Contains("Declare", StringComparison.Ordinal))
            .Select(s => (s.Id, State: s.Create("exp-19-" + s.Id)))
            .Where(s => s.State.Pending?.Kind == DecisionKind.FellowshipPhase).ToList();
        Assert.NotEmpty(asking);
        Assert.All(asking, s => Assert.True(Offers(s.State), s.Id));
        CheckNoOffer(TestScenarioCatalog.Find("setup")!.Create("exp-19-setup"));
    }

    private static MatchState Phase(int flags, string region = Rivendell)
    {
        var state = FirstFellowshipPhase(flags, 0, false);
        state.Fellowship.RegionId = region;
        return state;
    }

    private static MatchState FirstFellowshipPhase(int flags, int departures, bool keeper)
    {
        var state = MatchFactory.Create("exp-19-" + flags, new BaseCorpusFixture.ZeroRandom(), ExpansionConfigurations.FromFlags(flags));
        TurnFlow.Start(state);
        for (var step = 0; state.Pending!.Kind != DecisionKind.FellowshipPhase; step++)
        {
            Assert.True(step < 4, $"The setup stopped at {state.Pending.Kind}.");
            state = AnswerSetup(state, departures, keeper);
        }
        return state;
    }

    private static MatchState AnswerSetup(MatchState state, int departures, bool keeper) => state.Pending!.Kind switch
    {
        DecisionKind.CouncilVersions => Do(state, CommandKind.ChooseCompanionVersions, new()
        {
            GuideId = CharacterRules.Grey,
            Ids = [keeper ? LordsCharacters.Gandalf : CharacterRules.Grey, .. Alternates(state, departures)]
        }),
        DecisionKind.CouncilHomelands => Do(state, CommandKind.ChooseCouncilHomelands, new() { Placements = Homelands(state, departures) }),
        DecisionKind.CouncilToken => Do(state, CommandKind.ChooseCouncilToken, new() { Ids = [CouncilTokens.Politics] }),
        _ => throw new InvalidOperationException($"Unexpected setup decision {state.Pending.Kind}.")
    };

    private static IEnumerable<string> Alternates(MatchState state, int departures) => departures == 0 ? [] :
        CouncilCompanions.AvailableVersions(state).Where(v => v.FigureId != CharacterRules.Grey).Select(v => v.AlternateId);

    private static List<UnitPlacementChoice> Homelands(MatchState state, int departures) =>
        CouncilCompanions.CanStartOutside(state).Take(departures).Select(c => new UnitPlacementChoice
        {
            UnitId = c.Id,
            RegionId = CouncilCompanions.Homelands(c)[0]
        }).ToList();

    // By default no command here may ask for a random sample: the sequence is empty and refuses to give one.
    private static MatchState Do(MatchState state, CommandKind kind, CommandPayload payload, IRandomSource? random = null)
    {
        var result = Engine.Execute(state, state.Pending!.Owner, FellowshipTests.Command(state, kind, payload), random ?? new SequenceRandom());
        Assert.True(result.Receipt.Accepted, result.Receipt.Error);
        return result.State;
    }

    private static MatchState Declare(MatchState state) =>
        Do(state, CommandKind.DeclareFellowship, new() { Route = [state.Fellowship.RegionId] });

    private static bool Offers(MatchState state) =>
        Views.For(state, Side.FreePeoples).Options.Any(o => o.Kind == CommandKind.DeclareFellowship);

    private static void CheckOffered(MatchState state)
    {
        Assert.True(FellowshipFlow.DeclarationCanChange(state));
        Assert.True(Offers(state));
    }

    // Only the option goes: the phase's other options and its allowed commands stay.
    private static void CheckNoOffer(MatchState state)
    {
        Assert.False(FellowshipFlow.DeclarationCanChange(state));
        Assert.True(FellowshipFlow.CanDeclare(state));
        var options = Views.For(state, Side.FreePeoples).Options.Select(o => o.Kind).ToList();
        Assert.DoesNotContain(CommandKind.DeclareFellowship, options);
        Assert.Contains(CommandKind.SelectGuide, options);
        Assert.Contains(CommandKind.ContinueFellowship, options);
        Assert.Empty(Views.For(state, Side.Shadow).Options);
        Assert.Contains(CommandKind.DeclareFellowship, state.Pending!.Commands);
    }

    private static void CheckEmptyPreview(MatchState state)
    {
        var command = FellowshipTests.Command(state, CommandKind.DeclareFellowship, new() { Route = [state.Fellowship.RegionId] });
        var preview = new PreviewService(Engine).Preview(state, Side.FreePeoples, command);
        Assert.True(preview.Legal);
        Assert.Equal(0, preview.Fellowship!.Healed);
        Assert.Equal(state.Fellowship.RegionId, preview.Fellowship.RegionId);
        Assert.Empty(preview.PoliticsChanges);
        Assert.Empty(preview.RemovedIds);
        Assert.False(preview.HasUnknownResult);
    }

    private static void CheckOnlyBookkeeping(MatchState before, MatchState after)
    {
        Assert.Equal(DecisionKind.FellowshipPhase, after.Pending!.Kind);
        Assert.Empty(after.Resolution.Frames);
        Assert.Empty(Unexplained(StateJson(before), StateJson(after), StateBookkeeping));
        foreach (var side in Enum.GetValues<Side>())
            Assert.Empty(Unexplained(ViewJson(before, side), ViewJson(after, side), ViewBookkeeping));
    }

    // The rest of the Fellowship phase and the roll, with the same random source, reach the same game.
    private static void CheckTheGameGoesOnTheSame(MatchState before, MatchState after)
    {
        var without = PlayToTheRoll(StateCodec.Copy(before));
        var with = PlayToTheRoll(after);
        Assert.Equal(1, with.Fellowship.LastDeclarationTurn);
        Assert.Empty(Unexplained(StateJson(without), StateJson(with), StateBookkeeping));
    }

    private static MatchState PlayToTheRoll(MatchState state)
    {
        state = Do(state, CommandKind.ContinueFellowship, new());
        state = Do(state, CommandKind.AllocateHunt, new(), new BaseCorpusFixture.ZeroRandom());
        // Each sample names the revision that drew it, one later after the declaration; its value must still be the same.
        foreach (var sample in state.RandomSamples)
            sample.Revision = 0;
        return state;
    }

    private static JsonNode StateJson(MatchState state) => JsonNode.Parse(StateCodec.Write(state))!;

    private static JsonNode ViewJson(MatchState state, Side side) =>
        JsonNode.Parse(JsonSerializer.Serialize(Views.For(state, side), BoardCatalog.Json))!;

    // The differing paths outside the bookkeeping, with their values, so a failure names what really changed.
    private static List<string> Unexplained(JsonNode before, JsonNode after, IReadOnlyList<string> bookkeeping)
    {
        var differences = new List<string>();
        Compare("", before, after, differences);
        return differences.Where(difference => !bookkeeping.Any(path => Within(difference, path))).ToList();
    }

    private static bool Within(string difference, string path) => difference.StartsWith(path + " ", StringComparison.Ordinal) ||
        difference.StartsWith(path + ".", StringComparison.Ordinal) || difference.StartsWith(path + "[", StringComparison.Ordinal);

    private static void Compare(string path, JsonNode? before, JsonNode? after, List<string> differences)
    {
        if (before is JsonObject beforeObject && after is JsonObject afterObject)
        {
            foreach (var key in beforeObject.Select(p => p.Key).Union(afterObject.Select(p => p.Key)))
                Compare(path.Length == 0 ? key : path + "." + key, beforeObject[key], afterObject[key], differences);
        }
        else if (before is JsonArray beforeArray && after is JsonArray afterArray)
        {
            for (var index = 0; index < Math.Max(beforeArray.Count, afterArray.Count); index++)
                Compare($"{path}[{index}]", index < beforeArray.Count ? beforeArray[index] : null,
                    index < afterArray.Count ? afterArray[index] : null, differences);
        }
        else if (!JsonNode.DeepEquals(before, after))
            differences.Add($"{path} {before?.ToJsonString()} -> {after?.ToJsonString()}");
    }
}
