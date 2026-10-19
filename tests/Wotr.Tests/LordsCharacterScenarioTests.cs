using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

// UI-271: the seeder's Lords character scenarios wait on the decision their client test plays on its object.
public class LordsCharacterScenarioTests
{
    private static readonly Projection Views = GameRuntime.Projection(EffectCatalog.All);
    private static readonly Dictionary<string, MatchState> Scenarios = LordsCharacterScenario.Create(new SeededRandom(11));

    [Fact]
    public void FreePeoplesMayEnterElrondAndGaladrielWithAMusterDie()
    {
        var view = Views.For(Scenarios["ui271-entries-fp"], Side.FreePeoples);
        var entries = Entries(view);
        Assert.Contains(LordsCharacters.Elrond, entries);
        Assert.Contains(LordsCharacters.Galadriel, entries);
    }

    [Fact]
    public void ShadowMayEnterEveryLordsMinionAndBothVersionsOfTheMouthAndTheWitchKing()
    {
        var view = Views.For(Scenarios["ui271-entries-sh"], Side.Shadow);
        var entries = Entries(view);
        foreach (var id in new[]
                 {
                     LordsCharacters.Balrog, LordsCharacters.Gothmog, CharacterRules.Mouth, LordsCharacters.Mouth,
                     CharacterRules.WitchKing, LordsCharacters.WitchKing
                 })
            Assert.Contains(id, entries);
        var mouth = view.OwnCharacterRoster.Single(r => r.FigureId == CharacterRules.Mouth);
        Assert.Equal([CharacterRules.Mouth, LordsCharacters.Mouth], mouth.FutureVersions.Select(v => v.DefinitionId).Order());
    }

    [Fact]
    public void TheBlackCaptainInPlayMayTurnIntoTheChief()
    {
        var view = Views.For(Scenarios["ui271-witch-king-switch"], Side.Shadow);
        Assert.Contains(LordsCharacters.WitchKing, Entries(view));
        var row = view.OwnCharacterRoster.Single(r => r.FigureId == CharacterRules.WitchKing);
        Assert.Equal(CharacterRules.WitchKing, row.CurrentDefinitionId);
        Assert.Equal(LordsCharacters.WitchKing, Assert.Single(row.FutureVersions).DefinitionId);
    }

    [Theory]
    [InlineData("ui271-spear", ChiefReactions.Spear)]
    [InlineData("ui271-he-sees", ChiefReactions.Follow)]
    public void TheChiefsReactionWindowOffersItsReaction(string scenario, string reaction)
    {
        var state = Scenarios[scenario];
        Assert.Equal(DecisionKind.Reaction, state.Pending!.Kind);
        Assert.Equal(Side.Shadow, state.Pending.Owner);
        var view = Views.For(state, Side.Shadow);
        var respond = view.Options.Single(o => o.Kind == CommandKind.RespondEffect && o.Enabled);
        Assert.Contains(respond.EffectChoices, c => c.Id == reaction);
        Assert.Contains(view.Options, o => o.Kind == CommandKind.DeclineReaction && o.Enabled);
    }

    [Fact]
    public void TheBalrogsCardAsksForItsOrder()
    {
        var state = Scenarios["ui271-balrog-order"];
        Assert.Equal(DecisionKind.EffectChoice, state.Pending!.Kind);
        Assert.Equal("BalrogOrder", state.Pending.Presentation);
        var view = Views.For(state, Side.Shadow);
        var choices = view.Options.Single(o => o.Kind == CommandKind.ChooseEffect).EffectChoices.Select(c => c.Id).ToList();
        Assert.Contains(BalrogEventStep.Alone, choices);
        Assert.Contains(BalrogEventStep.Decline, choices);
    }

    private static List<string> Entries(PlayerView view) => view.Options
        .Where(o => o.Kind == CommandKind.EnterCharacter && o.Enabled)
        .SelectMany(o => o.CharacterActions).Where(c => c.Dice.Count > 0).Select(c => c.CharacterId).ToList();
}
