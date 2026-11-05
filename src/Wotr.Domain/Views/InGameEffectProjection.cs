using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public static class InGameEffectProjection
{
    public static List<ActiveEffectView> For(MatchState state, Side viewer)
    {
        var result = state.ActiveEffects.Where(e => Visibility.CanRead(viewer, e.OnlyFor))
            .Select(e => Effect(state, e)).ToList();
        var sources = result.Select(e => e.SourceId).ToHashSet(StringComparer.Ordinal);
        foreach (var player in state.Players)
            foreach (var card in player.Table.Where(id => !sources.Contains(id)))
                result.Add(TableCard(state, card, player.Side));
        if (state.Configuration.Kings && state.Kings is not null)
            foreach (var sovereign in state.Kings.Sovereigns.Where(s => s.Status == SovereignStatus.Corrupted))
                result.AddRange(SovereignEffects(sovereign));
        return result;
    }

    private static ActiveEffectView Effect(MatchState state, ActiveEffectState effect)
    {
        var view = TableCard(state, effect.SourceId, effect.Owner);
        view.Id = effect.Id;
        view.Modifier = effect.Modifier.ToString();
        view.Duration = effect.Duration.ToString();
        view.ExpiresWhen = effect.ExpiresWhen.ToString();
        view.ExpiryDescription = Expiry(effect);
        view.TargetId = effect.TargetId;
        view.Targets.Add(Scope(state, effect));
        return view;
    }

    private static EffectTargetView Scope(MatchState state, ActiveEffectState effect) => effect.Modifier switch
    {
        EffectModifier.RestrictRohanActivation => new() { Kind = "Nation", Id = "rohan", Label = "Rohan" },
        EffectModifier.PreventMinasTirithCombatCards => Target(state, "minas-tirith"),
        EffectModifier.PreventPassiveMusterAdvance => new() { Kind = "Politics", Id = "free-peoples", Label = "Inactive Free Peoples nations" },
        EffectModifier.PreventFellowshipHuntDie => new() { Kind = "Fellowship", Id = "fellowship", Label = "Fellowship movement" },
        // Saruman table effects store the expiry subject in TargetId, not their affected scope.
        _ when effect.TargetId.Length > 0 && effect.ExpiresWhen != EffectExpiry.CharacterLeavesPlay => Target(state, effect.TargetId),
        _ => new() { Kind = "Global", Label = "See source conditions" }
    };

    private static ActiveEffectView TableCard(MatchState state, string id, Side owner)
    {
        CardCatalog.All.TryGetValue(id, out var card);
        CharacterCatalog.All.TryGetValue(id, out var character);
        return new()
        {
            Id = $"table:{id}", SourceId = id, Owner = owner,
            SourceKind = card is not null ? "Card" : character is not null ? "Ability" : "Effect",
            Title = card?.Title ?? character?.Title ?? id,
            Description = card is not null ? CardTextCatalog.Event(id, state.Configuration) : "Public effect; inspect its source.",
            SourceReference = card?.Source ?? id,
            Duration = EffectDuration.UntilRemoved.ToString(),
            ExpiresWhen = EffectExpiry.None.ToString(),
            ExpiryDescription = "Until removed according to the source."
        };
    }

    private static EffectTargetView Target(MatchState state, string id)
    {
        if (BoardRules.Regions.TryGetValue(id, out var region))
            return new() { Kind = "Region", Id = id, Label = region.Name };
        if (BoardRules.Nations.ContainsKey(id))
            return new() { Kind = "Nation", Id = id, Label = id };
        var character = state.Characters.Find(c => c.Id == id && CharacterRules.OnBoard(c));
        return new() { Kind = "Character", Id = id,
            Label = character is not null && CharacterCatalog.All.TryGetValue(character.DefinitionId, out var definition)
                ? definition.Title : id };
    }

    private static string Expiry(ActiveEffectState effect)
    {
        var duration = effect.Duration switch
        {
            EffectDuration.EndTurn => "At the end of this turn",
            EffectDuration.EndBattle => "At the end of this battle",
            EffectDuration.EndCombatRound => "At the end of this combat round",
            _ => "When removed"
        };
        var condition = effect.ExpiresWhen switch
        {
            EffectExpiry.FreeNationAdvancedByAttackOrCompanion => "a Free Peoples nation advances by an attack or Companion ability",
            EffectExpiry.CharacterLeavesPlay when effect.Modifier == EffectModifier.RestrictRohanActivation => "the source character leaves play or Rohan activates",
            EffectExpiry.CharacterLeavesPlay => "the source character leaves play",
            EffectExpiry.FellowshipCompanionsAbsent => "none of the Companions required by the source remain in the Fellowship",
            EffectExpiry.AragornArmyOutsideFreeNation => "the Fellowship is located, or Aragorn no longer meets the source's army conditions at action end",
            EffectExpiry.FellowshipDeclaresSanctuary => "the Fellowship declares in an unconquered Free Peoples city or stronghold",
            EffectExpiry.MinasTirithSiegeEnds => "the siege of Minas Tirith ends",
            EffectExpiry.SmeagolNoLongerGuide => "Sméagol is no longer the Guide",
            EffectExpiry.CaptainConditionsLost => "the Captain of Despair conditions no longer hold",
            _ => ""
        };
        return condition.Length == 0 ? duration + "." : duration + ", or when " + condition + ".";
    }

    // UI-414 (owner): the Effects tab lists what holds by itself: each Corrupted sovereign's recruitment ban, titled by its
    // ability and naming its place, and Théoden's Mark of the White Hand. Brand's Shadows in the North and the Fate
    // abilities are the Shadow's optional uses, offered in battle and described with the character, so they are not
    // effects (KINGS pp.12-13).
    private static IEnumerable<ActiveEffectView> SovereignEffects(SovereignState sovereign)
    {
        var definition = SovereignCatalog.All[sovereign.Id];
        var name = definition.Title.Split(' ')[0];
        var target = sovereign.Id switch
        {
            SovereignCatalog.Brand => new EffectTargetView { Kind = "Nation", Id = "north", Label = "North figures in every region" },
            SovereignCatalog.Theoden => new EffectTargetView { Kind = "NationRegions", Id = "rohan", Label = "Regions of Rohan" },
            _ => new EffectTargetView { Kind = "Region", Id = definition.Home, Label = BoardRules.Regions[definition.Home].Name }
        };
        yield return new()
        {
            Id = $"{sovereign.Id}:recruitment", SourceId = definition.CorruptedCard, SourceKind = "Ability",
            Title = "Corrupted ruler · " + name, Owner = Side.Shadow, SourceReference = "KINGS pp.12–13; " + definition.CorruptedCard,
            Description = RecruitmentWeakness(sovereign.Id),
            Duration = "WhileCorrupted", ExpiresWhen = "SovereignRemoved",
            ExpiryDescription = "When this corrupted sovereign is removed.", Targets = [target]
        };
        if (sovereign.Id != SovereignCatalog.Theoden)
            yield break;
        yield return new()
        {
            Id = $"{sovereign.Id}:white-hand", SourceId = definition.CorruptedCard, SourceKind = "Ability",
            Title = "Mark of the White Hand · " + name, Owner = Side.Shadow, SourceReference = "KINGS pp.12–13; " + definition.CorruptedCard,
            Description = "While Shadow controls Helm's Deep, it is an Isengard Stronghold but still counts as a captured Free Peoples " +
                "Stronghold for victory points.",
            Duration = "WhileCorrupted", ExpiresWhen = "SovereignRemoved",
            ExpiryDescription = "When this corrupted sovereign is removed.",
            Targets = [new() { Kind = "Region", Id = "helms-deep", Label = BoardRules.Regions["helms-deep"].Name }]
        };
    }

    // The ban covers Muster and character abilities (KingsWeaknesses.BlocksRecruit), never an Event card's recruit.
    private static string RecruitmentWeakness(string sovereignId) => sovereignId switch
    {
        SovereignCatalog.Brand => "North Leaders and Elites cannot be recruited anywhere, except by Event cards.",
        SovereignCatalog.Theoden => "Free Peoples Leaders and Elites cannot be recruited in Rohan's regions, except by Event cards.",
        SovereignCatalog.Thranduil => "Elites cannot be recruited in the Woodland Realm, except by Event cards; Leaders still can.",
        SovereignCatalog.Dain => "Leaders and Elites cannot be recruited in Erebor, except by Event cards.",
        _ => "Leaders and Elites cannot be recruited in Minas Tirith, except by Event cards."
    };
}
