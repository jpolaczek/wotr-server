using Wotr.Contracts;

namespace Wotr.Domain;

public static class LordsDiceRoll
{
    public static void RecordCounts(MatchState state)
    {
        if (state.Configuration.Lords && state.Lords is not null)
            state.Lords.RolledCounts = Enum.GetValues<Side>().ToDictionary(side => side,
                side => state.Dice.Count(d => d.Owner == side && d.Location == DieLocation.Unrolled));
    }

    public static void Roll(DieState die, IRandomSource random)
    {
        var faces = LordsDiceCatalog.Definitions[die.Source].Faces;
        var face = faces[random.Next(faces.Count)];
        die.Face = face.Face;
        die.Special!.RemoveSymbol = face.Remove;
        die.Special.Chosen = false;
        die.Location = DieLocation.AwaitingChoice;
        die.HuntOrigin = "";
    }

    public static bool BeginChoices(MatchState state)
    {
        if (!state.Configuration.Lords || state.Lords is null)
            return false;
        var choices = new List<Side>();
        foreach (var side in Enum.GetValues<Side>())
        {
            var dice = Candidates(state, side);
            if (dice.Count == 1)
                Select(state, side, dice[0].Id);
            else if (dice.Count > 1)
                choices.Add(side);
        }
        state.Lords.ChoiceOrder = choices.OrderByDescending(s => state.Lords.RolledCounts[s])
            .ThenBy(s => s == Side.FreePeoples ? 0 : 1).ToList();
        return OpenNext(state);
    }

    public static List<DieState> Candidates(MatchState state, Side side) => state.Dice
        .Where(d => d.Owner == side && d.Special is not null && d.Location == DieLocation.AwaitingChoice).ToList();

    public static List<string> LegalChoices(MatchState state, Side side)
    {
        var dice = Candidates(state, side);
        var eyes = dice.Where(d => d.Face == DieFace.Eye).ToArray();
        return (eyes.Length > 0 ? eyes : dice.AsEnumerable()).Select(d => d.Id).ToList();
    }

    public static bool OpenNext(MatchState state)
    {
        if (state.Lords!.ChoiceOrder.Count == 0)
            return false;
        state.Open(DecisionKind.SpecialDieChoice, state.Lords.ChoiceOrder[0], [CommandKind.ChooseSpecialDie]);
        return true;
    }

    public static void Select(MatchState state, Side side, string chosenId)
    {
        foreach (var die in Candidates(state, side))
        {
            die.Special!.Chosen = die.Id == chosenId;
            die.Location = die.Id != chosenId ? DieLocation.Discarded
                : die.Face == DieFace.Eye ? DieLocation.Hunt : DieLocation.Available;
            if (die.Location == DieLocation.Hunt)
                die.HuntOrigin = side == Side.FreePeoples ? LordsDiceCatalog.KeeperEye : LordsDiceCatalog.LesserEye;
        }
    }
}
