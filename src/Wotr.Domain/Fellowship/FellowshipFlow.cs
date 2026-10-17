using Wotr.Contracts;

namespace Wotr.Domain;

public static class FellowshipFlow
{
    public static ResolutionFrame? Current(MatchState state) => state.Resolution.Frames.LastOrDefault() is { Fellowship: not null } frame ? frame : null;
    public static FellowshipProcedure? Active(MatchState state) => state.Resolution.Frames.LastOrDefault(f => f.Fellowship is not null)?.Fellowship;

    public static bool CanDeclare(MatchState state) => state.Phase == GamePhase.Fellowship && state.Pending?.Kind == DecisionKind.FellowshipPhase &&
        state.Fellowship.Hidden && !state.Fellowship.MordorStep.HasValue && state.Fellowship.LastDeclarationTurn != state.Turn;

    public static bool ValidRoute(MatchState state, IReadOnlyList<string> route, bool reveal) =>
        !state.Fellowship.MordorStep.HasValue && TravelRules.ValidPath(route, state.Fellowship.RegionId, state.Fellowship.Progress) &&
        (!reveal || !TravelRules.Sanctuary(state, route[^1]));

    public static void Declare(MatchState state, IReadOnlyList<string> route)
    {
        ActiveEffects.FellowshipLocated(state);
        Locate(state, route, false);
        state.Fellowship.LastDeclarationTurn = state.Turn;
        CaptainOfDespair.Declared(state);
        if (TravelRules.Sanctuary(state, state.Fellowship.RegionId))
        {
            state.Fellowship.Corruption = Math.Max(0, state.Fellowship.Corruption - 1);
            ActiveEffects.RemoveWhere(state, e => e.ExpiresWhen == EffectExpiry.FellowshipDeclaresSanctuary);
        }
        PoliticalRules.FellowshipDeclared(state, state.Fellowship.RegionId);
        TurnFlow.OpenFellowshipPhase(state);
        Push(state, FellowshipProcedureKind.Declaration, "fellowship", FellowshipReturn.Decision);
        var procedure = Current(state)!.Fellowship!;
        procedure.Route = [.. route];
        procedure.FireAndShadowPending = FireAndShadow.Applies(state, route);
        procedure.RevealEffectsPending = true;
        procedure.AwaitingHunts = true;
        if (SmeagolRules.Declared(state))
            CompositionChanged(state, FellowshipReturn.Parent);
    }

    public static void Hide(MatchState state)
    {
        state.Fellowship.Hidden = true;
        MordorRules.Attempt(state);
        state.Events.Add(new() { Revision = state.Revision, Kind = "FellowshipHidden", Text = "The Fellowship is hidden." });
    }

    public static void BeginReveal(MatchState state, string source, FellowshipReturn after = FellowshipReturn.Parent)
    {
        Push(state, FellowshipProcedureKind.Reveal, source, after);
        ActiveEffects.FellowshipLocated(state);
        state.Fellowship.Hidden = false;
        state.Open(DecisionKind.FellowshipRoute, Side.FreePeoples, [CommandKind.RevealFellowship]);
        state.Pending!.SourceId = source;
    }

    public static void Reveal(MatchState state, IReadOnlyList<string> route)
    {
        var procedure = Current(state)!.Fellowship!;
        procedure.Route = [.. route];
        procedure.FireAndShadowPending = FireAndShadow.Applies(state, route);
        procedure.AdditionalHuntRegions = TravelRules.AdditionalHunts(state, route);
        Locate(state, route, true);
        state.Pending = null;
        procedure.RevealEffectsPending = true;
        procedure.AwaitingHunts = true;
    }

    public static void AdditionalHuntCompleted(MatchState state)
    {
        var procedure = Current(state)?.Fellowship ?? throw new InvalidOperationException("Missing revelation continuation.");
        if (!procedure.AwaitingHunts || procedure.AdditionalHuntRegions.Count == 0)
            throw new InvalidOperationException("No additional Hunt to complete.");
        procedure.AdditionalHuntRegions.RemoveAt(0);
        if (procedure.AdditionalHuntRegions.Count == 0)
            Complete(state);
    }

    public static void RevealDeclaration(MatchState state)
    {
        var procedure = Active(state)!;
        ActiveEffects.FellowshipLocated(state);
        state.Fellowship.Hidden = false;
        procedure.AdditionalHuntRegions = TravelRules.AdditionalHunts(state, procedure.Route);
        state.Events.Add(new() { Revision = state.Revision, Kind = "FellowshipRevealed", Text = "The Fellowship is revealed after its declaration." });
    }

    public static void CompleteRevelation(MatchState state)
    {
        var procedure = Current(state)?.Fellowship;
        if (procedure is not { AwaitingHunts: true, RevealEffectsPending: false } || procedure.AdditionalHuntRegions.Count != 0)
            throw new InvalidOperationException("Revelation has unresolved effects.");
        Complete(state);
    }

    public static void BeginSeparation(MatchState state, string source, FellowshipReturn after = FellowshipReturn.Parent)
    {
        Push(state, FellowshipProcedureKind.Separation, source, after);
        state.Open(DecisionKind.CompanionSeparation, Side.FreePeoples, [CommandKind.SeparateCompanions]);
        state.Pending!.SourceId = source;
    }

    public static void BeginPlacement(MatchState state, string source, IReadOnlyList<string> ids, string origin, int range, bool chooseGuide)
    {
        BeginSeparation(state, source);
        var procedure = Current(state)!.Fellowship!;
        procedure.FixedCompanions = [.. ids];
        procedure.SeparationOrigin = origin;
        procedure.SeparationRange = range;
        procedure.ChooseGuideAfterPlacement = chooseGuide;
    }

    public static void BeginJourney(MatchState state, string source, CompanionJourney journey)
    {
        BeginSeparation(state, source);
        Current(state)!.Fellowship!.Journey = journey;
        state.Open(DecisionKind.CompanionSeparation, Side.FreePeoples, journey.AllowSeparation ? [CommandKind.SeparateCompanions, CommandKind.MoveCompanions] : [CommandKind.MoveCompanions]);
        state.Pending!.SourceId = source;
    }

    public static void FinishPlacement(MatchState state)
    {
        if (Current(state)!.Fellowship!.ChooseGuideAfterPlacement)
            CompositionChanged(state, FellowshipReturn.Parent);
        else
            Complete(state);
    }

    public static void CompositionChanged(MatchState state, FellowshipReturn after)
    {
        SmeagolRules.Reconcile(state);
        if (Current(state)?.Fellowship?.Kind != FellowshipProcedureKind.Separation)
            Push(state, FellowshipProcedureKind.Guide, "fellowship", after);
        else
            Current(state)!.Fellowship!.Kind = FellowshipProcedureKind.Guide;
        var guides = CompanionRules.Guides(state);
        if (guides.Count == 1)
        {
            SelectGuide(state, guides[0]);
            return;
        }
        state.Open(DecisionKind.Guide, Side.FreePeoples, [CommandKind.SelectGuide]);
        state.Pending!.PrivateChoices = guides;
    }

    public static void SelectGuide(MatchState state, string id)
    {
        if (state.Lords is not null && id == CharacterRules.Gollum)
            state.Lords.GollumEntered = true;
        LordsCompanionSetup.Reveal(state, Content.CharacterCatalog.Companion(state, id));
        if (state.Fellowship.Guide != id)
        {
            state.Fellowship.Guide = id;
            state.Events.Add(new() { Revision = state.Revision, Kind = "GuideChanged", Text = $"The Fellowship is guided by {Content.CharacterCatalog.Title(id)}." });
        }
        if (state.Pending?.Kind == DecisionKind.FellowshipPhase)
            TurnFlow.OpenHuntAllocation(state);
        else
            Complete(state);
    }

    private static void Locate(MatchState state, IReadOnlyList<string> route, bool reveal)
    {
        state.Fellowship.RegionId = route[^1];
        state.Fellowship.Progress = 0;
        state.Fellowship.Hidden = !reveal;
        state.Events.Add(new()
        {
            Revision = state.Revision,
            Kind = reveal ? "FellowshipRevealed" : "FellowshipDeclared",
            Text = $"The Fellowship {(reveal ? "is revealed" : "declares")} in {route[^1]} via {string.Join(" → ", route)}."
        });
    }

    private static void Push(MatchState state, FellowshipProcedureKind kind, string source, FellowshipReturn after)
    {
        state.Resolution.Frames.Add(new()
        {
            EffectId = "fellowship." + kind,
            ReturnDecision = after == FellowshipReturn.Decision ? state.Pending : null,
            Fellowship = new() { Kind = kind, Return = after, SourceId = source }
        });
        state.Pending = null;
    }

    public static void Complete(MatchState state)
    {
        var frame = Current(state) ?? throw new InvalidOperationException("Missing Fellowship continuation.");
        state.Resolution.Frames.RemoveAt(state.Resolution.Frames.Count - 1);
        state.Pending = null;
        if (state.Resolution.Frames.Count == 0)
            HuntPool.CompleteAction(state);
        switch (frame.Fellowship!.Return)
        {
            case FellowshipReturn.Decision:
                state.Pending = frame.ReturnDecision ?? throw new InvalidOperationException("Missing return decision.");
                break;
            case FellowshipReturn.FinishAction:
                TurnFlow.FinishAction(state, Side.FreePeoples);
                break;
            case FellowshipReturn.EndPhase:
                TurnFlow.OpenHuntAllocation(state);
                break;
        }
    }
}
