using Wotr.Contracts;

namespace Wotr.Domain;

public sealed class HuntOptions : IActionOptionsProvider
{
    public IEnumerable<ActionOption> GetOptions(MatchState state, Side actor)
    {
        if (state.Pending?.Owner != actor)
            yield break;
        if (HuntCommands.CanMove(state, actor))
        {
            var dice = state.Dice.Where(d => d.Owner == actor && DiceRules.CanPay(d, DieFace.Character)).Select(d => d.Id).ToList();
            if (dice.Count > 0)
                yield return new() { Kind = CommandKind.MoveFellowship, EligibleDice = dice, RulerTravel = KingsFarSighted.Option(state, CommandKind.MoveFellowship) };
        }
        if (HuntRuntime.Current(state)?.Hunt is not { } hunt)
            yield break;
        if (state.Pending.Kind == DecisionKind.HuntReroll)
        {
            var failed = HuntRoll.Failed(hunt);
            yield return new()
            {
                Kind = CommandKind.RerollHunt,
                EligibleIds = failed,
                Maximum = Math.Min(hunt.RerollSources.Count, failed.Count)
            };
        }
        if (state.Pending.Kind == DecisionKind.HuntDamage)
            yield return new()
            {
                Kind = CommandKind.ResolveHuntDamage,
                HuntResponses = state.Fellowship.Companions.Count > 1
                    ? [HuntResponse.Ring, HuntResponse.Guide, HuntResponse.RandomCompanion]
                    : [HuntResponse.Ring, HuntResponse.Guide]
            };
    }

    public static HuntView? View(MatchState state)
    {
        if (HuntRuntime.Active(state) is not { } hunt)
            return null;
        return View(state, hunt);
    }

    public static bool DamagePending(HuntProcedure hunt) => hunt.Stage is HuntStage.BeforeDraw or HuntStage.Draw or
        HuntStage.ShadowReplacement or HuntStage.Replacement or HuntStage.DetermineDamage;

    public static HuntView View(MatchState state, HuntProcedure hunt) => new()
    {
        SourceId = hunt.SourceId,
        Stage = hunt.Stage.ToString(),
        Bonus = hunt.Bonus,
        MovementBonus = HasKnownRoll(hunt) ? hunt.Bonus - hunt.RollBonus : null,
        EffectBonus = HasKnownRoll(hunt) ? hunt.RollBonus : null,
        RerollBonus = hunt.RerollBonus,
        Mordor = hunt.Mordor,
        EyeDamage = hunt.EyeDamage,
        Dice = hunt.Dice.Select(d => new HuntDieView
        {
            Id = d.Id,
            First = d.First,
            Result = d.Result,
            Rerolled = d.Rerolled,
            Success = HuntRoll.Success(hunt, d)
        }).ToList(),
        RerollSources = [.. hunt.RerollSources],
        TileId = hunt.TileId,
        TileDie = hunt.TileDie,
        Damage = hunt.Damage,
        Healing = hunt.Healing,
        DamagePending = DamagePending(hunt),
        CorruptionOnly = hunt.DamageRule == HuntDamageRule.CorruptionOnly,
        SeparatesCompanions = hunt.Purpose == HuntPurpose.CompanionSeparation,
        Reveal = hunt.Reveal,
        Stop = hunt.Stop,
        CasualtyTaken = hunt.CasualtyTaken,
        GuideReturns = hunt.DamageRule != HuntDamageRule.CorruptionOnly && !hunt.Mordor &&
            CompanionRules.Inside(state).Any(c => c.Id == state.Fellowship.Guide && CharacterRules.Has(c, CharacterRules.TakenAlive)),
        PoolCount = state.HuntTiles.Count(t => t.Location == "pool"),
        Face = hunt.TileId.Length > 0 ? HuntPoolProjection.Face(hunt.TileId) : null
    };

    private static bool HasKnownRoll(HuntProcedure hunt) => !hunt.Mordor &&
        (hunt.Movement || hunt.Rolled) && hunt.Stage is not (HuntStage.BeforeRoll or HuntStage.Roll);
}
