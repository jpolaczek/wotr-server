using System.Text.Json.Nodes;
using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

internal static class UI10ShapeCases
{
    public static readonly string[] Fields = ["ids", "unitIds", "characterIds", "companionIds",
        "placementUnit", "placementRegion", "lossUnit", "dieId", "regionId"];

    public static (MatchState State, JsonObject Command) Create(string field)
    {
        var state = BattleFixture.State();
        GameCommand command;
        switch (field)
        {
            case "ids":
                command = Discard(state);
                break;
            case "unitIds":
            case "characterIds":
                command = Move(state);
                break;
            case "companionIds":
                command = Companion(state);
                break;
            case "placementUnit":
            case "placementRegion":
                command = Recruit(state);
                break;
            case "lossUnit":
                (state, command) = BattleLoss();
                break;
            case "dieId":
                command = BattleFixture.Command(state, CommandKind.DrawCard, CharacterFixture.Cost(state, DieFace.Event));
                command.Payload.Deck = "character";
                break;
            case "regionId":
                state = BattleFixture.State(Side.Shadow);
                command = BattleFixture.Command(state, CommandKind.EnterCharacter, CharacterFixture.Cost(state, DieFace.Muster));
                command.Payload.CharacterId = CharacterRules.Saruman;
                command.Payload.RegionId = "orthanc";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(field));
        }
        Assert.Null(CharacterFixture.Engine().Validate(state, state.Pending!.Owner, command));
        var raw = UI10ProtocolFixture.Json(command);
        SetNull(raw["payload"]!, field);
        return (state, raw);
    }

    private static GameCommand Discard(MatchState state)
    {
        for (var number = 1; number <= 7; number++)
            EventTests.GiveCard(state, Side.FreePeoples, $"base.fp.character.{number:00}");
        Assert.True(CardFlow.RequireDiscard(state, Side.FreePeoples, DiscardContinuation.FinishAction));
        return BattleFixture.Command(state, CommandKind.DiscardCards,
            new() { Ids = state.Player(Side.FreePeoples).Hand.Take(state.Pending!.RequiredCount).ToList() });
    }

    private static GameCommand Move(MatchState state)
    {
        var unit = BattleFixture.Put(state, "gondor", UnitKind.Regular, "minas-tirith");
        var companion = CharacterFixture.Put(state, CharacterRules.Strider, "minas-tirith");
        return BattleFixture.Command(state, CommandKind.MoveArmies,
            BattleFixture.AttackPayload(state, [unit.Id, companion.Id], "minas-tirith", "osgiliath"));
    }

    private static GameCommand Companion(MatchState state)
    {
        var companion = CharacterFixture.Put(state, CharacterRules.Grey, "rivendell");
        var payload = CharacterFixture.Cost(state, DieFace.Character);
        payload.CompanionMoves.Add(new() { Ids = [companion.Id], Route = ["rivendell", "fords-of-bruinen"] });
        return BattleFixture.Command(state, CommandKind.MoveCompanions, payload);
    }

    private static GameCommand Recruit(MatchState state)
    {
        var unit = state.Units.First(u => u.Nation == "gondor" && u.Kind == UnitKind.Elite && u.Location == "reserve");
        var payload = CharacterFixture.Cost(state, DieFace.Muster);
        payload.Placements.Add(new() { UnitId = unit.Id, RegionId = "minas-tirith" });
        return BattleFixture.Command(state, CommandKind.Recruit, payload);
    }

    private static (MatchState, GameCommand) BattleLoss()
    {
        var state = BattleFixture.State();
        var attacker = BattleFixture.Put(state, "gondor", UnitKind.Regular, BattleFixture.Origin);
        BattleFixture.Put(state, "sauron", UnitKind.Regular, BattleFixture.Target);
        state = BattleFixture.NoCards(BattleFixture.Begin(state, [attacker.Id]), 6, 6);
        return (state, BattleFixture.Command(state, CommandKind.TakeBattleLosses,
            new() { Losses = [BattleFixture.Loss(attacker.Id)] }));
    }

    private static void SetNull(JsonNode payload, string field)
    {
        switch (field)
        {
            case "ids": payload["ids"]![0] = null; break;
            case "unitIds": payload["moves"]![0]!["unitIds"]![0] = null; break;
            case "characterIds": payload["moves"]![0]!["characterIds"]![0] = null; break;
            case "companionIds": payload["companionMoves"]![0]!["ids"]![0] = null; break;
            case "placementUnit": payload["placements"]![0]!["unitId"] = null; break;
            case "placementRegion": payload["placements"]![0]!["regionId"] = null; break;
            case "lossUnit": payload["losses"]![0]!["unitId"] = null; break;
            default: payload[field] = null; break;
        }
    }
}
