using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public sealed class BattleCards(EffectCatalog catalog)
{
    public static bool PubliclyProhibited(MatchState state, BattleState battle, Side actor) =>
        DenethorRules.BlocksCards(state, battle, actor) ||
        battle.FirstRoundRequiresCompanionCard && battle.Round == 1 && actor == Side.FreePeoples &&
        !BattleRules.Characters(state, battle.Force(actor)).Any(CharacterRules.IsCompanion);

    public bool CanCommit(MatchState state, BattleState battle, Side actor, string card) => card is not null && (card.Length == 0 ||
        !PubliclyProhibited(state, battle, actor) &&
        (state.Player(actor).Hand.Contains(card) || BattleTableCards.Available(state, battle, actor, card)) &&
        CardCatalog.Enabled(state, card) && CardCatalog.All.TryGetValue(card, out var metadata) && metadata.Side == actor &&
        catalog.Combats.TryGetValue(card, out var definition) && definition.Condition.CanPlay(state, battle, actor));

    public static string Back(BattleForce force) => CardCatalog.All.TryGetValue(force.Round.CardId, out var card) ? card.Deck : "";

    public void Commit(MatchState state, BattleState battle, Side actor, string card)
    {
        var round = battle.Force(actor).Round;
        round.CardCommitted = true;
        round.CardId = card;
        round.CardFromTable = BattleTableCards.Available(state, battle, actor, card);
        var player = state.Player(actor);
        player.CombatCardCommitted = !string.IsNullOrEmpty(card);
        player.CommittedCombatCard = string.IsNullOrEmpty(card) ? null : card;
        if (card.Length > 0 && !round.CardFromTable)
            HandRules.Remove(player, card);
        state.Pending = null;
        state.Events.Add(new()
        {
            Revision = state.Revision,
            Kind = "CombatCardCommitted",
            Activity = CommitFacts(state, battle, actor, card),
            Text = string.IsNullOrEmpty(card) ? $"{actor} declined a combat card." : $"{actor} committed a {Back(battle.Force(actor))} combat card."
        });
        if (actor == battle.Attacker.Side)
            battle.Stage = BattleStage.DefenderCard;
        else if (!KingsChieftainCombat.Offer(state, battle))
            Reveal(state, battle);
    }

    public void Reveal(MatchState state, BattleState battle)
    {
        battle.CardsRevealed = true;
        BattleTableCards.Reveal(state, battle);
        battle.Stage = BattleStage.BeforeRoll;
        // Stable ordering of each card's programs; the defender wins equal initiatives.
        battle.Work = new[] { battle.Defender, battle.Attacker }.Where(f => f.Round.CardId.Length > 0)
            .SelectMany(f => KingsChieftainCombat.Effects(f, catalog.Combats[f.Round.CardId])
                .Select(e => new CombatWork(f.Side, f.Round.CardId, e.ProgramId, e.Initiative)))
            .OrderBy(w => w.Initiative).ToList();
        foreach (var force in new[] { battle.Attacker, battle.Defender })
            if (force.Round.CardId.Length > 0)
                state.Events.Add(new() { Revision = state.Revision, Kind = "CombatCardRevealed",
                    Activity = HistoryFacts.Card(state, force.Side, ActionCardUse.CombatReveal, force.Round.CardId),
                    Text = $"{force.Side} revealed {force.Round.CardId}." });
    }

    private static ActionEvent CommitFacts(MatchState state, BattleState battle, Side actor, string card)
    {
        var result = HistoryFacts.Battle(state, battle);
        result.Actor = actor;
        result.Command = CommandKind.CommitCombatCard;
        result.Facts.Cards.Add(new()
        {
            Owner = actor, Use = ActionCardUse.CombatCommit, Deck = Back(battle.Force(actor)),
            Count = card.Length > 0 ? 1 : 0
        });
        return result;
    }

    public static void Discard(MatchState state, BattleState battle)
    {
        foreach (var force in new[] { battle.Attacker, battle.Defender })
        {
            var player = state.Player(force.Side);
            if (player.CommittedCombatCard is { } card)
                player.Discard.Add(card);
            player.CombatCardCommitted = false;
            player.CommittedCombatCard = null;
        }
    }
}
