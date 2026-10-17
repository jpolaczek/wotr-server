using Wotr.Contracts;

namespace Wotr.Domain;

public static class BattleRoll
{
    public static bool Hits(int raw, int modifier, int target) => raw == 6 || raw != 1 && raw + modifier >= target;
    public static int DiceCount(MatchState state, BattleForce force) => !BattleRules.Alive(state, force) ? 0 : Math.Max(1, Math.Clamp(
        BattleRules.Strength(state, force) + BattleRules.CaptainBonus(state, force) + force.Round.StrengthBonus,
        1, Math.Clamp(force.Round.CombatDiceLimit, 1, 5)) - force.Round.CombatDicePenalty - force.CombatDiceReduction);
    public static int HitCount(BattleForce force) => Math.Max(0, force.Round.Dice.Count(d => d.Hit || d.ConvertedHit || d.RerollHit) +
        (force.Round.DoubleSixes ? force.Round.Dice.Count(d => d.Result == 6) + force.Round.Dice.Count(d => d.Reroll == 6) : 0) +
        force.Round.ExtraHits - force.Round.SpentHits);
    public static List<string> Failed(BattleForce force) => force.Round.Dice.Where(d => !d.Hit && !d.ConvertedHit && !d.CannotReroll).Select(d => d.Id).ToList();
    public static int RerollMaximum(MatchState state, BattleForce force) => Math.Min(Failed(force).Count,
        Math.Clamp(BattleLeadership.Total(state, force), 0, Math.Clamp(force.Round.RerollLimit, 0, 5)));

    public static void Combat(MatchState state, BattleState battle, IRandomSource random)
    {
        foreach (var force in new[] { battle.Attacker, battle.Defender })
        {
            force.Round.Dice = Roll(DiceCount(state, force), force.Round.CombatModifier, BattleRules.HitTarget(state, battle, force),
                $"{battle.RoundScope}:{force.Side}", random);
            Record(state, battle, force.Side, "CombatRoll", force.Round.Dice.Select(d => d.Result));
        }
    }

    public static void Reroll(MatchState state, BattleState battle, IRandomSource random)
    {
        foreach (var force in new[] { battle.Attacker, battle.Defender })
        {
            foreach (var die in force.Round.Dice.Where(d => force.Round.SelectedRerolls.Contains(d.Id)))
            {
                die.Reroll = random.Next(6) + 1;
                die.RerollModifier = force.Round.RerollModifier;
                die.RerollHit = Hits(die.Reroll.Value, die.RerollModifier, die.HitTarget);
            }
            Record(state, battle, force.Side, "LeaderReroll", force.Round.Dice.Where(d => d.Reroll.HasValue).Select(d => d.Reroll!.Value));
        }
    }

    // Special attacks explicitly provide their modifiers; opposing combat effects never leak into these dice.
    public static void Special(MatchState state, BattleState battle, Side actor, string source, int count,
        IRandomSource random, int? target = null, int modifier = 0)
    {
        var force = battle.Force(actor);
        var dice = Roll(count, modifier, target ?? BattleRules.HitTarget(state, battle, force), $"{battle.RoundScope}:special:{battle.SpecialAttacks.Count}", random);
        battle.SpecialAttacks.Add(new(actor, source, dice));
        var hits = dice.Count(d => d.Hit);
        force.Round.SpecialHits += hits;
        Record(state, battle, actor, "SpecialAttack", dice.Select(d => d.Result));
        BattleLosses.Begin(state, battle.Opponent(actor), hits, source);
    }

    private static List<BattleDie> Roll(int count, int modifier, int target, string prefix, IRandomSource random) =>
        Enumerable.Range(0, Math.Max(0, count)).Select(index =>
        {
            var raw = random.Next(6) + 1;
            return new BattleDie { Id = $"{prefix}:{index}", Result = raw, Modifier = modifier, HitTarget = target, Hit = Hits(raw, modifier, target) };
        }).ToList();

    private static void Record(MatchState state, BattleState battle, Side side, string kind, IEnumerable<int> results) =>
        state.Events.Add(new() { Revision = state.Revision, Kind = kind,
            Activity = HistoryFacts.Battle(state, battle),
            Text = $"{side}: {kind} [{string.Join(", ", results)}]." });
}
