using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using static Wotr.Tests.BattleFixture;

namespace Wotr.Tests;

internal static class CardPackBattleFixture
{
    public static MatchState BeforeCards(string card, string character = CharacterRules.Aragorn, bool fpAttacks = true, EffectCatalog? catalog = null)
    {
        var state = State(fpAttacks ? Side.FreePeoples : Side.Shadow);
        var fp = fpAttacks ? Origin : Target;
        var sh = fpAttacks ? Target : Origin;
        for (var i = 0; i < 3; i++)
        {
            Put(state, "gondor", UnitKind.Regular, fp);
            Put(state, "sauron", UnitKind.Regular, sh);
        }
        Put(state, "gondor", UnitKind.Leader, fp);
        AddCharacter(state, character, fp);
        AddCharacter(state, "base.character.gimli", fp);
        AddCharacter(state, CharacterRules.WitchKing, sh);
        AddCharacter(state, CharacterRules.Mouth, sh);
        Put(state, "sauron", UnitKind.Nazgul, sh);
        EventTests.GiveCard(state, Side.FreePeoples, card);
        if (catalog is not null)
            EventTests.GiveCard(state, Side.Shadow, ShCard);
        var ids = state.Units.Where(u => u.Location == Origin).Select(u => u.Id)
            .Concat(state.Characters.Where(c => c.Location == Origin).Select(c => c.Id));
        return Begin(state, ids, catalog);
    }

    private static void AddCharacter(MatchState state, string definition, string region)
    {
        var id = definition == CharacterRules.Aragorn ? CharacterRules.Strider : definition == CharacterRules.White ? CharacterRules.Grey : definition;
        var character = CharacterFixture.Put(state, id, region);
        CharacterLifecycle.ApplyVersion(character, CharacterCatalog.Base[definition]);
    }

    public static MatchState Reveal(MatchState state, string card, EffectCatalog? catalog = null, params int[] dice)
    {
        if (state.Pending!.Kind == DecisionKind.Reaction)
            state = CharacterFixture.Do(state, CommandKind.DeclineReaction, catalog: catalog);
        var fpFirst = state.Pending!.Owner == Side.FreePeoples;
        var sh = catalog is null ? "" : ShCard;
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = fpFirst ? card : sh }, catalog: catalog);
        return Execute(state, CommandKind.CommitCombatCard, new() { CardId = fpFirst ? sh : card },
            new SequenceRandom(dice.Length == 0 ? new int[8] : dice), catalog);
    }

    public static MatchState NoRerolls(MatchState state, EffectCatalog? catalog = null)
    {
        while (state.Pending?.Kind == DecisionKind.BattleReroll)
            state = Execute(state, CommandKind.RerollBattle, catalog: catalog);
        return state;
    }

    public static EffectCatalog Opponent(EffectStep step, int initiative) => new(
        EffectCatalog.Base.Programs.Values.Where(p => p.Id != EffectCatalog.ReactionProgram).Append(new("test.pack02-opponent", [step])),
        EffectCatalog.Base.Events.Values, EffectCatalog.Base.Reactions.Values,
        EffectCatalog.Base.Combats.Values.Where(c => c.CardId != ShCard).Append(new(ShCard, new UnconditionalCombat(), [new(initiative, "test.pack02-opponent")])));
}
