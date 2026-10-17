using Wotr.Contracts;
using Wotr.Domain.Content;
using static Wotr.Domain.AvailabilityDescription;

namespace Wotr.Domain;

internal sealed class CharacterRosterEntries
{
    private readonly MatchState state;
    private readonly Side viewer;
    private readonly ILookup<string, CharacterEntryPlan> normal;
    private readonly ILookup<string, CharacterEntryPlan> council;
    private readonly IReadOnlyList<string> councilTokens;
    private readonly IReadOnlyList<DieState> dice;

    public CharacterRosterEntries(MatchState state, Side viewer)
    {
        this.state = state;
        this.viewer = viewer;
        normal = CharacterEntry.Candidates(state, viewer).ToLookup(p => p.DefinitionId);
        council = KingsCouncilEntry.Candidates(state, viewer).ToLookup(p => p.DefinitionId);
        councilTokens = viewer == Side.Shadow ? KingsCouncilEntry.Tokens(state) : [];
        dice = state.Dice.Where(d => d.Owner == viewer && d.Location == DieLocation.Available).ToArray();
    }

    public CharacterEntryVersionView Version(CharacterEntryDescription description, CharacterRosterLifecycle lifecycle)
    {
        var definition = CharacterCatalog.All[description.DefinitionId];
        var view = new CharacterEntryVersionView
        {
            DefinitionId = definition.Id,
            Title = definition.Title,
            Mode = Mode(description, lifecycle),
            ExtraDiceNextRecovery = definition.ExtraActionDice,
            Entry = Availability(description, false)
        };
        if (state.Configuration.Council && state.Configuration.Kings && KingsChieftains.IsChieftain(definition.Id))
        {
            view.CouncilEntry = Availability(description, true);
            view.CouncilTokens = [.. councilTokens];
        }
        return view;
    }

    private ActionAvailabilityView Availability(CharacterEntryDescription description, bool withCouncil)
    {
        var candidates = (withCouncil ? council : normal)[description.DefinitionId].ToArray();
        var eligible = dice.Where(d => CharacterEntry.CanPay(d, description.DefinitionId)).Select(d => d.Id).ToList();
        var requirements = withCouncil
            ? "This chieftain has never entered and has a legal entry region. A Council token replaces only the national or Fellowship prerequisite."
            : description.Requirements;
        var conditions = new List<AvailabilityConditionView>
        {
            Condition("Cost", "entry-die", "An eligible entry die is available.", eligible.Count > 0)
        };
        if (withCouncil)
            conditions.Add(Condition("Cost", "entry-council", "An unspent Council token is available and none was used this turn.", councilTokens.Count > 0));
        // The Council planner short-circuits without a token; do not label an unexamined region as illegal.
        if (!withCouncil || councilTokens.Count > 0)
            conditions.Add(Condition("Condition", "entry-requirements", requirements, candidates.Length > 0));
        var view = Create(state, viewer, description.DefinitionId, withCouncil ? "enter-with-council" : "enter-character",
            "Enter " + CharacterCatalog.All[description.DefinitionId].Title,
            withCouncil ? description.Source + "; KINGS p.18" : description.Source,
            Cost(description.DefinitionId, withCouncil),
            "During your normal action. Additional dice enter at the next Recover, never immediately.",
            ActionWindow(state, viewer) && state.Pending!.Commands.Contains(CommandKind.EnterCharacter), conditions.ToArray());
        view.EligibleDice = eligible;
        view.Targets = candidates.Select(c => c.Region).Distinct().Order(StringComparer.Ordinal).ToList();
        return view;
    }

    private static string Cost(string version, bool council)
    {
        var die = CharacterEntry.RequiredCost(version) == DieFace.WillOfTheWest ? "One Will of the West result."
            : KingsChieftains.IsChieftain(version) ? "One Muster-capable result or any non-Eye Ruler result."
            : "One Muster-capable result.";
        return council ? die + " Spend one Council token with the die." : die;
    }

    private static CharacterEntryMode Mode(CharacterEntryDescription description, CharacterRosterLifecycle lifecycle) =>
        lifecycle == CharacterRosterLifecycle.EliminatedWithReplacement ? CharacterEntryMode.ReturnAsNewVersion
        : description.DefinitionId is CharacterRules.White or CharacterRules.Aragorn || lifecycle == CharacterRosterLifecycle.OnBoard
            ? CharacterEntryMode.Transformation : CharacterEntryMode.FirstEntry;
}
