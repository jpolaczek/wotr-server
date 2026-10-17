using Wotr.Contracts;
using Wotr.Domain;
using static Wotr.Tests.CharacterFixture;

namespace Wotr.Tests;

internal static class Shadow01Fixture
{
    public const string RevealOne = "base.hunt.standard.1.1.1";

    public static MatchState BeforeEvent(string card, string region = "hollin", bool mordor = false)
    {
        var state = HuntTests.State(0, region);
        state.RandomSamples.Clear();
        if (mordor)
        {
            state.Fellowship.RegionId = MordorRules.Track;
            state.Fellowship.MordorStep = 2;
        }
        TurnFlow.SelectNext(state, Side.Shadow);
        EventTests.GiveCard(state, Side.Shadow, card);
        return state;
    }

    public static CommandPayload EventCost(MatchState state, string card)
    {
        var payload = Cost(state, DieFace.Character);
        payload.CardCost = DieFace.Character;
        payload.CardId = card;
        return payload;
    }

    public static MatchState Play(MatchState state, string card, params int[] random) =>
        Do(state, CommandKind.PlayEvent, EventCost(state, card), new SequenceRandom(random));

    public static void Pool(MatchState state, params string[] tiles)
    {
        HuntTests.Pool(state, tiles);
        state.HuntTiles = tiles.Select(id => state.HuntTiles.Single(t => t.Id == id))
            .Concat(state.HuntTiles.Where(t => !tiles.Contains(t.Id))).ToList();
    }

    public static MatchState BeforeCombat(string card, bool shadowAttacks = false, string companion = CharacterRules.Aragorn,
        string fpCard = "base.fp.character.01")
    {
        var state = CardPackBattleFixture.BeforeCards(fpCard, companion, !shadowAttacks);
        state.Fellowship.RegionId = BattleFixture.Target;
        EventTests.GiveCard(state, Side.Shadow, card);
        return state;
    }

    public static MatchState Reveal(MatchState state, string card, string fpCard = "", params int[] dice)
    {
        var shadowFirst = state.Pending!.Owner == Side.Shadow;
        state = Do(state, CommandKind.CommitCombatCard, new() { CardId = shadowFirst ? card : fpCard });
        return Do(state, CommandKind.CommitCombatCard, new() { CardId = shadowFirst ? fpCard : card },
            new SequenceRandom(dice.Length == 0 ? new int[8] : dice));
    }

    public static ActionOption Choices(MatchState state) => new EffectOptions().GetOptions(state, state.Pending!.Owner)
        .Single(o => o.Kind == CommandKind.ChooseEffect);
}
