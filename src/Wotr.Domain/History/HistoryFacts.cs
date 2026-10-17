using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

internal static class HistoryFacts
{
    public static T Copy<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, BoardCatalog.Json), BoardCatalog.Json)!;

    public static HuntOutcomeView Fellowship(MatchState state) => new()
    {
        RegionId = state.Fellowship.RegionId, Progress = state.Fellowship.Progress,
        MordorStep = state.Fellowship.MordorStep, Corruption = state.Fellowship.Corruption,
        Hidden = state.Fellowship.Hidden, GuideId = state.Fellowship.Guide
    };

    public static ActionEvent At(MatchState state, ActionContextKind kind, string source = "", string region = "", string nation = "") => new()
    {
        Turn = state.Turn,
        Automatic = true,
        Context = new()
        {
            Kind = kind, DefinitionId = source, NationId = nation,
            Regions = BoardRules.Regions.ContainsKey(region) ? [region] : [],
            BattleId = BattleRuntime.Active(state)?.Id ?? "",
            BattleRound = BattleRuntime.Active(state)?.Round,
            HuntChainId = state.HuntReport is { Completed: false } hunt ? hunt.Id : ""
        },
        Facts = new() { SourceId = source }
    };

    public static ActionEvent Political(MatchState state, NationState nation)
    {
        var result = At(state, ActionContextKind.Politics, nation: nation.Id);
        result.Facts.Nations.Add(new() { Id = nation.Id, Active = nation.Active, StepsToWar = nation.StepsToWar });
        return result;
    }

    public static ActionEvent Card(MatchState state, Side owner, ActionCardUse use, string id = "", string deck = "", int count = 1)
    {
        var result = At(state, ActionContextKind.Cards, id);
        result.Facts.Cards.Add(new()
        {
            Owner = owner, Use = use, Id = id, Count = count,
            Deck = deck.Length > 0 ? deck : CardCatalog.All.GetValueOrDefault(id)?.Deck ?? ""
        });
        return result;
    }

    public static ActionEvent Battle(MatchState state, BattleState battle)
    {
        var result = At(state, ActionContextKind.Battle, region: battle.Region);
        result.Context.BattleId = battle.Id;
        result.Context.BattleRound = battle.Round;
        result.Facts.Battle = BattleProjection.Public(state, battle);
        return result;
    }

    public static ActionEvent BattleBoundary(MatchState state, BattleState battle)
    {
        var result = Battle(state, battle);
        var region = BoardRules.Region(state, battle.Region);
        result.Facts.BattlePlace = new()
        {
            RegionId = region.Id, NationId = region.Nation ?? "", Settlement = region.Settlement ?? "",
            Controller = state.Control.GetValueOrDefault(region.Id),
            Units = state.Units.Where(u => u.Location == battle.Region || u.Location == battle.Attacker.Region || u.Location == battle.Defender.Region)
                .Select(Projection.Unit).ToList(),
            Characters = state.Characters.Where(c => c.Location == battle.Region || c.Location == battle.Attacker.Region || c.Location == battle.Defender.Region)
                .Select(Projection.Character).ToList()
        };
        result.Facts.Outcome = Outcome(state);
        return result;
    }

    public static ActionOutcome Outcome(MatchState state) => new()
    {
        FreePeoplesPoints = MilitaryVictory.Points(state, Side.FreePeoples),
        ShadowPoints = MilitaryVictory.Points(state, Side.Shadow),
        Winner = state.Winner, Victory = state.Victory
    };

    public static ActionEvent Sovereign(MatchState state, string id)
    {
        var result = At(state, ActionContextKind.Characters, id);
        result.Facts.Sovereigns = KingsProjection.PublicSovereigns(state).Where(s => s.Id == id).ToList();
        return result;
    }

    public static ActionEvent Unit(MatchState state, UnitState unit)
    {
        var result = At(state, ActionContextKind.Army, region: unit.Location);
        result.Facts.Figures.Add(new() { After = MovementFacts.Unit(unit), To = unit.Location });
        return result;
    }

    public static DieFace? Face(DieState? die) => die is null || die.Location is DieLocation.Unrolled or DieLocation.Removed ? null : die.Face;
}
