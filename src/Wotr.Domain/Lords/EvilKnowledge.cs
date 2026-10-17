using Wotr.Contracts;

namespace Wotr.Domain;

public static class EvilKnowledge
{
    public static bool Available(MatchState state, BattleState battle) => state.Configuration.Lords &&
        state.Lords is { EvilKnowledgeUsed: false } && battle.Mode == BattleMode.Siege && battle.Attacker.Side == Side.Shadow &&
        BattleRules.Characters(state, battle.Attacker).Any(c => CharacterRules.Has(c, LordsCharacters.EvilKnowledge));

    public static void Use(MatchState state, BattleState battle)
    {
        state.Lords!.EvilKnowledgeUsed = true;
        battle.Stage = BattleStage.NextRound;
        state.Pending = null;
        state.Events.Add(new()
        {
            Revision = state.Revision, Kind = "SiegeExtended",
            Text = "The Mouth of Sauron used Evil Knowledge to extend the siege without reducing an Elite."
        });
    }
}
