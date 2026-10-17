using Wotr.Contracts;

namespace Wotr.Domain;

public sealed class HuntRuntime(EffectCatalog catalog)
{
    private readonly EffectRuntime effects = new(catalog);
    public static ResolutionFrame? Current(MatchState state) => state.Resolution.Frames.LastOrDefault() is { Hunt: not null } frame ? frame : null;
    public static HuntProcedure? Active(MatchState state) => state.Resolution.Frames.LastOrDefault(f => f.Hunt is not null)?.Hunt;

    public static void BeginMove(MatchState state, IRandomSource random, HuntReturn after = HuntReturn.FinishMove, int diceReduction = 0)
    {
        var hunt = Push(state, "fellowship-move", after);
        hunt.Movement = true;
        hunt.DiceReduction = diceReduction;
        if (hunt.Mordor)
        {
            MordorRules.Attempt(state);
            hunt.EyeDamage = state.Dice.Count(d => d.Location == DieLocation.Hunt);
            state.Events.Add(new() { Revision = state.Revision, Kind = "MordorMoveAttempted", Text = "The Fellowship attempts to move on the Mordor Track." });
            return;
        }
        state.Fellowship.Progress++;
        state.Events.Add(new() { Revision = state.Revision, Kind = "FellowshipMoved", Text = $"Fellowship progress: {state.Fellowship.Progress}." });
        hunt.Stage = HuntStage.BeforeRoll;
    }

    public static void BeginTile(MatchState state, string source, HuntReturn after = HuntReturn.Parent, int eyeDamage = 0,
        HuntTileRule tileRule = HuntTileRule.All, HuntDamageRule damageRule = HuntDamageRule.Normal,
        HuntPurpose purpose = HuntPurpose.Hunt)
    {
        var hunt = Push(state, source, after);
        hunt.EyeDamage = eyeDamage;
        hunt.TileRule = tileRule;
        hunt.DamageRule = damageRule;
        hunt.Purpose = purpose;
    }

    public static void BeginRoll(MatchState state, string source, IRandomSource random)
    {
        var hunt = Push(state, source, HuntReturn.Parent);
        hunt.Rolled = true;
        hunt.Stage = HuntStage.BeforeRoll;
    }

    private static HuntProcedure Push(MatchState state, string source, HuntReturn after)
    {
        var hunt = new HuntProcedure { SourceId = source, Return = after, Stage = HuntStage.BeforeDraw, Mordor = state.Fellowship.MordorStep.HasValue };
        state.Resolution.Frames.Add(new() { EffectId = "hunt", Hunt = hunt });
        HuntReports.Begin(state, hunt);
        state.Pending = null;
        return hunt;
    }

    public void Continue(MatchState state, IRandomSource random)
    {
        if (FellowshipFlow.Current(state)?.Fellowship is { AwaitingHunts: true } revelation)
        {
            ContinueRevelation(state, revelation);
            return;
        }
        var hunt = Current(state)!.Hunt!;
        switch (hunt.Stage)
        {
            case HuntStage.BeforeRoll:
                hunt.Stage = HuntStage.Roll;
                effects.BeginPublicWindow(state, Side.Shadow, hunt.SourceId, "hunt.roll");
                break;
            case HuntStage.Roll:
                hunt.Stage = HuntStage.BeforeDraw;
                HuntRoll.Roll(state, hunt, random);
                break;
            case HuntStage.BeforeDraw:
                if ((hunt.Movement || hunt.Rolled) && !hunt.Mordor && hunt.EyeDamage == 0)
                    hunt.Stage = HuntStage.Done;
                else
                {
                    hunt.Stage = HuntStage.Draw;
                    effects.BeginPublicWindow(state, Side.FreePeoples, hunt.SourceId, "hunt.draw");
                }
                break;
            case HuntStage.Draw:
                Draw(state, hunt, random);
                break;
            case HuntStage.Replacement:
                hunt.Stage = HuntStage.DetermineDamage;
                effects.BeginPublicWindow(state, Side.FreePeoples, hunt.TileId, "hunt.replace");
                break;
            case HuntStage.ShadowReplacement:
                hunt.Stage = HuntStage.Replacement;
                effects.BeginPublicWindow(state, Side.Shadow, hunt.TileId, WeShallGetIt.Window);
                break;
            case HuntStage.DetermineDamage:
                DetermineDamage(state, hunt, random);
                break;
            case HuntStage.Table:
                hunt.Stage = HuntStage.Guide;
                effects.BeginPublicWindow(state, Side.FreePeoples, hunt.TileId, "hunt.table");
                break;
            case HuntStage.Guide:
                Guide(state, hunt);
                break;
            case HuntStage.Damage:
                Damage(state, hunt, random);
                break;
            case HuntStage.Corruption:
                if (HobbitDeparture.PlaceGroup(state, hunt))
                    break;
                hunt.Stage = HuntStage.Reveal;
                HuntDamage.ApplyCorruption(state, hunt);
                break;
            case HuntStage.Reveal:
                Reveal(state, hunt);
                break;
            case HuntStage.Done:
                Complete(state, hunt);
                break;
        }
    }

    private static void Draw(MatchState state, HuntProcedure hunt, IRandomSource random)
    {
        var tile = HuntPool.Draw(state, random);
        hunt.TileId = tile.Id;
        hunt.DepartureOrigin = state.Fellowship.RegionId;
        hunt.DepartureProgress = state.Fellowship.Progress;
        hunt.Stage = WeShallGetIt.AfterDraw(state);
        state.Events.Add(new() { Revision = state.Revision, Kind = "HuntTileDrawn", Text = $"{tile.Id}: Hunt tile drawn." });
        WeWontGoBackStep.Begin(state, tile.Id);
    }

    private static void DetermineDamage(MatchState state, HuntProcedure hunt, IRandomSource random)
    {
        if (FireAndShadow.ResolveEye(state, hunt))
            return;
        var definition = HuntPool.Definitions[hunt.TileId];
        if (hunt.TileRule != HuntTileRule.All && definition.Value == "eye" ||
            hunt.TileRule == HuntTileRule.IgnoreEyesAndFellowship && definition.Pool == "special-fp")
        {
            HuntPool.Discard(state, hunt.TileId);
            hunt.Stage = HuntStage.Done;
            state.Events.Add(new() { Revision = state.Revision, Kind = "HuntTileIgnored", Text = $"{hunt.SourceId}: {hunt.TileId} discarded without effect." });
            return;
        }
        hunt.TileDie = definition.Value == "die" ? random.Next(6) + 1 : null;
        var value = definition.Value == "eye" ? hunt.EyeDamage : hunt.TileDie ?? int.Parse(definition.Value, System.Globalization.CultureInfo.InvariantCulture);
        hunt.Damage = Math.Max(0, value);
        if (hunt.Purpose == HuntPurpose.CompanionSeparation)
        {
            hunt.Stage = HuntStage.Done;
            HuntPool.Discard(state, hunt.TileId);
            EffectRuntime.BeginProgram(state, Side.FreePeoples, hunt.SourceId, ShadowCharacterCards03.BreakingChoice);
            return;
        }
        hunt.Healing = Math.Max(0, -value);
        hunt.Reveal = definition.Reveal && !hunt.IgnoreRevealIcon;
        GollumHunt.IgnoreNumberedReveal(state, hunt);
        hunt.Stop = definition.Stop && hunt.Movement;
        hunt.Stage = HuntStage.Table;
        state.Events.Add(new() { Revision = state.Revision, Kind = "HuntDamageDetermined", Text = $"{hunt.TileId}: damage {hunt.Damage}, healing {hunt.Healing}." });
    }

    private void Guide(MatchState state, HuntProcedure hunt)
    {
        if (hunt.CasualtyEffectsPending)
        {
            hunt.CasualtyEffectsPending = false;
            if (effects.BeginPublicWindow(state, Side.Shadow, hunt.SourceId, "hunt.casualty"))
                return;
        }
        GollumHunt.IgnoreNumberedReveal(state, hunt);
        var guide = state.Fellowship.Guide;
        if (!hunt.OfferedGuides.Contains(guide))
        {
            hunt.OfferedGuides.Add(guide);
            if (effects.BeginPublicWindow(state, Side.FreePeoples, guide, "hunt.guide"))
                return;
        }
        hunt.Stage = HuntStage.Damage;
    }

    private static void Damage(MatchState state, HuntProcedure hunt, IRandomSource random)
    {
        if (hunt.DamageRule == HuntDamageRule.CorruptionOnly || hunt.Damage == 0 || hunt.CasualtyTaken || state.Fellowship.Companions.Count == 0)
        {
            hunt.Stage = HuntStage.Corruption;
            return;
        }
        if (hunt.DamageRule == HuntDamageRule.RandomCompanionFirst)
        {
            HuntDamage.Resolve(state, hunt, HuntResponse.RandomCompanion, random);
            return;
        }
        state.Open(DecisionKind.HuntDamage, Side.FreePeoples, [CommandKind.ResolveHuntDamage]);
        state.Pending!.SourceId = hunt.TileId;
    }

    private static void Reveal(MatchState state, HuntProcedure hunt)
    {
        HuntPool.Discard(state, hunt.TileId);
        hunt.Stage = HuntStage.Done;
        if (hunt.DeclarationHunt)
        {
            if (hunt.Reveal && state.Fellowship.Hidden)
                FellowshipFlow.RevealDeclaration(state);
            return;
        }
        if (hunt.Mordor)
        {
            if (hunt.Reveal)
            {
                ActiveEffects.FellowshipLocated(state);
                state.Fellowship.Hidden = false;
                state.Events.Add(new() { Revision = state.Revision, Kind = "FellowshipRevealed", Text = "The Fellowship is revealed on the Mordor Track." });
            }
            return;
        }
        if (hunt.Reveal && state.Fellowship.Hidden)
            FellowshipFlow.BeginReveal(state, hunt.TileId);
    }

    private void ContinueRevelation(MatchState state, FellowshipProcedure revelation)
    {
        if (revelation.RevealEffectsPending)
        {
            revelation.RevealEffectsPending = false;
            var window = revelation.Kind == FellowshipProcedureKind.Declaration ? "fellowship.declared" : "fellowship.revealed";
            if (effects.BeginPublicWindow(state, Side.Shadow, revelation.SourceId, window))
                return;
        }
        if (revelation.FireAndShadowPending)
            FireAndShadow.Begin(state, revelation);
        else if (revelation.AdditionalHuntRegions.Count > 0)
            BeginTile(state, revelation.AdditionalHuntRegions[0], HuntReturn.Stronghold);
        else
            FellowshipFlow.CompleteRevelation(state);
    }

    private static void Complete(MatchState state, HuntProcedure hunt)
    {
        if (hunt.Mordor && hunt.Movement)
        {
            MordorRules.CompleteMove(state, hunt);
            HuntReports.Capture(state);
            if (RingVictory.Check(state))
                return;
        }
        HuntReports.Capture(state);
        HuntReports.Record(state, hunt, HuntReportKind.HuntCompleted);
        state.Resolution.Frames.RemoveAt(state.Resolution.Frames.Count - 1);
        if (hunt.Return == HuntReturn.FinishMove)
            TurnFlow.FinishAction(state, Side.FreePeoples, true);
        else if (hunt.Return == HuntReturn.Stronghold)
            FellowshipFlow.AdditionalHuntCompleted(state);
        if (state.Resolution.Frames.Count == 0)
            HuntPool.CompleteAction(state);
    }
}
