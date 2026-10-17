using Wotr.Contracts;

namespace Wotr.Domain;

public static class LordsDiceSetup
{
    public const string VilyaRing = "lords.ring.vilya";
    public const string NenyaRing = "lords.ring.nenya";
    public const string NaryaRing = "lords.ring.narya";

    // Called by content composition before the first recovery, never a client command.
    // Full Lords content/start remains gated until the character/card modules are ready.
    public static void Initialize(MatchState state)
    {
        if (!state.Configuration.Lords || state.Phase != GamePhase.Setup || state.Lords is not null)
            throw new InvalidOperationException("Lords dice must be initialized once during setup.");
        state.Lords = new();
        foreach (var definition in LordsDiceCatalog.Definitions.Values)
            state.Dice.Add(new()
            {
                Id = definition.Id,
                Source = definition.Id,
                Owner = definition.Owner,
                Location = DieLocation.Removed,
                Special = new()
            });
        state.Rings = new[] { VilyaRing, NenyaRing, NaryaRing }
            .Select(id => new RingState { Id = id, Owner = Side.FreePeoples }).ToList();
    }
}
