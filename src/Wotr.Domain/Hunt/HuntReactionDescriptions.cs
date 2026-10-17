namespace Wotr.Domain;

// Summaries of registered reactions, not an alternative source of legality or costs.
internal static class HuntReactionDescriptions
{
    public static (string Title, string Detail)? Describe(EffectReaction reaction) => reaction.Id switch
    {
        FreePeoplesCharacterCards01.ReplaceReaction => ("Replace tile",
            "Draw a replacement before determining damage. The new tile is unknown."),
        FreePeoplesCharacterCards01.ProtectReaction or FreePeoplesCharacterCards02.HornReaction => ("Reduce damage",
            "Reduce Hunt damage by 1, to a minimum of 0."),
        FreePeoplesCharacterCards02.StaffReaction => ("Prevent tile draw",
            "Do not draw this Hunt tile. Continue the action after this Hunt."),
        ShadowCharacterCards03.CrebainReaction => ("Increase roll bonus",
            "Add +1 to this Hunt roll and its rerolls. This does not grant a reroll."),
        ShadowCharacterCards03.WornReaction => ("Discard a Character card",
            "Choose a Free Peoples Character card on the table, or discard a random one from their hand if any."),
        ShadowCharacterCards03.BalrogReaction => ("Extra Hunt tile",
            "Draw an additional Hunt tile for the route through Moria; ignore Eye tiles."),
        _ when reaction.Id == ShadowCharacterCards03.BalrogReaction + ".declaration" => ("Extra Hunt tile",
            "Draw an additional Hunt tile; ignore Eye tiles. Also ignore Reveal when declaring in a Free Peoples city or stronghold."),
        NenyaReaction.Id => ("Nenya · Replace Eye",
            "Give Nenya to Shadow, remove this Eye from the game and draw another tile."),
        WeShallGetIt.Reaction => ("We Shall Get It",
            "Discard this card to draw a replacement Hunt tile before its effects resolve."),
        _ when reaction.RequiredTableCard == LordsShadowCards.Gates => ("The Gates are Closed",
            "Discard this card for an additional Hunt tile; ignore Eye and Reveal."),
        _ => null
    };
}
