using Wotr.Contracts;

namespace Wotr.Domain;

public static class ShadowArmyEvents
{
    public static bool Supports(ArmyEventOrder order) => Optional(order) || Siege(order);
    public static bool Optional(ArmyEventOrder? order) => order is ArmyEventOrder.NazgulManeuver or ArmyEventOrder.WitchKingManeuver or ArmyEventOrder.BalrogManeuver or ArmyEventOrder.DolGuldurManeuver;

    public static bool Siege(ArmyEventOrder order) => order is ArmyEventOrder.GrondSiege or ArmyEventOrder.UrukHaiSiege;

    private static List<string> Required(MatchState state, ArmyEventOrder order, string region, bool garrison)
    {
        if (order == ArmyEventOrder.DolGuldurManeuver)
            return state.Units.Where(u => region == "dol-guldur" && u.Location == region && u.Garrison == garrison &&
                BoardRules.Owner(u) == Side.Shadow && BoardRules.IsArmyUnit(u)).Select(u => u.Id).ToList();
        if (order == ArmyEventOrder.BalrogManeuver)
            return state.Characters.Where(c => c.DefinitionId == LordsCharacters.Balrog && c.Location == region &&
                c.Garrison == garrison && c.Level == 2).Select(c => c.Id).ToList();
        if (order == ArmyEventOrder.UrukHaiSiege)
            return state.Units.Where(u => u.Nation == "isengard" && BoardRules.IsArmyUnit(u) &&
                u.Location == region && u.Garrison == garrison).Select(u => u.Id).ToList();
        var required = state.Characters.Where(c => CharacterRules.IsWitchKing(c) && c.Location == region &&
            c.Garrison == garrison).Select(c => c.Id).ToList();
        if (order == ArmyEventOrder.NazgulManeuver)
            required.AddRange(state.Units.Where(u => u.Kind == UnitKind.Nazgul && u.Location == region &&
                u.Garrison == garrison).Select(u => u.Id));
        return required;
    }

    public static IEnumerable<ArmyOption> Armies(MatchState state, Side actor, ArmyEventOrder order, bool attack)
    {
        if (actor != Side.Shadow || !attack && Siege(order) ||
            order == ArmyEventOrder.UrukHaiSiege && CharacterRules.InPlay(state, CharacterRules.Saruman) is null)
            yield break;
        var armies = attack ? BattleOptions.AttackingArmies(state, actor) : MovementOptions.Armies(state, actor);
        foreach (var army in armies)
        {
            var required = Required(state, order, army.RegionId, army.Garrison);
            if (required.Count == 0)
                continue;
            army.RequiredAnyIds = Siege(order) || order == ArmyEventOrder.DolGuldurManeuver ? [] : required;
            army.Targets.RemoveAll(t => Siege(order) ? t.BattleMode != BattleMode.Siege :
                !required.Any(t.EligibleIds.Contains));
            if (army.Targets.Count > 0)
                yield return army;
        }
    }

    public static ActionOption Option(MatchState state, Side actor, ArmyEventOrder order, bool attack)
    {
        var armies = Armies(state, actor, order, attack).ToList();
        return new()
        {
            Kind = attack ? CommandKind.AttackArmy : CommandKind.MoveArmies,
            Armies = armies,
            ArmyEvent = new(),
            Minimum = 1,
            Maximum = !attack && order == ArmyEventOrder.NazgulManeuver ? 2 : 1,
            EligibleIds = armies.SelectMany(a => a.UnitIds.Concat(a.CharacterIds)).Distinct().ToList(),
            Targets = armies.SelectMany(a => a.Targets.Select(t => t.RegionId)).Distinct().ToList(),
            Enabled = armies.Count > 0
        };
    }

    private static bool IncludesRequired(MatchState state, ArmyMoveChoice move, ArmyEventOrder order, bool garrison) =>
        Required(state, order, move.From, garrison).Any(move.UnitIds.Concat(move.CharacterIds).Contains);

    public static BattleState? Attack(MatchState state, Side actor, CommandPayload payload, ArmyEventOrder order)
    {
        if (actor != Side.Shadow || payload.Moves.Count != 1 || payload.DieId != "" || payload.AbilityId != "" || payload.Route.Count != 0 || payload.Moves.Any(m => m.Route.Count != 0))
            return null;
        var move = payload.Moves[0];
        var battle = BattleRules.Attack(state, actor, move, DieFace.Army);
        if (battle is null)
            return null;
        if (!Siege(order))
            return IncludesRequired(state, move, order, battle.Attacker.Garrison) ? battle : null;
        if (battle.Mode != BattleMode.Siege || Required(state, order, move.From, false).Count == 0 ||
            order == ArmyEventOrder.UrukHaiSiege && CharacterRules.InPlay(state, CharacterRules.Saruman) is null)
            return null;
        battle.MandatorySiegeRounds = 3;
        battle.FirstRoundRequiresCompanionCard = true;
        return battle;
    }

    public static MovementPlan? Move(MatchState state, Side actor, CommandPayload payload, ArmyEventOrder order)
    {
        if (actor != Side.Shadow || payload.Moves.Any(m => m.Route.Count != 0) || !Optional(order) || payload.DieId != "" || payload.AbilityId != "" || payload.Route.Count != 0 ||
            payload.Moves.Count < 1 || payload.Moves.Count > (order == ArmyEventOrder.NazgulManeuver ? 2 : 1))
            return null;
        var board = MovementPlan.CopyBoard(state);
        var moved = new HashSet<string>(StringComparer.Ordinal);
        foreach (var move in payload.Moves)
            if (!IncludesRequired(board, move, order, false) || RegionProtection.Blocks(state, actor, move.To) ||
                move.UnitIds.Concat(move.CharacterIds).Any(id => string.IsNullOrEmpty(id) || !moved.Add(id)) ||
                !MovementRules.Move(board, actor, move, DieFace.Army))
                return null;
        return new(board, payload.Moves);
    }
}
