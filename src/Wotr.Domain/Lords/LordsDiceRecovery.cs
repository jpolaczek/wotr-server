using Wotr.Contracts;

namespace Wotr.Domain;

public static class LordsDiceRecovery
{
    public static void Recover(MatchState state, bool recoveredFellowshipDie)
    {
        if (!state.Configuration.Lords || state.Lords is null)
            return;
        state.Lords.ChoiceOrder.Clear();
        state.Lords.RolledCounts.Clear();
        foreach (var die in state.Dice.Where(d => d.Special is not null))
        {
            var definition = LordsDiceCatalog.Definitions[die.Source];
            var special = die.Special!;
            special.PermanentlyRemoved |= MustRemove(state, die, definition);
            die.Location = !special.PermanentlyRemoved && Eligible(state, definition, recoveredFellowshipDie)
                ? DieLocation.Unrolled : DieLocation.Removed;
            die.HuntOrigin = "";
            die.SpentWith = null;
            special.Chosen = false;
            special.RemoveSymbol = false;
        }
    }

    private static bool Eligible(MatchState state, SpecialDieDefinition definition, bool recoveredFellowshipDie)
    {
        if (definition.Id == LordsDiceCatalog.Narya)
            return CharacterRules.GuideDefinition(state) == LordsDiceCatalog.KeeperGandalf &&
                (state.Turn == 1 || recoveredFellowshipDie);
        return CharacterRules.InPlay(state, definition.CharacterId) is not null;
    }

    private static bool MustRemove(MatchState state, DieState die, SpecialDieDefinition definition)
    {
        var character = state.Characters.Find(c => c.DefinitionId == definition.CharacterId);
        if (character?.Location is "casualties" or "removed")
            return true;
        if (die.Source == LordsDiceCatalog.Gothmog && CharacterRules.InPlay(state, CharacterRules.WitchKing) is not null)
            return true;
        if (!die.Special!.Chosen || !die.Special.RemoveSymbol)
            return false;
        return die.Owner == Side.FreePeoples
            ? CharacterRules.InPlay(state, CharacterRules.White) is not null
            : CharacterRules.InPlay(state, CharacterRules.WitchKing) is not null ||
                CharacterRules.InPlay(state, LordsDiceCatalog.ChiefWitchKing) is not null;
    }
}
