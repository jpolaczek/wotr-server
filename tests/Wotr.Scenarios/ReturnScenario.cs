using Wotr.Contracts;
using Wotr.Domain;

internal static class ReturnScenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random)
    {
        var result = new Dictionary<string, MatchState>();
        foreach (var name in new[] { "ring-paid", "secret-combat", "no-regular", "sh-str02-corsairs-relief",
            "boundary-random-companion", "reveal", "another-way", "ring-destroyed" })
        {
            var text = RegressionFixtures.Read(name);
            result.Add(name, StateCodec.Read(text));
        }
        var expansions = ExpansionUiScenario.Create(random);
        result.Add("council", expansions["ui09-council"]);
        result.Add("ui69-recovery", StateCodec.Copy(expansions["ui09-council"]));
        result.Add("corruption", ExpansionUiFixture.Execute(expansions["ui09-corruption-hidden"], CommandKind.AttemptCorruption,
            new() { CharacterId = "kings.sovereign.thranduil", DieId = LordsDiceCatalog.Nenya }, 0));
        if (result["corruption"].Pending?.Kind != DecisionKind.CorruptionTile)
            throw new InvalidOperationException("Return fixture must pause inside the private corruption choice.");
        var actions = InteractionScenario.Create(random);
        result.Add("restart", actions["receipt"]);
        var draft = actions["race"];
        foreach (var die in draft.Dice.Where(d => d.Owner == Side.FreePeoples))
            die.Face = DieFace.Army;
        result.Add("draft", draft);
        result.Add("policy", BattleScenario.Create(random)["policy-fp"]);
        var maps = MapOverrideScenario.Create(random);
        foreach (var name in new[] { "map16-base", "map16-newcities", "map16-whitehand" })
            result.Add(name, maps[name]);
        result.Add("map16-paid", MapOverrideScenario.HistoricalPaidStep());
        result.Add("ui62-ring", InGameContextScenario.RecoveryRing(random));
        result.Add("ui62-ring-draft", InGameContextScenario.RecoveryRing(random));
        result.Add("ui62-tile", InGameContextScenario.RecoveryTile(random));
        result.Add("ui62-victory", InGameContextScenario.RecoveryVictory(random));
        foreach (var name in new[] { "draft", "restart", "reveal", "no-regular", "council", "ring-destroyed" })
            result.Add("ui70-" + name, StateCodec.Copy(result[name]));
        result.Add("ui70-card", StateCodec.Copy(actions["ui62-effect-cost"]));
        result.Add("ui70-hunt", StateCodec.Copy(actions["ui34-response-Ring-click"]));
        result.Add("ui70-live", StateCodec.Copy(result["draft"]));
        result.Add("ui71-council-entry", BoardCompletionScenario.CouncilEntry(random));
        for (var index = 0; index < 10; index++)
            result.Add("waiting-" + index, StateCodec.Copy(result["draft"]));
        foreach (var (key, state) in result.ToArray())
        {
            var id = "return-" + key + "-" + Guid.NewGuid().ToString("N");
            result[key] = StateCodec.Read(StateCodec.Write(state).Replace(state.Id, id, StringComparison.Ordinal));
        }
        return result;
    }
}
