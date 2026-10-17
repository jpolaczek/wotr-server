using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public sealed class CouncilCommands : ICommandHandler, IActionOptionsProvider, ICommandPreviewProvider
{
    public bool Handles(CommandKind kind) => kind is CommandKind.ChooseCouncilHomelands or
        CommandKind.ChooseCouncilToken or CommandKind.UseCouncilToken;

    public string? Validate(MatchState state, Side actor, GameCommand command)
    {
        var p = command.Payload;
        if (!state.Configuration.Council || state.Lords?.Council is null || p.DieId != "" || p.RingId != "")
            return "IllegalChoice";
        var valid = command.Kind switch
        {
            CommandKind.ChooseCouncilHomelands => actor == Side.FreePeoples && state.Turn == 1 &&
                state.Phase == GamePhase.Fellowship && state.Pending?.Kind == DecisionKind.CouncilHomelands &&
                !state.Lords.Council.HomelandsChosen && CouncilSetup.ValidHomelands(state, p),
            CommandKind.ChooseCouncilToken => actor == Side.Shadow && state.Pending?.Kind == DecisionKind.CouncilToken &&
                state.Lords.Council.GrantedTokens.Count == 0 && DraftSelection.IsSubset(p.Ids, CouncilTokens.All, 1, 1),
            CommandKind.UseCouncilToken => actor == Side.Shadow && state.Phase == GamePhase.ActionResolution &&
                state.Pending?.Kind == DecisionKind.Action && CouncilTokens.ValidUse(state, p),
            _ => false
        };
        return valid ? null : "IllegalChoice";
    }

    public void Apply(MatchState state, Side actor, GameCommand command, IRandomSource random)
    {
        switch (command.Kind)
        {
            case CommandKind.ChooseCouncilHomelands:
                CouncilSetup.ChooseHomelands(state, command.Payload.Placements);
                break;
            case CommandKind.ChooseCouncilToken:
                CouncilSetup.Grant(state, command.Payload.Ids);
                TurnFlow.OpenFellowshipPhase(state);
                break;
            case CommandKind.UseCouncilToken:
                CouncilTokens.Use(state, command.Payload);
                break;
        }
    }

    public void DescribePreview(MatchState state, GameCommand command, ActionPreview preview) =>
        CouncilPreview.Describe(state, command, preview);

    public IEnumerable<ActionOption> GetOptions(MatchState state, Side actor)
    {
        if (!state.Configuration.Council || state.Pending?.Owner != actor)
            yield break;
        if (state.Pending.Kind == DecisionKind.CouncilHomelands)
            yield return Homelands(state);
        else if (state.Pending.Kind == DecisionKind.CouncilToken)
            yield return new() { Kind = CommandKind.ChooseCouncilToken, EligibleIds = [.. CouncilTokens.All], Minimum = 1, Maximum = 1 };
        else if (state.Pending.Kind == DecisionKind.Action && actor == Side.Shadow && !state.Player(actor).RingActionPending)
            foreach (var option in Tokens(state))
                yield return option;
    }

    private static ActionOption Homelands(MatchState state)
    {
        var characters = CouncilCompanions.CanStartOutside(state).ToArray();
        return new()
        {
            Kind = CommandKind.ChooseCouncilHomelands,
            EligibleIds = characters.Select(c => c.Id).ToList(),
            Maximum = characters.Length,
            AllowEmptySelection = true,
            CharacterActions = characters.Select(c => new CharacterActionOption
            {
                CharacterId = c.Id,
                Title = CharacterCatalog.All[c.DefinitionId].Title,
                Targets = [.. CouncilCompanions.Homelands(c)]
            }).ToList()
        };
    }

    private static IEnumerable<ActionOption> Tokens(MatchState state)
    {
        if (CouncilTokens.Available(state, CouncilTokens.Politics))
        {
            var nations = CouncilTokens.Nations(state);
            yield return new() { Kind = CommandKind.UseCouncilToken, EligibleIds = [CouncilTokens.Politics],
                Targets = nations, Enabled = nations.Count > 0, Reason = "Advance a Shadow nation." };
        }
        if (CouncilTokens.Available(state, CouncilTokens.Travel))
        {
            var travel = ShadowTravel.Options(state).ToList();
            yield return new() { Kind = CommandKind.UseCouncilToken, EligibleIds = [CouncilTokens.Travel],
                Travel = travel, Enabled = travel.Count > 0, Reason = "Move Nazgûl and Minions." };
        }
    }
}
