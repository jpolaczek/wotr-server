using Wotr.Contracts;

namespace Wotr.Domain;

public static class BattleRules
{
    public static IEnumerable<UnitState> Units(MatchState state, BattleForce force) => state.Units.Where(u =>
        force.Participants.Contains(u.Id) && u.Location == force.Region && u.Garrison == force.Garrison && BoardRules.Owner(u) == force.Side);

    public static IEnumerable<CharacterState> Characters(MatchState state, BattleForce force) => state.Characters.Where(c =>
        force.Participants.Contains(c.Id) && c.Location == force.Region && c.Garrison == force.Garrison && c.Side == force.Side);

    public static int Strength(MatchState state, BattleForce force) => Units(state, force).Count(BoardRules.IsArmyUnit);
    public static int CaptainBonus(MatchState state, BattleForce force) => Characters(state, force)
        .Where(c => !force.Round.CancelledAbilities.Contains(c.Id))
        .Sum(c => CharacterRules.Has(c, CharacterRules.Captain) ? 1 : CharacterRules.Has(c, LordsCharacters.Flame) ? 2 : CouncilAbilities.ChampionBonus(state, c));
    public static bool WhiteRiderActive(MatchState state) => BattleRuntime.Active(state) is { WhiteRiderUsed: true } battle &&
        !state.Characters.Any(c => c.DefinitionId == CharacterRules.White &&
            battle.Force(Side.FreePeoples).Round.CancelledAbilities.Contains(c.Id));
    public static bool Alive(MatchState state, BattleForce force) => Units(state, force).Any(BoardRules.IsArmyUnit);
    public static bool AtWar(MatchState state, UnitState unit) => unit.Kind == UnitKind.Nazgul ||
        state.Nations.Single(n => n.Id == unit.Nation).StepsToWar == 0;

    public static BattleState? Attack(MatchState state, Side actor, ArmyMoveChoice move, DieFace cost)
    {
        if (RegionProtection.Blocks(state, actor, move.To) || cost is not (DieFace.Army or DieFace.Character) || Mode(state, actor, move.From, move.To) is not { } mode)
            return null;
        var participants = move.UnitIds.Concat(move.CharacterIds).ToHashSet(StringComparer.Ordinal);
        var units = state.Units.Where(u => move.UnitIds.Contains(u.Id)).ToList();
        var characters = state.Characters.Where(c => move.CharacterIds.Contains(c.Id)).ToList();
        if (participants.Count != move.UnitIds.Count + move.CharacterIds.Count || units.Count != move.UnitIds.Count ||
            characters.Count != move.CharacterIds.Count || !units.Any(BoardRules.IsArmyUnit) ||
            units.Any(u => u.Location != move.From || u.Garrison != (mode == BattleMode.Sortie) || BoardRules.Owner(u) != actor || !AtWar(state, u)) ||
            characters.Any(c => c.Location != move.From || c.Garrison != (mode == BattleMode.Sortie) || c.Side != actor))
            return null;
        if (cost == DieFace.Character && characters.Count == 0 && !units.Any(u => CharacterRules.Leads(state, u)))
            return null;
        var rear = state.Units.Where(u => u.Location == move.From && u.Garrison == (mode == BattleMode.Sortie) && BoardRules.Owner(u) == actor && !participants.Contains(u.Id)).ToList();
        var rearCharacters = state.Characters.Any(c => c.Location == move.From && c.Garrison == (mode == BattleMode.Sortie) && c.Side == actor && !participants.Contains(c.Id));
        if ((rear.Count > 0 || rearCharacters) && !rear.Any(BoardRules.IsArmyUnit))
            return null;
        return new()
        {
            Region = move.To,
            Mode = mode,
            Attacker = new() { Side = actor, Region = move.From, Garrison = mode == BattleMode.Sortie, Participants = [.. participants] },
            Defender = new()
            {
                Side = TurnFlow.Other(actor),
                Region = move.To,
                Garrison = mode == BattleMode.Siege,
                Participants = state.Units.Where(u => u.Location == move.To && u.Garrison == (mode == BattleMode.Siege) && BoardRules.Owner(u) != actor).Select(u => u.Id)
                    .Concat(state.Characters.Where(c => c.Location == move.To && c.Garrison == (mode == BattleMode.Siege) && c.Side != actor).Select(c => c.Id)).ToList()
            }
        };
    }

    public static BattleState? PaidAttack(MatchState state, Side actor, CommandPayload payload)
    {
        var die = DiceRules.AvailableDie(state, actor, payload.DieId);
        return die is not null && CharacterCosts.CanPayArmy(state, actor, payload) && payload.Moves.Count == 1
            ? Attack(state, actor, payload.Moves[0], payload.MovementCost) : null;
    }

    private static BattleMode? Mode(MatchState state, Side actor, string from, string to)
    {
        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to) || !BoardRules.Regions.TryGetValue(from, out var origin))
            return null;
        if (from == to)
        {
            if (BoardRules.Besieged(state, from, actor) && state.Units.Any(u => u.Location == from && u.Garrison && BoardRules.Owner(u) == actor && BoardRules.IsArmyUnit(u)))
                return BattleMode.Sortie;
            return BoardRules.Besieged(state, from, TurnFlow.Other(actor)) && BoardRules.HasEnemyArmy(state, from, actor, true) ? BattleMode.Siege : null;
        }
        if (!origin.Neighbors.Contains(to) || !BoardRules.HasEnemyArmy(state, to, actor, false))
            return null;
        return BoardRules.Besieged(state, to, actor) && state.Units.Any(u => u.Location == to && u.Garrison && BoardRules.Owner(u) == actor && BoardRules.IsArmyUnit(u))
            ? BattleMode.Relief : BattleMode.Field;
    }

    public static int HitTarget(MatchState state, BattleState battle, BattleForce force) => force == battle.Attacker &&
        (battle.Mode == BattleMode.Siege || battle.Round == 1 && BoardRules.Region(state, battle.Region).Settlement is "City" or "Fortification") ? 6 : 5;
}
