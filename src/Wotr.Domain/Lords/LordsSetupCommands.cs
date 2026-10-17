using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public sealed class LordsSetupCommands : ICommandHandler, IActionOptionsProvider
{
    public bool Handles(CommandKind kind) => kind == CommandKind.ChooseCompanionVersions;

    public string? Validate(MatchState state, Side actor, GameCommand command)
    {
        var p = command.Payload;
        return state.Configuration.Lords && state.Lords is not null && state.Phase == GamePhase.Setup &&
            actor == Side.FreePeoples && state.Pending?.Kind == DecisionKind.CouncilVersions && p.DieId == "" &&
            CouncilCompanions.ValidVersions(state, p.Ids) && LordsCompanionSetup.StartingGuides.Contains(p.GuideId)
            ? null : "IllegalChoice";
    }

    public void Apply(MatchState state, Side actor, GameCommand command, IRandomSource random) =>
        LordsCompanionSetup.Choose(state, command.Payload.Ids, command.Payload.GuideId);

    public IEnumerable<ActionOption> GetOptions(MatchState state, Side actor)
    {
        if (!state.Configuration.Lords || state.Phase != GamePhase.Setup ||
            state.Pending is not { Kind: DecisionKind.CouncilVersions, Owner: Side.FreePeoples } || actor != Side.FreePeoples)
            yield break;
        var groups = CouncilCompanions.AvailableVersions(state).ToArray();
        var versions = groups.SelectMany(v => new[] { v.BaseId, v.AlternateId }).ToList();
        yield return new()
        {
            Kind = CommandKind.ChooseCompanionVersions,
            EligibleIds = versions,
            CompanionVersions = groups.Select(v => new CompanionVersionOption
            {
                FigureId = v.FigureId,
                BaseId = v.BaseId,
                AlternateId = v.AlternateId,
                Required = v.FigureId == CharacterRules.Grey
            }).ToList(),
            Targets = [.. LordsCompanionSetup.StartingGuides],
            Minimum = 1,
            Maximum = groups.Length,
            CharacterActions = versions.Select(id => new CharacterActionOption
            {
                CharacterId = id,
                Title = CharacterCatalog.All[id].Title,
                Detail = id switch
                {
                    FateGimli.Standard => "Son of Gloin",
                    FateGimli.Alternate => "Dwarven Lord",
                    _ => id.StartsWith("lords.", StringComparison.Ordinal) ? "Lords version" : "Base version"
                }
            }).ToList()
        };
    }
}
