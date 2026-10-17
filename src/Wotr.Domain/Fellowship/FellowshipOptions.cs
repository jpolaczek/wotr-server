using Wotr.Contracts;

namespace Wotr.Domain;

public sealed class FellowshipOptions : IActionOptionsProvider
{
    public IEnumerable<ActionOption> GetOptions(MatchState state, Side actor)
    {
        if (actor != Side.FreePeoples || state.Pending?.Owner != actor)
            yield break;
        if (state.Pending.Kind is DecisionKind.FellowshipPhase or DecisionKind.Guide)
            yield return new() { Kind = CommandKind.SelectGuide, EligibleIds = CompanionRules.Guides(state) };
        if (FellowshipFlow.CanDeclare(state))
            yield return Location(state, CommandKind.DeclareFellowship);
        if (MordorRules.CanEnter(state))
            yield return new() { Kind = CommandKind.EnterMordor };
        if (state.Pending.Kind == DecisionKind.FellowshipRoute)
            yield return Location(state, CommandKind.RevealFellowship);
        if (state.Pending.Kind is not (DecisionKind.Action or DecisionKind.CompanionSeparation))
            yield break;
        var dice = state.Dice.Where(d => d.Owner == actor && d.Location == DieLocation.Available && DiceRules.CanPay(d, DieFace.Character)).Select(d => d.Id).ToList();
        var forced = state.Pending.Kind == DecisionKind.CompanionSeparation;
        if (forced && FellowshipFlow.Current(state)?.Fellowship is { FixedCompanions.Count: > 0 } placement)
        {
            yield return FixedPlacement(state, placement);
            yield break;
        }
        if (!forced && !state.Fellowship.Hidden)
        {
            var hidingDice = state.Dice.Where(d => CharacterRules.CanHide(state, d.Id)).Select(d => d.Id).ToList();
            if (hidingDice.Count > 0)
                yield return new() { Kind = CommandKind.HideFellowship, EligibleDice = hidingDice };
        }
        if (dice.Count == 0 && !forced)
            yield break;
        if (CompanionRules.Journey(state)?.AllowSeparation != false && state.Fellowship.Companions.Count > 0 && (!state.Fellowship.MordorStep.HasValue || forced))
            yield return new()
            {
                Kind = CommandKind.SeparateCompanions,
                RulerTravel = forced ? null : KingsFarSighted.Option(state, CommandKind.SeparateCompanions),
                EligibleIds = [.. state.Fellowship.Companions],
                EligibleDice = forced ? [] : dice,
                Minimum = 1,
                Maximum = state.Fellowship.Companions.Count,
                Travel = state.Fellowship.MordorStep.HasValue ? [] : CompanionRules.Inside(state)
                    .Select(c => Travel(state, c.Id, state.Fellowship.RegionId, CompanionRules.Range(state, [c], true), true)).ToList()
            };
        if (forced && CompanionRules.Journey(state) is null)
            yield break;
        var outside = state.Characters.Where(c => c.Side == actor && c.Level > 0 && BoardRules.Regions.ContainsKey(c.Location)).ToList();
        if (outside.Count > 0)
            yield return new()
            {
                Kind = CommandKind.MoveCompanions,
                RulerTravel = forced ? null : KingsFarSighted.Option(state, CommandKind.MoveCompanions),
                EligibleIds = outside.Select(c => c.Id).ToList(),
                EligibleDice = forced ? [] : dice,
                MaximumGroups = forced ? CompanionRules.Journey(state)!.MaximumGroups : 0,
                AllowEmptySelection = forced && CompanionRules.Journey(state)!.AllowNoMovement,
                Travel = outside.Select(c => CompanionTravel(state, c, outside)).ToList()
            };
    }

    private static ActionOption FixedPlacement(MatchState state, FellowshipProcedure placement) => new()
    {
        Kind = CommandKind.SeparateCompanions,
        EligibleIds = [.. placement.FixedCompanions],
        Minimum = placement.FixedCompanions.Count,
        Maximum = placement.FixedCompanions.Count,
        Travel = placement.FixedCompanions.Select(id =>
        {
            var option = Travel(state, id, placement.SeparationOrigin, placement.SeparationRange, true);
            option.RequiredGroup = [.. placement.FixedCompanions];
            return option;
        }).ToList()
    };

    internal static TravelOption CompanionTravel(MatchState state, CharacterState character, IReadOnlyList<CharacterState> companions, int rangeBonus = 0)
    {
        var journey = CompanionRules.Journey(state);
        var shadowfax = journey?.FixedLevel is null && character.DefinitionId == CharacterRules.White;
        var range = (journey?.FixedLevel ?? CharacterRules.MovementLevel(character)) + (journey?.Bonus ?? 0) + rangeBonus;
        var option = Travel(state, character.Id, character.Location, CompanionRules.Range(state, [character], false) + rangeBonus, true);
        option.Range = range;
        if (shadowfax)
        {
            option.SmallGroupRange = 4 + (journey?.Bonus ?? 0) + rangeBonus;
            option.SmallGroupPartners = companions.Where(CharacterRules.IsHobbit).Select(c => c.Id).ToList();
        }
        return option;
    }

    internal static List<TravelOption> RulerTravel(MatchState state, CommandKind kind)
    {
        if (kind == CommandKind.SeparateCompanions)
            return CompanionRules.Inside(state).Select(c => Travel(state, c.Id, state.Fellowship.RegionId,
                CompanionRules.Range(state, [c], true) + 1, true)).ToList();
        var companions = state.Characters.Where(c => c.Side == Side.FreePeoples && c.Level > 0 && CharacterRules.OnBoard(c)).ToList();
        return companions.Select(c => CompanionTravel(state, c, companions, 1)).ToList();
    }

    private static ActionOption Location(MatchState state, CommandKind kind)
    {
        var travel = Travel(state, "fellowship", state.Fellowship.RegionId, state.Fellowship.Progress, false);
        if (kind == CommandKind.RevealFellowship)
            travel.Targets.RemoveAll(r => TravelRules.Sanctuary(state, r));
        return new() { Kind = kind, Targets = travel.Targets, Travel = [travel] };
    }

    private static TravelOption Travel(MatchState state, string id, string origin, int range, bool companions)
    {
        var siegeEnd = companions && CompanionRules.CanEnterSiege(state);
        var sieges = companions ? BoardRules.Regions.Keys.Where(r => BoardRules.Besieged(state, r, Side.FreePeoples)).ToList() : [];
        return new()
        {
            Id = id,
            Origin = origin,
            Range = range,
            Targets = TravelRules.Reachable(state, origin, range, companions, siegeEnd),
            StopRegions = companions ? BoardRules.Regions.Keys.Where(r => TravelRules.ShadowStronghold(state, r) || siegeEnd && sieges.Contains(r)).ToList() : [],
            BlockedRegions = siegeEnd ? sieges.Where(r => r == origin).ToList() : sieges
        };
    }
}
