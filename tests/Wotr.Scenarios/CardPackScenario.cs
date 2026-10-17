using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

internal static class CardPackScenario
{
    private static readonly GameEngine Engine = new(new CorruptionCommands(), new HuntCommands(), new FellowshipCommands(), new CharacterCommands(), new MovementCommands(), new EffectCommands(), new BattleCommands(), new StrongholdCommands(), new DeclineReactionHandler());

    public static Dictionary<string, MatchState> Create(IRandomSource random)
    {
        var scenarios = CardPack02Scenario.Create(random);
        foreach (var pair in CardPack03Scenario.Create(random))
            scenarios.Add(pair.Key, pair.Value);
        foreach (var pair in CardPack04Scenario.Create(random))
            scenarios.Add(pair.Key, pair.Value);
        foreach (var pair in Strategy01Scenario.Create(random))
            scenarios.Add(pair.Key, pair.Value);
        foreach (var pair in Strategy02Scenario.Create(random))
            scenarios.Add(pair.Key, pair.Value);
        foreach (var pair in Strategy03Scenario.Create(random))
            scenarios.Add(pair.Key, pair.Value);
        foreach (var pair in Strategy04Scenario.Create(random))
            scenarios.Add(pair.Key, pair.Value);
        foreach (var pair in Shadow01Scenario.Create(random))
            scenarios.Add(pair.Key, pair.Value);
        foreach (var pair in Shadow02Scenario.Create(random))
            scenarios.Add(pair.Key, pair.Value);
        foreach (var pair in Shadow03Scenario.Create(random))
            scenarios.Add(pair.Key, pair.Value);
        foreach (var pair in Shadow04Scenario.Create(random))
            scenarios.Add(pair.Key, pair.Value);
        foreach (var pair in ShadowStrategy01Scenario.Create(random))
            scenarios.Add(pair.Key, pair.Value);
        foreach (var pair in ShadowStrategy03Scenario.Create(random))
            scenarios.Add(pair.Key, pair.Value);
        foreach (var pair in ShadowStrategy04Scenario.Create(random))
            scenarios.Add(pair.Key, pair.Value);
        foreach (var pair in ShadowStrategy02Scenario.Create(random))
            scenarios.Add(pair.Key, pair.Value);
        foreach (var pair in LordsShadowScenario.Create(random))
            scenarios.Add(pair.Key, pair.Value);
        foreach (var pair in LordsFinalCardsScenario.Create(random))
            scenarios.Add(pair.Key, pair.Value);
        foreach (var pair in KingsCards01Scenario.Create(random))
            scenarios.Add(pair.Key, pair.Value);
        foreach (var pair in KingsCards02Scenario.Create(random))
            scenarios.Add(pair.Key, pair.Value);
        foreach (var pair in KingsCards03Scenario.Create(random))
            scenarios.Add(pair.Key, pair.Value);
        foreach (var pair in KingsCards04Scenario.Create(random))
            scenarios.Add(pair.Key, pair.Value);
        foreach (var pair in FateCardsScenario.Create(random))
            scenarios.Add(pair.Key, pair.Value);
        foreach (var pair in FateGimliScenario.Create(random))
            scenarios.Add(pair.Key, pair.Value);
        foreach (var pair in ExpansionUiScenario.Create(random))
            scenarios.Add(pair.Key, pair.Value);
        foreach (var pair in ExpansionAbilityScenario.Create(random))
            scenarios.Add(pair.Key, pair.Value);
        foreach (var pair in ExpansionWindowScenario.Create(random))
            scenarios.Add(pair.Key, pair.Value);
        scenarios.Add("hunt-protection", Protection(random));
        scenarios.Add("ui54-hunt-protection", Protection(random));
        scenarios.Add("mighty-attack", Combat(random, false));
        scenarios.Add("blade", Combat(random, true));
        return scenarios;
    }

    private static MatchState Protection(IRandomSource random)
    {
        var state = FellowshipScenario.Actions(random);
        state.Fellowship.MordorStep = 0;
        state.Fellowship.RegionId = MordorRules.Track;
        state.Fellowship.Progress = 0;
        foreach (var card in new[] { FreePeoplesCharacterCards01.Mithril, FreePeoplesCharacterCards01.Axe })
        {
            Give(state, card);
            HandRules.Remove(state.Player(Side.FreePeoples), card);
            state.Player(Side.FreePeoples).Table.Add(card);
        }
        foreach (var tile in state.HuntTiles)
            tile.Location = tile.Id is "base.hunt.standard.3.0.1" or "base.hunt.special-sh.die.0.1" ? "pool" : "removed";
        return Execute(state, CommandKind.MoveFellowship, new() { DieId = state.Dice[0].Id }, 1);
    }

    private static MatchState Combat(IRandomSource random, bool blade)
    {
        var state = FellowshipScenario.Actions(random);
        foreach (var unit in state.Units)
            BoardRules.ReturnToReserve(unit);
        foreach (var nation in state.Nations)
        {
            nation.Active = true;
            nation.StepsToWar = 0;
        }
        state.Dice[0].Face = DieFace.Army;
        for (var i = 0; i < 2; i++)
        {
            Unit(state, "gondor", UnitKind.Regular, "minas-tirith");
            Unit(state, "sauron", UnitKind.Regular, "lossarnach");
        }
        if (blade)
        {
            Unit(state, "gondor", UnitKind.Leader, "minas-tirith");
            Character(state, "base.character.merry", "minas-tirith");
            Character(state, CharacterRules.WitchKing, "lossarnach");
            Character(state, CharacterRules.Mouth, "lossarnach");
        }
        else
        {
            Character(state, CharacterRules.Aragorn, "minas-tirith");
            Character(state, "base.character.gimli", "minas-tirith");
        }
        var card = blade ? FreePeoplesCharacterCards01.Mithril : FreePeoplesCharacterCards01.Axe;
        Give(state, card);
        state = Execute(state, CommandKind.AttackArmy, new()
        {
            DieId = state.Dice[0].Id,
            MovementCost = DieFace.Army,
            Moves = [new() { From = "minas-tirith", To = "lossarnach",
                UnitIds = state.Units.Where(u => u.Location == "minas-tirith").Select(u => u.Id).ToList(),
                CharacterIds = state.Characters.Where(c => c.Location == "minas-tirith").Select(c => c.Id).ToList() }]
        });
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = card });
        state = Execute(state, CommandKind.CommitCombatCard, new(), blade ? [0, 0, 0, 0] : [0, 1, 2, 3, 0, 0]);
        if (!blade)
            return state;
        state = Execute(state, CommandKind.RerollBattle, new() { Ids = BattleRoll.Failed(BattleRuntime.Active(state)!.Attacker) });
        return Execute(state, CommandKind.RerollBattle, new(), 5, 0);
    }

    internal static void Unit(MatchState state, string nation, UnitKind kind, string region) =>
        state.Units.First(u => u.Nation == nation && u.Kind == kind && u.Location == "reserve").Location = region;

    internal static void Character(MatchState state, string id, string region)
    {
        var figureId = id == CharacterRules.Aragorn ? CharacterRules.Strider : id == CharacterRules.White ? CharacterRules.Grey : id;
        var character = CharacterCatalog.Companion(state, figureId);
        CharacterLifecycle.ApplyVersion(character, CharacterCatalog.Base[id]);
        character.Location = region;
        state.Fellowship.Companions.Remove(figureId);
        state.Characters.Add(character);
        if (character.Side == Side.Shadow)
            state.MinionEverEntered = true;
        CharacterLifecycle.ReconcileDice(state);
    }

    internal static void Give(MatchState state, string card)
    {
        var player = state.Player(CardCatalog.All[card].Side);
        foreach (var deck in player.Decks.Values)
            deck.Remove(card);
        if (!player.Hand.Contains(card))
            HandRules.Add(player, card);
    }

    internal static MatchState EmptyBoard(IRandomSource random)
    {
        var state = FellowshipScenario.Actions(random);
        foreach (var unit in state.Units)
            BoardRules.ReturnToReserve(unit);
        foreach (var nation in state.Nations)
        {
            nation.Active = true;
            nation.StepsToWar = 0;
        }
        return state;
    }

    internal static MatchState PlayEvent(MatchState state, string card, params int[] rolls)
    {
        Give(state, card);
        var die = state.Dice.First(d => d.Owner == CardCatalog.Base[card].Side && d.Location == DieLocation.Available);
        die.Face = CardCatalog.Base[card].EventType;
        return Execute(state, CommandKind.PlayEvent, new() { DieId = die.Id, CardId = card, CardCost = die.Face }, rolls);
    }

    internal static MatchState Execute(MatchState state, CommandKind kind, CommandPayload payload, params int[] rolls)
    {
        var result = Engine.Execute(state, state.Pending!.Owner, new()
        {
            MatchId = state.Id,
            ExpectedRevision = state.Revision,
            DecisionId = state.Pending.Id,
            CommandId = Guid.NewGuid().ToString("N"),
            Kind = kind,
            Payload = payload
        }, new RecordedRolls(rolls));
        if (!result.Receipt.Accepted)
            throw new InvalidOperationException("Invalid card scenario: " + result.Receipt.Error);
        return result.State;
    }

    private sealed class RecordedRolls(int[] values) : IRandomSource
    {
        private readonly Queue<int> rolls = new(values);
        public int Next(int upperBound) => rolls.Dequeue();
    }

    internal static Dictionary<string, PlayerView> LayoutViews(IReadOnlyDictionary<string, MatchState> states, IEnumerable<string> names)
    {
        var included = names.ToHashSet();
        var projection = new Projection(new CoreOptions(), new TurnOptions(), new MusterOptions(), new MovementOptions(),
            new EffectOptions(), new FellowshipOptions(), new CharacterOptions(), new HuntOptions(), new BattleOptions(), new StrongholdCommands());
        return states.Where(p => included.Contains(p.Key)).ToDictionary(p => p.Key, p =>
        {
            var snapshot = StateCodec.Copy(p.Value);
            var owner = snapshot.Pending!.Owner;
            var player = snapshot.Player(owner);
            foreach (var card in CardCatalog.Base.Values.Where(c => c.Side == owner))
            {
                if (player.Hand.Count >= 6)
                    break;
                if (!player.Hand.Contains(card.Id) && !player.Table.Contains(card.Id) && !player.Discard.Contains(card.Id))
                    Give(snapshot, card.Id);
            }
            return projection.For(snapshot, owner);
        });
    }
}
