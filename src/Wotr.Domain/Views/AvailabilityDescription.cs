using Wotr.Contracts;

namespace Wotr.Domain;

internal static class AvailabilityDescription
{
    public static ActionAvailabilityView Create(MatchState state, Side viewer, string subject, string action,
        string title, string source, string cost, string timing, bool window, params AvailabilityConditionView[] conditions)
    {
        var met = conditions.All(c => c.Met);
        return new()
        {
            SubjectId = subject, ActionId = action, Title = title, Source = source,
            Cost = cost, Timing = timing, ConditionsMet = met,
            WindowOpen = window && state.Winner is null && state.Pending?.Owner == viewer,
            CanAct = met && window && state.Winner is null && state.Pending?.Owner == viewer,
            Conditions = [.. conditions]
        };
    }

    public static AvailabilityConditionView Condition(string category, string code, string text, bool met) =>
        new() { Category = category, Code = code, Description = text, Met = met };

    public static bool ActionWindow(MatchState state, Side viewer) => state.Phase == GamePhase.ActionResolution &&
        state.Pending is { Kind: DecisionKind.Action } pending && pending.Owner == viewer;
}
