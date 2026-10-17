using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using static ExpansionUiFixture;

internal static class ExpansionWindowScenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["t34-evil-knowledge"] = Siege(random),
        ["t34-nenya"] = Nenya(random),
        ["t34-lore-dismay"] = Lore(random),
        ["t34-lore-eye"] = Lore(random),
        ["ui58-dismay-before-lore"] = Lore(random),
        ["t34-vilya-decline"] = ExpansionUiScenario.Vilya(random)
    };

    private static MatchState Siege(IRandomSource random)
    {
        var state = ExpansionUiMatrix.ResourceState(random);
        TurnFlow.RollAndBeginActions(state, new Zero());
        while (state.Pending!.Kind is DecisionKind.SpecialDieChoice or DecisionKind.RulerDiscard)
        {
            var special = state.Pending.Kind == DecisionKind.SpecialDieChoice;
            var choices = special ? LordsDiceRoll.LegalChoices(state, state.Pending.Owner) : KingsDice.DiscardCandidates(state, state.Pending.Owner);
            state = Execute(state, special ? CommandKind.ChooseSpecialDie : CommandKind.DiscardRulerDie, new() { DieId = choices[0] });
        }
        foreach (var unit in state.Units)
            BoardRules.ReturnToReserve(unit);
        foreach (var nation in state.Nations)
        {
            nation.Active = true;
            nation.StepsToWar = 0;
        }
        // ResourceState already has the Mouth figure. Configure its alternative
        // version instead of adding a second physical copy of the same identity.
        var mouth = state.Characters.Single(c => c.Id == CharacterRules.Mouth);
        CharacterLifecycle.ApplyVersion(mouth, CharacterCatalog.All[LordsCharacters.Mouth]);
        mouth.Location = "minas-tirith";
        CardPackScenario.Unit(state, "sauron", UnitKind.Elite, "minas-tirith");
        CardPackScenario.Unit(state, "gondor", UnitKind.Regular, "minas-tirith");
        var elite = state.Units.Single(u => u.Kind == UnitKind.Elite && u.Location == "minas-tirith");
        state.Units.Single(u => u.Kind == UnitKind.Regular && u.Location == "minas-tirith").Garrison = true;
        TurnFlow.SelectNext(state, Side.Shadow);
        var die = Die(state, Side.Shadow);
        die.Face = DieFace.Army;
        state = Execute(state, CommandKind.AttackArmy, new()
        {
            DieId = die.Id, MovementCost = DieFace.Army,
            Moves = [new() { From = "minas-tirith", To = "minas-tirith", UnitIds = [elite.Id], CharacterIds = [mouth.Id] }]
        });
        state = Execute(state, CommandKind.CommitCombatCard);
        state = Execute(state, CommandKind.CommitCombatCard, samples: new int[10]);
        while (state.Pending!.Kind == DecisionKind.BattleReroll)
            state = Execute(state, CommandKind.RerollBattle);
        return state;
    }

    private static MatchState Nenya(IRandomSource random)
    {
        var state = Actions(random);
        Put(state, KeeperRings.Galadriel, "lorien");
        var eye = Die(state, Side.Shadow);
        eye.Face = DieFace.Eye;
        eye.Location = DieLocation.Hunt;
        eye.HuntOrigin = "allocated";
        foreach (var tile in state.HuntTiles)
            tile.Location = tile.Id == "base.hunt.standard.eye.1.1" ? "pool" : "set-aside";
        state = Execute(state, CommandKind.MoveFellowship, new() { DieId = Die(state, Side.FreePeoples).Id }, 5, 0);
        state.HuntTiles.Single(t => t.Id == "base.hunt.standard.3.0.1").Location = "pool";
        return state;
    }

    private static MatchState Lore(IRandomSource random)
    {
        var state = ExpansionUiScenario.Dismay(random);
        CardPackScenario.Give(state, LordsShadowCards.Lore);
        HandRules.Remove(state.Player(Side.Shadow), LordsShadowCards.Lore);
        state.Player(Side.Shadow).Table.Add(LordsShadowCards.Lore);
        if (!KingsDismay.Begin(state))
            throw new InvalidOperationException("Lore ordering requires the before-actions Dismay window.");
        return state;
    }

    private sealed class Zero : IRandomSource
    {
        public int Next(int upperBound) => 0;
    }
}
