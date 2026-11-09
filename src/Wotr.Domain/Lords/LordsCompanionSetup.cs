using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public static class LordsCompanionSetup
{
    public static IReadOnlyList<string> Versions { get; } = [CharacterRules.Grey, LordsCharacters.Gandalf];
    public static IReadOnlyList<string> StartingGuides { get; } = [CharacterRules.Grey, CharacterRules.Strider];

    public static void Begin(MatchState state)
    {
        if (state.Lords is null)
            LordsDiceSetup.Initialize(state);
        SmeagolTiles.Initialize(state);
        if (state.Configuration.Council)
            state.Lords!.Council = new();
        state.Open(DecisionKind.CouncilVersions, Side.FreePeoples, [CommandKind.ChooseCompanionVersions]);
        state.Pending!.PrivateChoices = CouncilCompanions.AvailableVersions(state)
            .SelectMany(v => new[] { v.BaseId, v.AlternateId }).ToList();
    }

    public static void Choose(MatchState state, IReadOnlyList<string> versions, string guide)
    {
        foreach (var version in CouncilCompanions.AvailableVersions(state))
        {
            var character = CharacterCatalog.Companion(state, version.FigureId);
            CharacterLifecycle.ApplyVersion(character, CharacterCatalog.All[CouncilCompanions.Selected(version, versions)]);
            if (!state.Characters.Contains(character))
                state.Characters.Add(character);
        }
        state.Player(Side.FreePeoples).PrivateCompanionVersions = [.. versions];
        state.Events.Add(new()
        {
            Revision = state.Revision,
            Kind = "CompanionVersionsChosen",
            OnlyFor = Side.FreePeoples,
            Text = $"Selected {string.Join(", ", versions)}."
        });
        state.Fellowship.Guide = guide;
        Reveal(state, CharacterCatalog.Companion(state, guide));
        state.Pending = null;
        TurnFlow.BeginTurn(state);
    }

    public static void Reveal(MatchState state, CharacterState character)
    {
        if (state.Lords is null || character.Side != Side.FreePeoples ||
            !CharacterCatalog.All.TryGetValue(character.DefinitionId, out var definition) ||
            !definition.Initial && !state.Lords.RevealedCompanionVersions.ContainsKey(character.Id))
            return;
        if (state.Lords.RevealedCompanionVersions.GetValueOrDefault(character.Id) == character.DefinitionId)
            return;
        state.Lords.RevealedCompanionVersions[character.Id] = character.DefinitionId;
        var activity = HistoryFacts.At(state, ActionContextKind.Characters, character.DefinitionId);
        activity.Facts.Figures.Add(new() { After = MovementFacts.Character(character), To = character.Location });
        state.Events.Add(new()
        {
            Revision = state.Revision,
            Kind = "CompanionRevealed",
            Activity = activity,
            Text = $"{character.Id} is revealed as the {VersionName(character.DefinitionId)} version."
        });
    }

    // Both versions of a Companion share the title, so the log names the revealed card by its edition.
    private static string VersionName(string definitionId) => definitionId.Split('.')[0] switch
    {
        "lords" => "Lords of Middle-earth",
        "fate" => "Fate of Erebor",
        _ => "base"
    };
}
