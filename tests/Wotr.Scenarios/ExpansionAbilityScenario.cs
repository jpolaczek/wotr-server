using Wotr.Contracts;
using Wotr.Domain;
using static ExpansionUiFixture;

internal static class ExpansionAbilityScenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["t34-brand"] = Recruit(random, SovereignCatalog.Brand),
        ["t34-dain"] = Recruit(random, SovereignCatalog.Dain),
        ["t34-balrog-activate"] = Balrog(random, true),
        ["t34-balrog-deactivate"] = Balrog(random, false),
        ["t34-gothmog-recruit"] = Gothmog(random),
        ["t34-gothmog-upgrade"] = Gothmog(random),
        ["t34-travel-fp"] = Travel(random, Side.FreePeoples),
        ["t34-travel-sh"] = Travel(random, Side.Shadow),
        ["ui22-mouth-army"] = SpecialCost(random, "mouth"),
        ["ui22-sorcery-army"] = SpecialCost(random, "sorcery"),
        ["ui22-balrog-army"] = SpecialCost(random, "balrog"),
        ["ui22-balrog-travel"] = SpecialCost(random, "balrog")
    };

    private static MatchState Recruit(IRandomSource random, string id)
    {
        var state = Actions(random, Full(false));
        var definition = SovereignCatalog.All[id];
        state.Nations.Single(n => n.Id == definition.Nation).StepsToWar = 0;
        SovereignLifecycle.Awaken(state, Sovereign(state, id), definition.Home);
        var ruler = KingsDice.Ruler(state, Side.FreePeoples)!;
        ruler.Location = DieLocation.Available;
        ruler.Face = DieFace.Muster;
        return state;
    }

    private static MatchState SpecialCost(IRandomSource random, string ability)
    {
        var state = Actions(random, ability == "mouth" ? new() : Full());
        foreach (var unit in state.Units)
            BoardRules.ReturnToReserve(unit);
        var balrog = ability == "balrog";
        var region = balrog ? "moria" : "osgiliath";
        var definition = balrog ? LordsCharacters.Balrog : ability == "mouth" ? CharacterRules.Mouth : LordsCharacters.Mouth;
        var character = Put(state, definition, region);
        if (balrog)
            character.Level = 2;
        var soldier = state.Units.First(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular);
        soldier.Location = region;
        state.Units.First(u => u.Kind == UnitKind.Nazgul).Location = region;
        var nation = state.Nations.Single(n => n.Id == "sauron");
        nation.Active = true;
        nation.StepsToWar = 0;
        var die = balrog ? state.Dice.Single(d => d.Source == LordsDiceCatalog.Balrog) : Die(state, Side.Shadow);
        die.Location = DieLocation.Available;
        die.Face = balrog ? DieFace.UseBalrog : DieFace.Muster;
        if (die.Special != null)
            die.Special.Chosen = true;
        TurnFlow.SelectNext(state, Side.Shadow);
        return state;
    }

    private static MatchState Balrog(IRandomSource random, bool activate)
    {
        var state = Actions(random);
        Put(state, LordsCharacters.Balrog, "moria").Level = activate ? 0 : 2;
        if (activate)
        {
            var die = state.Dice.Single(d => d.Source == LordsDiceCatalog.Balrog);
            die.Location = DieLocation.Available;
            die.Face = DieFace.UseBalrog;
            die.Special!.Chosen = true;
            TurnFlow.SelectNext(state, Side.Shadow);
        }
        else
            Die(state, Side.FreePeoples).Face = DieFace.WillOfTheWest;
        return state;
    }

    private static MatchState Gothmog(IRandomSource random)
    {
        var state = Actions(random);
        Put(state, LordsCharacters.Gothmog, "minas-morgul");
        Die(state, Side.Shadow).Face = DieFace.Muster;
        TurnFlow.SelectNext(state, Side.Shadow);
        return state;
    }

    private static MatchState Travel(IRandomSource random, Side side)
    {
        var state = Actions(random, Full(false));
        if (side == Side.FreePeoples)
        {
            state = Execute(state, CommandKind.SeparateCompanions, new()
            {
                DieId = Die(state, side).Id,
                Ids = [.. state.Fellowship.Companions],
                Route = [state.Fellowship.RegionId]
            });
            foreach (var sovereign in state.Kings!.Sovereigns)
                SovereignLifecycle.Awaken(state, sovereign, SovereignCatalog.All[sovereign.Id].Home);
            var ruler = KingsDice.Ruler(state, side)!;
            ruler.Face = DieFace.Character;
            ruler.Location = DieLocation.Available;
        }
        else
        {
            foreach (var (id, region) in new[]
            {
                (LordsCharacters.Balrog, "moria"), (LordsCharacters.Gothmog, "minas-morgul"),
                (CharacterRules.Mouth, "barad-dur"), (CharacterRules.WitchKing, "barad-dur"),
                (KingsChieftains.Serpent, "near-harad"), (KingsChieftains.Mirkwood, "dol-guldur"),
                (KingsChieftains.Ugluk, "orthanc")
            })
                Put(state, id, region);
            state.Characters.Single(c => c.Id == LordsCharacters.Balrog).Level = 2;
            foreach (var nazgul in state.Units.Where(u => u.Kind == UnitKind.Nazgul))
                nazgul.Location = "barad-dur";
        }
        TurnFlow.SelectNext(state, side);
        return state;
    }
}
