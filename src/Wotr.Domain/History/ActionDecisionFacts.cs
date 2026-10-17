using Wotr.Contracts;

namespace Wotr.Domain;

internal static class ActionDecisionFacts
{
    public static int Ensure(MatchState before, MatchState after, Side actor, GameCommand command, int first)
    {
        var kind = command.Kind;
        var expected = kind switch
        {
            CommandKind.MoveArmies => "ArmyMoved",
            CommandKind.Recruit => "UnitRecruited",
            CommandKind.CommitCombatCard or CommandKind.SetCombatCardPolicy => "CombatCardCommitted",
            CommandKind.PlayEvent => "EventPlayed",
            CommandKind.AttackArmy => "BattleStarted",
            CommandKind.ChooseCompanionVersions => "CompanionVersionsChosen",
            _ => ""
        };
        for (var i = first; expected.Length > 0 && i < after.Events.Count; i++)
            if (after.Events[i].Kind == expected)
                return i;
        after.Events.Insert(first, new()
        {
            Revision = after.Revision,
            Kind = kind == CommandKind.FinishActionSequence ? "ActionFinished" : "ActionDecision",
            OnlyFor = ActionCommandCatalog.PrivateSelection(kind) ? actor : null,
            Text = ActionCommandText.Describe(kind),
            Activity = new() { Turn = before.Turn }
        });
        return first;
    }

    public static ActionContext Context(MatchState before, MatchState after, GameCommand command)
    {
        var battle = BattleRuntime.Active(before) ?? BattleRuntime.Active(after);
        var kind = ActionCommandCatalog.Context(command.Kind);
        var result = new ActionContext
        {
            Kind = battle is not null ? ActionContextKind.Battle : kind,
            BattleId = battle?.Id ?? "",
            BattleRound = battle?.Round,
            HuntChainId = after.HuntReport is { } hunt && (hunt.StartedRevision == after.Revision || !hunt.Completed ||
                before.HuntReport?.Completed == false) ? hunt.Id : "",
            NationId = command.Payload.NationId,
            DefinitionId = command.Kind == CommandKind.PlayEvent ? command.Payload.CardId :
                command.Kind is CommandKind.UseCharacterAbility or CommandKind.UseCouncilToken or CommandKind.MoveArmies or
                    CommandKind.MoveShadowCharacters or CommandKind.DrawCard ? command.Payload.AbilityId : ""
        };
        if (battle is not null)
            result.Regions = [battle.Region];
        else
            result.Regions = command.Payload.Moves.SelectMany(m => new[] { m.From, m.To })
                .Concat(command.Payload.Placements.Select(p => p.RegionId))
                .Concat(command.Payload.CompanionMoves.SelectMany(m => m.Route))
                .Append(command.Payload.RegionId).Where(BoardRules.Regions.ContainsKey).Distinct().ToList();
        if (result.DefinitionId.Length == 0 && EffectRuntime.Current(before)?.Effect is { } effect)
        {
            result.DefinitionId = effect.SourceId;
            if (result.Regions.Count == 0 && BoardRules.Regions.ContainsKey(effect.RegionId))
                result.Regions.Add(effect.RegionId);
        }
        if (result.Regions.Count == 0 && kind is ActionContextKind.Fellowship or ActionContextKind.Hunt &&
            after.Fellowship.MordorStep is null)
            result.Regions.Add(after.Fellowship.RegionId);
        return result;
    }

    public static void Movements(MatchState before, MatchState after, GameCommand command)
    {
        var movements = MovementFacts.Capture(before, command.Payload);
        var events = after.Events.Skip(before.Events.Count).Where(e => e.Kind == "ArmyMoved").ToArray();
        for (var i = 0; i < movements.Count && i < events.Length; i++)
        {
            var data = events[i].Activity ??= new() { Turn = before.Turn };
            data.Actor = before.Pending!.Owner;
            data.Command = command.Kind;
            data.Automatic = false;
            if (data.Movements.Count == 0)
            {
                data.Movements = [movements[i]];
                data.Context = new() { Kind = ActionContextKind.Army, Regions = [movements[i].From, movements[i].To] };
            }
        }
    }
}
