namespace Wotr.Domain;

public sealed class EffectCatalog
{
    public const string ReactionProgram = "core.reaction";
    public IReadOnlyDictionary<string, EffectDefinition> Programs { get; }
    public IReadOnlyDictionary<string, EventDefinition> Events { get; }
    public IReadOnlyDictionary<string, EffectReaction> Reactions { get; }
    public IReadOnlyDictionary<string, CombatDefinition> Combats { get; }
    public static EffectCatalog Base { get; } = CreateBase();
    public static EffectCatalog All { get; } = new(
        Base.Programs.Values.Where(p => p.Id != ReactionProgram)
            .Concat(LordsFreePeoplesCards.Programs).Concat(LordsShadowCards.Programs)
            .Concat(KingsFreePeoplesCards01.Programs).Concat(KingsCards02.Programs)
            .Concat(KingsCards03.Programs).Concat(KingsCards04.Programs).Concat(FateCards.Programs),
        Base.Events.Values.Concat(LordsFreePeoplesCards.Events).Concat(LordsShadowCards.Events)
            .Concat(KingsFreePeoplesCards01.Events).Concat(KingsCards02.Events)
            .Concat(KingsCards03.Events).Concat(KingsCards04.Events).Concat(FateCards.Events),
        Base.Reactions.Values.Concat(LordsShadowCards.Reactions).Append(FateGimli.Reaction),
        Base.Combats.Values.Concat(LordsFreePeoplesCards.Combats).Concat(LordsShadowCards.Combats)
            .Concat(KingsFreePeoplesCards01.Combats).Concat(KingsCards02.Combats)
            .Concat(KingsCards03.Combats).Concat(KingsCards04.Combats).Concat(FateCards.Combats));

    public EffectCatalog(IEnumerable<EffectDefinition> programs, IEnumerable<EventDefinition> events,
        IEnumerable<EffectReaction>? reactions = null, IEnumerable<CombatDefinition>? combats = null)
    {
        Programs = programs.Append(new(ReactionProgram, [])).ToDictionary(p => p.Id);
        Events = events.ToDictionary(e => e.CardId);
        Reactions = (reactions ?? []).ToDictionary(r => r.Id);
        Combats = (combats ?? []).ToDictionary(c => c.CardId);
        if (Combats.Values.Any(c => c.Effects.Count == 0 || c.Effects.Any(e => e.Initiative is < 0 or > 7 || !Programs.ContainsKey(e.ProgramId))) ||
            Events.Values.Any(e => !Programs.ContainsKey(e.ProgramId)) ||
            Reactions.Values.Any(r => !Programs.ContainsKey(r.ProgramId) ||
                r.Cost == ReactionCost.DiscardTableCard && r.RequiredTableCard is null))
            throw new ArgumentException("Every card and reaction must reference a registered program.");
    }

    private static EffectCatalog CreateBase()
    {
        const string bilbo = "base.fp.character.12";
        const string wisdom = "base.fp.strategy.08";
        const string celeborn = "base.fp.strategy.15";
        return new([
            new(bilbo, [new HealCorruptionStep(1, 2)]),
            new(wisdom, [new AdvanceNationStep(true)]),
            new(celeborn, [new RecruitEffectStep("elves", "lorien"), new DrawCardsStep("strategy")]),
            NenyaReaction.Program,
            CouncilHobbitReturn.Program,
            KingsPolitics.Program,
            KingsAnarion.Program,
            .. KingsBattle.Programs,
            .. KingsChieftainCombat.Programs,
            .. ChiefReactions.Programs,
            .. CouncilGuideReactions.Programs,
            .. CharacterReactions.Programs,
            .. FreePeoplesCharacterCards01.Programs,
            .. FreePeoplesCharacterCards02.Programs,
            .. FreePeoplesCharacterCards03.Programs,
            .. FreePeoplesCharacterCards04.Programs,
            .. FreePeoplesStrategyCards01.Programs,
            .. FreePeoplesStrategyCards02.Programs,
            .. FreePeoplesStrategyCards03.Programs,
            .. FreePeoplesStrategyCards04.Programs,
            .. ShadowCharacterCards01.Programs,
            .. ShadowCharacterCards02.Programs,
            .. ShadowCharacterCards03.Programs,
            .. ShadowCharacterCards04.Programs,
            .. ShadowStrategyCards01.Programs,
            .. ShadowStrategyCards02.Programs,
            .. ShadowStrategyCards03.Programs,
            .. ShadowStrategyCards04.Programs
        ], [
            new(bilbo, bilbo, new UnconditionalEvent()),
            new(wisdom, wisdom, new FreePeoplesNationTarget()),
            new(celeborn, celeborn, new UnconditionalEvent()),
            .. FreePeoplesCharacterCards01.Events,
            .. FreePeoplesCharacterCards02.Events,
            .. FreePeoplesCharacterCards03.Events,
            .. FreePeoplesCharacterCards04.Events,
            .. FreePeoplesStrategyCards01.Events,
            .. FreePeoplesStrategyCards02.Events,
            .. FreePeoplesStrategyCards03.Events,
            .. FreePeoplesStrategyCards04.Events,
            .. ShadowCharacterCards01.Events,
            .. ShadowCharacterCards02.Events,
            .. ShadowCharacterCards03.Events,
            .. ShadowCharacterCards04.Events,
            .. ShadowStrategyCards01.Events,
            .. ShadowStrategyCards02.Events,
            .. ShadowStrategyCards03.Events,
            .. ShadowStrategyCards04.Events
        ], [.. CharacterReactions.Reactions, .. FreePeoplesCharacterCards01.Reactions, .. FreePeoplesCharacterCards02.Reactions,
            .. ShadowCharacterCards03.Reactions, NenyaReaction.Reaction, .. ChiefReactions.Reactions, .. CouncilGuideReactions.Reactions],
        [
            .. FreePeoplesCharacterCards01.Combats,
            .. FreePeoplesCharacterCards02.Combats,
            .. FreePeoplesCharacterCards03.Combats,
            .. FreePeoplesCharacterCards04.Combats,
            .. FreePeoplesStrategyCards01.Combats,
            .. FreePeoplesStrategyCards02.Combats,
            .. FreePeoplesStrategyCards03.Combats,
            .. FreePeoplesStrategyCards04.Combats,
            .. ShadowCharacterCards01.Combats,
            .. ShadowCharacterCards02.Combats,
            .. ShadowCharacterCards03.Combats,
            .. ShadowCharacterCards04.Combats,
            .. ShadowStrategyCards01.Combats,
            .. ShadowStrategyCards02.Combats,
            .. ShadowStrategyCards03.Combats,
            .. ShadowStrategyCards04.Combats
        ]);
    }
}
