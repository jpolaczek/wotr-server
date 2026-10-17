using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public sealed class CharacterOptions : IActionOptionsProvider
{
    public IEnumerable<ActionOption> GetOptions(MatchState state, Side actor)
    {
        if (BalrogEventTravel.Active(state, actor))
        {
            yield return BalrogEventTravel.Option(state);
            yield break;
        }
        if (NazgulEventTravel.Active(state, actor))
        {
            yield return NazgulEventTravel.Option(state);
            yield break;
        }
        if (state.Pending is not { Kind: DecisionKind.Action } || state.Pending.Owner != actor)
            yield break;
        var entries = Entries(state, actor);
        yield return Action(CommandKind.EnterCharacter, entries);
        yield return Action(CommandKind.UseCharacterAbility, CharacterAbilities.Options(state, actor).Where(o => o.Dice.Count > 0).ToList());
        if (actor != Side.Shadow)
            yield break;
        var travel = ShadowTravel.Options(state).ToList();
        var dice = state.Dice.Where(d => d.Owner == actor && DiceRules.CanPay(d, DieFace.Character)).Select(d => d.Id).ToList();
        var balrogCosts = LordsArmyCosts.Options(state, actor).Where(o => o.AbilityId == LordsArmyCosts.UseBalrog).ToList();
        dice.AddRange(balrogCosts.Select(o => o.DieId));
        yield return new()
        {
            Kind = CommandKind.MoveShadowCharacters,
            Travel = travel,
            EligibleDice = dice,
            MovementCosts = balrogCosts,
            EligibleIds = travel.Select(t => t.Id).ToList(),
            Targets = travel.SelectMany(t => t.Targets).Distinct().ToList(),
            Enabled = dice.Count > 0 && travel.Count > 0,
            Reason = "Requires a Character die and a Nazgûl or mobile Minion on the map."
        };
    }

    private static List<CharacterActionOption> Entries(MatchState state, Side actor)
    {
        var councilTokens = actor == Side.Shadow ? KingsCouncilEntry.Tokens(state) : [];
        return CharacterEntry.Candidates(state, actor).Concat(KingsCouncilEntry.Candidates(state, actor))
            .Distinct().GroupBy(e => e.DefinitionId).Select(group => new CharacterActionOption
        {
            CharacterId = group.Key,
            Title = CharacterCatalog.All[group.Key].Title,
            Detail = KingsChieftains.IsChieftain(group.Key)
                ? "Enter play. One shared Shadow Ruler die is available at the next Recover." : "Enter play. The additional action die becomes available next turn.",
            Targets = group.Select(e => e.Region).ToList(),
            CouncilTokens = KingsChieftains.IsChieftain(group.Key) ? councilTokens : [],
            RequiresCouncilToken = KingsChieftains.IsChieftain(group.Key) && !KingsChieftains.Prerequisite(state, group.Key),
            Recruitment = BalrogEntryRecruit.Options(state, group.Key).ToList(),
            Dice = state.Dice.Where(d => d.Owner == actor && CharacterEntry.CanPay(d, group.First())).Select(d => d.Id).ToList()
        }).Where(o => o.Dice.Count > 0).ToList();
    }

    private static ActionOption Action(CommandKind kind, List<CharacterActionOption> choices) => new()
    {
        Kind = kind,
        CharacterActions = choices,
        EligibleIds = choices.SelectMany(c => c.Targets).Distinct().ToList(),
        EligibleDice = choices.SelectMany(c => c.Dice).Distinct().ToList(),
        Targets = choices.SelectMany(c => c.Targets).Distinct().ToList(),
        Enabled = choices.Count > 0,
        Reason = "No character meets the conditions with an available die."
    };
}
