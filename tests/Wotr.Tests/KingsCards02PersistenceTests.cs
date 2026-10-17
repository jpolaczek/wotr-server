using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using static Wotr.Domain.KingsCards02;
using static Wotr.Tests.KingsCardsFixture;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsCards02PersistenceTests
{
    [PostgresFact]
    public async Task EveryEventRestartsPaymentAndItsFirstDecision()
    {
        foreach (var card in Ids)
        {
            var state = card == West ? KingsWestTests.Ready() : State(CardCatalog.All[card].Side);
            await LordsPersistenceTests.Restart(state, CommandKind.PlayEvent, Cost(state, card));
        }
    }

    [PostgresFact]
    public async Task NationalCardsRestartBranchRandomRemovalAndBothRecruitmentForms()
    {
        foreach (var (card, sovereign) in new[] { (Dain, SovereignCatalog.Dain), (Thranduil, SovereignCatalog.Thranduil) })
        {
            var state = State();
            Tile(state, sovereign);
            state = Play(state, card);
            await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = ["corruption"] }, 0);
            await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = ["recruit"] });
            state = Choose(state, "recruit");
            var option = new EffectOptions().GetOptions(state, Side.FreePeoples).Single();
            var payload = card == Dain ? new CommandPayload
            {
                Placements = option.Recruitment.Where(g => g.Kind != UnitKind.Elite)
                    .Select(g => new UnitPlacementChoice { UnitId = g.ReserveIds[0], RegionId = "erebor" }).ToList()
            } : new CommandPayload { Ids = [option.EligibleIds[0]] };
            await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, payload);
        }
    }

    [PostgresFact]
    public async Task ServiceRestartsChosenRulerRecruitmentAndRandomReturn()
    {
        var state = State();
        Tile(state, SovereignCatalog.Dain);
        state = Play(state, Service);
        var choice = new CommandPayload { Ids = [SovereignCatalog.Dain] };
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, choice);
        state = Do(state, CommandKind.ChooseEffect, choice);
        var option = new EffectOptions().GetOptions(state, Side.FreePeoples).Single();
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new()
        {
            Placements = [new() { UnitId = option.EligibleIds[0], RegionId = "erebor" }]
        }, 0);
    }

    [PostgresFact]
    public Task VillagesRestartsTheEntireFourNationPlan() =>
        RestartVillages(Play(State(), Villages));

    private static Task RestartVillages(MatchState state) =>
        LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, KingsVillagesTests.Plan(state));

    [PostgresFact]
    public async Task WelcomeRestartsBothPrintedRemovalCosts()
    {
        foreach (var face in new[] { DieFace.Character, DieFace.WillOfTheWest })
        {
            var state = KingsWelcomeTests.OnTable();
            Companion(state, Gimli, "erebor");
            var die = Die(state);
            die.Face = face;
            await LordsPersistenceTests.Restart(state, CommandKind.DiscardWelcome, new() { DieId = die.Id });
        }
    }

    [PostgresFact]
    public async Task WestRestartsTargetEachDrawAndPrivateFinalTileChoice()
    {
        var state = KingsWestTests.Ready(2);
        Pool(state, One, HuntTests.Three);
        state = Play(state, West);
        var target = new CommandPayload { Ids = [SovereignCatalog.Dain] };
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, target, 0);
        state = Do(state, CommandKind.ChooseEffect, target, 0);
        await LordsPersistenceTests.Restart(state, CommandKind.DrawCorruptionTile, new(), 0);
        state = Do(state, CommandKind.DrawCorruptionTile, new(), 0);
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseCorruptionTile, new() { Ids = [One] });
    }

    [PostgresFact]
    public async Task EveryCombatRestartsSecretCommitAndReveal()
    {
        foreach (var card in Ids)
        {
            var state = Combat(card);
            var fpCard = CardCatalog.All[card].Side == Side.FreePeoples ? card : "";
            var shCard = fpCard.Length == 0 ? card : "";
            state.Player(Side.FreePeoples).Hand = fpCard.Length == 0 ? [] : [fpCard];
            state.Player(Side.Shadow).Hand = shCard.Length == 0 ? [] : [shCard];
            var commit = new CommandPayload { CardId = fpCard };
            await LordsPersistenceTests.Restart(state, CommandKind.CommitCombatCard, commit);
            state = Do(state, CommandKind.CommitCombatCard, commit);
            var dice = card == West ? 0 : card == Welcome ? 3 : card is Dain or Thranduil ? 5 : 4;
            await LordsPersistenceTests.Restart(state, CommandKind.CommitCombatCard, new() { CardId = shCard }, new int[dice]);
        }
    }

    internal static MatchState Combat(string card)
    {
        var state = card == West ? KingsCards02CombatTests.VainReady() : Battle();
        var battle = BattleRuntime.Active(state)!;
        if (card is Dain or Thranduil)
            battle.Force(Side.FreePeoples).Participants.Add(BattleFixture.Put(state, "elves", UnitKind.Elite, battle.Force(Side.FreePeoples).Region).Id);
        if (card == Welcome)
        {
            var minion = LordsFixture.Put(state, CharacterRules.WitchKing, battle.Force(Side.Shadow).Region);
            CharacterLifecycle.ApplyVersion(minion, CharacterCatalog.All[CharacterRules.WitchKing]);
            battle.Force(Side.Shadow).Participants.Add(minion.Id);
        }
        return state;
    }

    [PostgresFact]
    public async Task HopeRestartsChosenAwakeningAndVainRestartsZeroOrPaidLeadership()
    {
        var state = KingsCards02CombatTests.InitialHope();
        KingsCards02CombatTests.AddInitial(state, SovereignCatalog.Denethor);
        state = KingsChieftainBattleFixture.Cards(state, "", Service);
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = [SovereignCatalog.Denethor] });
        state = KingsChieftainBattleFixture.Cards(KingsCards02CombatTests.VainReady(), West);
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new(), 0, 0, 0, 0);
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = [state.Pending!.PrivateChoices[0]] }, 0, 0, 0, 0);
    }

    [PostgresFact]
    public Task ServantRestartsItsChosenMinionsEntireLeadershipCost()
    {
        var state = Combat(Welcome);
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        var mouth = LordsFixture.Put(state, CharacterRules.Mouth, force.Region);
        CharacterLifecycle.ApplyVersion(mouth, CharacterCatalog.All[CharacterRules.Mouth]);
        force.Participants.Add(mouth.Id);
        state = KingsChieftainBattleFixture.Cards(state, Welcome);
        return LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = [mouth.Id] }, 0, 0, 0);
    }
}
