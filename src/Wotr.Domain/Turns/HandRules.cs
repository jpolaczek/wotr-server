namespace Wotr.Domain;

// All gameplay mutations use these helpers, including future card recovery/transfer effects.
public static class HandRules
{
    public static void Add(PlayerState player, string card)
    {
        player.Hand.Add(card);
        Changed(player);
    }

    public static bool Remove(PlayerState player, string card)
    {
        if (!player.Hand.Remove(card))
            return false;
        Changed(player);
        return true;
    }

    private static void Changed(PlayerState player)
    {
        player.HandVersion++;
        AutoDecisions.Expire(player);
    }
}
