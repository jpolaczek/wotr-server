using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

internal static class BaseAbilityScenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["ability-gimli"] = Gimli(random),
        ["ability-voice-recruit-full"] = Saruman(random, "recruit", scarce: false),
        ["ability-voice-recruit-scarce"] = Saruman(random, "recruit", scarce: true),
        ["ability-voice-upgrade-full"] = Saruman(random, "upgrade", scarce: false),
        ["ability-voice-upgrade-scarce"] = Saruman(random, "upgrade", scarce: true)
    };

    private static MatchState Gimli(IRandomSource random)
    {
        var state = FellowshipScenario.Actions(random);
        PutCharacter(state, "base.character.gimli", "erebor");
        var dwarves = state.Nations.Single(n => n.Id == "dwarves");
        dwarves.Active = true;
        dwarves.StepsToWar = 2;
        var dice = state.Dice.Where(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available).ToArray();
        dice[0].Face = DieFace.Army;
        dice[1].Face = DieFace.Event;
        return state;
    }

    private static MatchState Saruman(IRandomSource random, string ability, bool scarce)
    {
        var state = FellowshipScenario.Actions(random);
        var isengard = state.Nations.Single(n => n.Id == "isengard");
        isengard.Active = true;
        isengard.StepsToWar = 0;
        foreach (var unit in state.Units.Where(u => u.Nation == "isengard"))
            BoardRules.ReturnToReserve(unit);
        foreach (var unit in state.Units.Where(u => u.Nation == "isengard" && u.Kind == UnitKind.Regular).Take(2))
            unit.Location = "orthanc";
        PutCharacter(state, CharacterRules.Saruman, "orthanc");
        if (scarce)
            LimitReserve(state, ability == "recruit" ? UnitKind.Regular : UnitKind.Elite);
        var dice = state.Dice.Where(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available).ToArray();
        dice[0].Face = DieFace.Muster;
        dice[1].Face = DieFace.ArmyMuster;
        TurnFlow.SelectNext(state, Side.Shadow);
        return state;
    }

    private static void LimitReserve(MatchState state, UnitKind kind)
    {
        // Remaining figures are on the real board, split to avoid an unrelated excess decision.
        var regions = new[] { "moria", "dol-guldur", "mount-gundabad" };
        var units = state.Units.Where(u => u.Nation == "isengard" && u.Kind == kind && u.Location == "reserve").Skip(1).ToArray();
        for (var index = 0; index < units.Length; index++)
            units[index].Location = regions[index % regions.Length];
    }

    private static void PutCharacter(MatchState state, string definition, string region)
    {
        var character = CharacterCatalog.Companion(state, definition);
        state.Fellowship.Companions.Remove(character.Id);
        character.Location = region;
        state.Characters.Add(character);
        CharacterLifecycle.ReconcileDice(state);
    }
}
