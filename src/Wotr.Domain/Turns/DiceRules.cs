using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public static class DiceRules
{
    private static readonly IReadOnlyDictionary<Side, DieDefinition> Definitions = BoardCatalog.LoadBase().Dice.ToDictionary(d => d.Side);
    public const string FellowshipMove = "fellowship-move";

    public static void Recover(MatchState state)
    {
        state.MandatoryHuntAllocation = state.Dice.Any(d => d.Owner == Side.FreePeoples &&
            d.Location == DieLocation.Hunt && d.HuntOrigin == FellowshipMove);
        LordsDiceRecovery.Recover(state, state.MandatoryHuntAllocation);
        KingsDice.Recover(state);
        foreach (var player in state.Players)
        {
            var definition = Definitions[player.Side];
            var count = definition.InitialCount + Math.Clamp(player.ExtraDiceNextRecovery, 0, definition.MaximumCount - definition.InitialCount);
            var index = 0;
            foreach (var die in state.Dice.Where(d => d.Owner == player.Side && d.Source == "standard"))
            {
                die.Location = index++ < count ? DieLocation.Unrolled : DieLocation.Removed;
                die.HuntOrigin = "";
            }
        }
    }

    public static void Roll(MatchState state, IRandomSource random)
    {
        LordsDiceRoll.RecordCounts(state);
        var shadowRuler = KingsDice.Ruler(state, Side.Shadow)?.Location == DieLocation.Unrolled;
        foreach (var die in state.Dice.Where(d => d.Location == DieLocation.Unrolled))
        {
            if (state.Configuration.Lords && die.Special is not null)
            {
                LordsDiceRoll.Roll(die, random);
                continue;
            }
            RollActionDie(die, random, shadowRuler);
        }
    }

    public static void RollActionDie(DieState die, IRandomSource random, bool deferEyes = false)
    {
        var faces = Definitions[die.Owner].Faces;
        die.Face = faces[random.Next(faces.Count)];
        die.Location = die.Face != DieFace.Eye ? DieLocation.Available
            : deferEyes ? DieLocation.AwaitingChoice : DieLocation.Hunt;
        die.HuntOrigin = die.Location == DieLocation.Hunt ? "rolled-eye" : "";
    }

    public static DieState? AvailableDie(MatchState state, Side actor, string id) =>
        state.Dice.Find(d => d.Id == id && d.Owner == actor && d.Location == DieLocation.Available);

    public static bool CanDraw(DieState die) => CanPay(die, DieFace.Event) || CanPay(die, DieFace.CardDraw);

    public static bool IsShadowHuntDie(DieState die) => die.Location == DieLocation.Hunt &&
        (die.Owner == Side.Shadow || die.Special is not null && die.HuntOrigin == LordsDiceCatalog.KeeperEye);

    public static bool CanPay(DieState die, DieFace required) => die.Location == DieLocation.Available &&
        (die.Face == required || die.Face == DieFace.WillOfTheWest ||
            (die.Face == DieFace.ArmyMuster && required is DieFace.Army or DieFace.Muster));

    public static void Pay(MatchState state, Side actor, string id, DieFace required)
    {
        var die = AvailableDie(state, actor, id);
        if (die is null || !CanPay(die, required))
            throw new InvalidOperationException("Cost must be validated before applying an action.");
        state.HistoryCapture?.Pay(die, required);
        if (state.Lords is not null && die.Face == DieFace.WillOfTheWest)
            state.Lords.PaidWillOfWestIds.Add(id);
        CaptainOfDespair.RecordCost(state, die);
        if (die.Face == DieFace.WillOfTheWest)
            die.Face = required;
        die.Location = DieLocation.Resolving;
        state.Resolution.PaidDieIds.Add(id);
    }

    // Called after the complete Fellowship/Hunt resolution, not when movement is merely declared.
    public static void CompleteCost(MatchState state, Side actor, bool fellowshipMove)
    {
        if (fellowshipMove && actor != Side.FreePeoples)
            throw new InvalidOperationException("Only Free Peoples can pay for Fellowship movement.");
        foreach (var id in state.Resolution.PaidDieIds)
        {
            var die = state.Dice.Single(d => d.Id == id);
            if (die.Owner != actor || die.Location != DieLocation.Resolving)
                throw new InvalidOperationException("Resolving cost lost its owner or location.");
            var fellowshipBox = fellowshipMove && CouncilStrider.ToFellowshipBox(state, id);
            var entersHunt = fellowshipMove && !fellowshipBox && !ActiveEffects.PreventsFellowshipHuntDie(state);
            die.Location = fellowshipBox ? DieLocation.FellowshipBox : entersHunt ? DieLocation.Hunt : DieLocation.Spent;
            die.HuntOrigin = entersHunt ? FellowshipMove : "";
            CaptainOfDespair.CompleteCost(state, die);
        }
        state.Resolution.PaidDieIds.Clear();
        state.Lords?.PaidWillOfWestIds.Clear();
        state.Lords?.CaptainDice.Clear();
        if (state.Lords?.Council is { } council)
            council.FellowshipBoxDieId = "";
    }

    public static List<string> HuntCandidates(MatchState state) => state.Dice
        .Where(d => d.Owner == Side.Shadow && d.Source == "standard" && d.Location == DieLocation.Unrolled)
        .Select(d => d.Id).ToList();
    public static int HuntMinimum(MatchState state) => state.MandatoryHuntAllocation ? 1 : 0;
    public static int HuntMaximum(MatchState state) => Math.Min(HuntCandidates(state).Count, Math.Max(1, state.Fellowship.Companions.Count));
}
