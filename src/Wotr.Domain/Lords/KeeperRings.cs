using Wotr.Contracts;

namespace Wotr.Domain;

public static class KeeperRings
{
    public const string Elrond = "lords.character.elrond";
    public const string Galadriel = "lords.character.galadriel";

    public static bool Available(MatchState state, string ringId) => state.Configuration.Lords && state.Lords is not null &&
        KingsRulerTokens.RingAllowed(state, Side.FreePeoples) && state.Rings.Any(r => r.Id == ringId && r.Owner == Side.FreePeoples);

    public static void Transfer(MatchState state, string ringId)
    {
        if (!Available(state, ringId))
            throw new InvalidOperationException("A Keeper ability requires its available Ring.");
        state.Rings.Single(r => r.Id == ringId).Owner = Side.Shadow;
        KingsRulerTokens.RecordRing(state, Side.FreePeoples);
        state.Events.Add(new()
        {
            Revision = state.Revision,
            Kind = "KeeperRingUsed",
            Text = $"Free Peoples used {ringId}; Shadow now holds this Ring."
        });
    }

    public static string? NaryaNation(MatchState state)
    {
        if (!Available(state, LordsDiceSetup.NaryaRing) ||
            CharacterRules.InPlay(state, LordsDiceCatalog.KeeperGandalf) is not { } gandalf)
            return null;
        var region = BoardRules.Region(state, gandalf.Location);
        return BoardRules.IsSettlement(region) && region.Nation is not null &&
            BoardRules.Nations[region.Nation].Side == Side.FreePeoples &&
            CharacterEntry.Unconquered(state, region.Id, Side.FreePeoples) ? region.Nation : null;
    }

    public static void UseNarya(MatchState state, string dieId)
    {
        var nation = state.Nations.Single(n => n.Id == NaryaNation(state));
        CharacterRules.PayAny(state, Side.FreePeoples, dieId);
        Transfer(state, LordsDiceSetup.NaryaRing);
        var advanced = nation.StepsToWar > 0;
        nation.Active = true;
        nation.StepsToWar = 0;
        if (advanced)
            ActiveEffects.PoliticalAdvance(state, nation.Id, PoliticalAdvanceCause.CompanionAbility);
        state.Events.Add(new() { Revision = state.Revision, Kind = "NationAtWar", Text = $"Narya activated {nation.Id} and brought it to war." });
        TurnFlow.FinishAction(state, Side.FreePeoples);
    }
}
