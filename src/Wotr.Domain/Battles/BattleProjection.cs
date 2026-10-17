using Wotr.Contracts;

namespace Wotr.Domain;

public static class BattleProjection
{
    public static BattleView? LastRound(MatchState state) => state.LastBattleRound is null ? null :
        System.Text.Json.JsonSerializer.Deserialize<BattleView>(System.Text.Json.JsonSerializer.Serialize(state.LastBattleRound));

    public static void RememberRound(MatchState state, BattleState battle)
    {
        if (!battle.CardsRevealed)
            return;
        var report = View(state, battle.Attacker.Side)!;
        report.Stage = "RoundComplete";
        report.Attacker.OwnRerollSelection.Clear();
        report.Defender.OwnRerollSelection.Clear();
        report.OwnTableCombatCards.Clear();
        state.LastBattleRound = report;
        state.Events.Add(new()
        {
            Revision = state.Revision, Kind = "BattleRoundCompleted", Text = $"Round {battle.Round} completed.",
            Activity = HistoryFacts.Battle(state, battle)
        });
    }

    public static BattleView? View(MatchState state, Side viewer)
    {
        var battle = BattleRuntime.Active(state);
        if (battle is null)
            return null;
        return View(state, battle, viewer);
    }

    internal static BattleView Public(MatchState state, BattleState battle) => View(state, battle, null);

    private static BattleView View(MatchState state, BattleState battle, Side? viewer)
    {
        var losses = BattleLosses.Current(state);
        return new()
        {
            Id = battle.Id,
            RegionId = battle.Region,
            Round = battle.Round,
            MandatorySiegeRounds = battle.MandatorySiegeRounds,
            CompanionRequiredForCard = battle.FirstRoundRequiresCompanionCard && battle.Round == 1,
            DenethorBlocksCards = DenethorRules.BlocksCards(state, battle, Side.FreePeoples),
            VoluntaryLosses = losses?.Voluntary == true,
            Stage = battle.Stage.ToString(),
            Mode = battle.Mode,
            CardsRevealed = battle.CardsRevealed,
            DefenderSheltered = battle.DefenderSheltered,
            ArrivalBeforeBattle = battle.ArrivalBeforeBattle,
            WhiteRiderActive = BattleRules.WhiteRiderActive(state),
            OwnTableCombatCards = viewer.HasValue ? [.. BattleTableCards.Candidates(state, battle, viewer.Value)] : [],
            Attacker = Force(state, battle, battle.Attacker, viewer),
            Defender = Force(state, battle, battle.Defender, viewer),
            LossOwner = losses?.Owner,
            PendingHits = losses?.Hits ?? 0,
            LossSource = losses?.SourceId ?? "",
            SpecialAttacks = battle.SpecialAttacks.Select(a => new BattleAttackView { Side = a.Owner, SourceId = a.SourceId, Dice = a.Dice.Select(Die).ToList() }).ToList()
        };
    }

    private static BattleForceView Force(MatchState state, BattleState battle, BattleForce force, Side? viewer) => new()
    {
        Side = force.Side,
        RegionId = force.Region,
        Garrison = force.Garrison,
        ParticipantIds = [.. force.Participants],
        Units = BattleRules.Units(state, force).Select(Projection.Unit).ToList(),
        Characters = BattleRules.Characters(state, force).Select(Projection.Character).ToList(),
        Strength = BattleRules.Strength(state, force),
        CaptainBonus = BattleRules.CaptainBonus(state, force),
        Leadership = BattleLeadership.Total(state, force),
        ForfeitedLeadership = force.Round.ForfeitedLeadership.ToDictionary(p => p.Key, p => p.Value),
        CancelledAbilities = [.. force.Round.CancelledAbilities],
        CombatDice = BattleRoll.DiceCount(state, force),
        CombatDiceReduction = force.CombatDiceReduction,
        HitTarget = BattleRules.HitTarget(state, battle, force),
        CombatModifier = force.Round.CombatModifier,
        RerollModifier = force.Round.RerollModifier,
        Hits = BattleRoll.HitCount(force),
        SpentHits = force.Round.SpentHits,
        CardCommitted = force.Round.CardCommitted,
        CardBack = BattleCards.Back(force),
        CardId = force.Side == viewer || battle.CardsRevealed ? force.Round.CardId : null,
        CardCancelled = force.Round.CardCancelled,
        Dice = force.Round.Dice.Select(Die).ToList(),
        OwnRerollSelection = force.Side == viewer ? [.. force.Round.SelectedRerolls] : []
    };

    private static BattleDieView Die(BattleDie die) => new()
    {
        Id = die.Id,
        Result = die.Result,
        Modifier = die.Modifier,
        HitTarget = die.HitTarget,
        Hit = die.Hit || die.ConvertedHit,
        ConvertedHit = die.ConvertedHit,
        Reroll = die.Reroll,
        RerollModifier = die.RerollModifier,
        RerollHit = die.RerollHit,
        CannotReroll = die.CannotReroll
    };
}
