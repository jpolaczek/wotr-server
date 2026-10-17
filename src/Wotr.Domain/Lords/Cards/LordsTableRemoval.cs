using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public static class LordsTableRemoval
{
    public static bool Handles(CommandKind kind) => kind is CommandKind.DiscardLore or CommandKind.DiscardGates;
    private static string Card(CommandKind kind) => kind == CommandKind.DiscardLore ? LordsShadowCards.Lore : LordsShadowCards.Gates;

    public static ActionOption? Option(MatchState state, Side actor, CommandKind kind)
    {
        if (!state.Configuration.Lords || actor != Side.FreePeoples || state.Pending is not { Kind: DecisionKind.Action, Owner: Side.FreePeoples } ||
            !state.Player(Side.Shadow).Table.Contains(Card(kind)))
            return null;
        if (kind == CommandKind.DiscardLore)
            return TableRingCost.Option(state, kind);
        var guide = CharacterRules.GuideDefinition(state);
        var characterAllowed = guide is CharacterRules.Grey or LordsDiceCatalog.KeeperGandalf or CharacterRules.Gollum;
        var dice = state.Dice.Where(d => d.Owner == actor && d.Location == DieLocation.Available &&
            (d.Face == DieFace.WillOfTheWest || characterAllowed && d.Face == DieFace.Character)).Select(d => d.Id).ToList();
        return dice.Count == 0 ? null : new() { Kind = kind, EligibleDice = dice };
    }

    public static bool Valid(MatchState state, Side actor, GameCommand command) =>
        Option(state, actor, command.Kind)?.EligibleDice.Contains(command.Payload.DieId) == true &&
        (command.Kind == CommandKind.DiscardLore ? TableRingCost.Valid(state, actor, command.Payload) : command.Payload.RingId == "");

    public static void Apply(MatchState state, GameCommand command)
    {
        if (command.Kind == CommandKind.DiscardLore)
            TableRingCost.Pay(state, command.Payload);
        else
            CharacterRules.PayAny(state, Side.FreePeoples, command.Payload.DieId);
        var card = Card(command.Kind);
        TableCardRules.Discard(state, Side.Shadow, card);
        state.Events.Add(new() { Revision = state.Revision, Kind = "TableCardRemoved", Text = $"Free Peoples spent an action to discard {CardCatalog.All[card].Title}." });
        TurnFlow.FinishAction(state, Side.FreePeoples);
    }
}
