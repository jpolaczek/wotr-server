using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public sealed class Projection(params IActionOptionsProvider[] providers)
{
    private static readonly IReadOnlyDictionary<string, Side> NationSides =
        BoardCatalog.LoadBase().Nations.ToDictionary(n => n.Id, n => n.Side);
    public PlayerView For(MatchState state, Side viewer) => Build(state, viewer, true);

    // OBS-02: what an observer sees of one side — its hand and knowledge, who acts and what is being decided —
    // without options, decision tokens, private preferences or what that side could do next.
    public PlayerView Observe(MatchState state, Side perspective)
    {
        var view = Build(state, perspective, false);
        view.Decision = view.Decision is { } decision ? Described(decision) : null;
        view.AutoDecision = new();
        view.RulerTokenActions = [];
        view.Availability = [];
        return view;
    }

    private static DecisionView Described(DecisionView decision) => new()
    {
        Kind = decision.Kind,
        RequiredCount = decision.RequiredCount,
        Presentation = decision.Presentation,
        SourceId = decision.SourceId,
        Window = decision.Window
    };

    private PlayerView Build(MatchState state, Side viewer, bool withOptions)
    {
        var mine = state.Player(viewer);
        var pending = state.Pending;
        var companions = CompanionRules.Inside(state);
        var rulerActions = viewer == Side.FreePeoples ? KingsRulerTokenOptions.Inspect(state) : [];
        return new()
        {
            MatchId = state.Id,
            Configuration = ConfigurationRules.Normalize(state.Configuration),
            Revision = state.Revision,
            ActionSequence = ActionSequenceOptions.View(state),
            Turn = state.Turn,
            Phase = state.Phase,
            You = viewer,
            DecisionOwner = pending?.Owner,
            Winner = state.Winner,
            Victory = state.Victory,
            Units = state.Units.Select(Unit).ToList(),
            Characters = state.Characters.Where(c => BoardRules.Regions.ContainsKey(c.Location)).Select(Character).ToList(),
            Nations = state.Nations.Select(n => new NationView { Id = n.Id, Active = n.Active, StepsToWar = n.StepsToWar }).ToList(),
            Control = new(state.Control),
            Dice = state.Dice.Select(Die).ToList(),
            Rings = state.Rings.Select(r => new RingView { Id = r.Id, Owner = r.Owner }).ToList(),
            CouncilTokens = CouncilTokens.View(state),
            Sovereigns = KingsProjection.Sovereigns(state, viewer),
            RulerTokens = KingsRulerTokenOptions.Tokens(state),
            CombatReplacements = KingsBattle.Replacements(state),
            RulerTokenActions = AvailabilityDescription.ActionWindow(state, viewer) ? rulerActions : [],
            RegionOverrides = BoardProjection.Overrides(state),
            CorruptionAttempt = KingsProjection.Attempt(state, viewer),
            Hand = [.. mine.Hand],
            AutoDecision = AutoDecisions.View(mine),
            EffectTrail = state.Resolution.Frames.Where(f => f.Effect is not null)
                .SelectMany(f => f.Effect!.Committed).Where(c => c.Owner == viewer).Select(c => new EffectTrailView
                {
                    SourceId = c.SourceId,
                    ChoiceId = c.ChoiceId,
                    TargetId = c.TargetId
                }).ToList(),
            Effects = InGameEffectProjection.For(state, viewer),
            Availability = InGameProjection.Availability(state, viewer, rulerActions),
            MilitaryVictory = InGameProjection.Victory(state),
            OwnCompanionVersions = [.. mine.PrivateCompanionVersions],
            OwnCompanions = viewer == Side.FreePeoples ? companions.Select(c => Companion(state, c)).ToList() : [],
            OwnCharacterRoster = CharacterRosterProjection.For(state, viewer),
            Hunt = HuntOptions.View(state),
            HuntReport = HuntReports.View(state),
            HuntPool = HuntPoolProjection.For(state, viewer),
            Battle = BattleProjection.View(state, viewer),
            EventLoss = EventLosses.View(state),
            LastBattleRound = BattleProjection.LastRound(state),
            Fellowship = new()
            {
                RegionId = state.Fellowship.RegionId,
                Progress = state.Fellowship.Progress,
                Corruption = state.Fellowship.Corruption,
                Hidden = state.Fellowship.Hidden,
                Guide = state.Fellowship.Guide,
                CompanionCount = state.Fellowship.Companions.Count,
                CompanionIdentities = companions.Select(c => CharacterCatalog.All[c.DefinitionId].Identity).ToList(),
                RevealedCompanionVersions = state.Lords is null ? [] : new(state.Lords.RevealedCompanionVersions),
                MordorStep = state.Fellowship.MordorStep,
                MordorAttemptedThisTurn = state.Fellowship.MordorStep.HasValue
                    ? state.Fellowship.LastMordorAttemptTurn == state.Turn : null
            },
            Players = state.Players.Select(p => Player(p, state, viewer)).ToList(),
            Decision = pending?.Owner == viewer ? new()
            {
                Id = pending.Id,
                Kind = pending.Kind,
                RequiredCount = pending.RequiredCount,
                SourceId = pending.SourceId,
                Window = pending.Window,
                Choices = [.. pending.PrivateChoices],
                Presentation = string.IsNullOrEmpty(pending.Presentation)
                    ? withOptions ? Presentation(pending.Kind) : KnownPresentation(pending.Kind) ?? ""
                    : pending.Presentation
            } : null,
            Options = withOptions && pending?.Owner == viewer
                ? providers.SelectMany(p => p.GetOptions(state, viewer)).ToList() : [],
            History = HistoryProjection.For(state, viewer)
        };
    }

    internal static CharacterView Character(CharacterState character) => new()
    {
        Id = character.Id,
        DefinitionId = character.DefinitionId,
        Side = character.Side,
        Location = character.Location,
        Garrison = character.Garrison,
        Level = character.Level,
        Leadership = character.Leadership,
        NationIcon = character.NationIcon
    };

    private static CharacterView Companion(MatchState state, CharacterState character)
    {
        var view = Character(character);
        view.Level = CompanionRules.Level(state, character);
        return view;
    }

    internal static UnitView Unit(UnitState unit) => new()
    {
        Id = unit.Id,
        Nation = unit.Nation,
        Side = NationSides[unit.Nation],
        Kind = unit.Kind,
        Location = unit.Location,
        Garrison = unit.Garrison
    };

    private static DieView Die(DieState die) => new()
    {
        Id = die.Id,
        Owner = die.Owner,
        Source = die.Source,
        Face = die.Location is DieLocation.Removed or DieLocation.Unrolled ? null : die.Face,
        Location = die.Location,
        HuntOrigin = die.HuntOrigin,
        SpentWith = die.SpentWith,
        RemoveSymbol = die.Location is not (DieLocation.Removed or DieLocation.Unrolled) && die.Special?.RemoveSymbol == true,
        SpecialDieChosen = die.Special?.Chosen == true,
        PermanentlyRemoved = die.Special?.PermanentlyRemoved == true,
        SourceTitle = DieInspection.SourceTitle(die),
        RecoveryDescription = DieInspection.Recovery(die)
    };

    private static PlayerSummary Player(PlayerState player, MatchState state, Side viewer) => new()
    {
        Side = player.Side,
        TableCards = [.. player.Table],
        VictoryPoints = MilitaryVictory.Points(state, player.Side),
        HandCount = BattleTableCards.PublicHandCount(state, player),
        HandDeckCounts = BattleTableCards.PublicHandDecks(state, player),
        DiscardCount = player.Discard.Count,
        DeckCounts = player.Decks.ToDictionary(d => d.Key, d => d.Value.Count),
        CombatCardCommitted = player.CombatCardCommitted,
        VisibleCombatCard = player.Side == viewer || BattleRuntime.Active(state)?.CardsRevealed == true ? player.CommittedCombatCard : null,
        CombatCardBack = BattleRuntime.Active(state) is { } battle ? BattleCards.Back(battle.Force(player.Side)) : "",
        CorruptionTileCount = player.HiddenCorruptionTiles.Count,
        VisibleCorruptionTiles = Visibility.CanReadCorruption(state, viewer, player.Side)
            ? [.. player.HiddenCorruptionTiles] : [],
        RingUsedThisTurn = player.RingUsedThisTurn
    };
    private static string Presentation(DecisionKind kind) =>
        KnownPresentation(kind) ?? throw new InvalidOperationException("Missing decision presentation.");

    // OBS-02/REPLAY-01: a read-only view describes a decision without a presentation instead of failing.
    private static string? KnownPresentation(DecisionKind kind) => kind switch
    {
        DecisionKind.DiscardCards => "CardGrid",
        DecisionKind.AllocateHunt => "DiceTransfer",
        DecisionKind.SpecialDieChoice => "SpecialDieChoice",
        DecisionKind.RulerDiscard => "RulerDiscard",
        DecisionKind.Dismay => "DismayRerollOrKeep",
        DecisionKind.KeepActionDie => "KeepDieOrDecline",
        DecisionKind.Action => "ObjectIntentCost",
        DecisionKind.FellowshipPhase => "MapPortrait",
        DecisionKind.CombatCard or DecisionKind.Reaction => "CardOrDecline",
        DecisionKind.Losses => "HitsUnits",
        DecisionKind.RemoveExcess => "UnitsToReserve",
        DecisionKind.EffectChoice => "EffectTargets",
        DecisionKind.HuntDamage => "GuideRandomCorruption",
        DecisionKind.HuntReroll => "HuntDice",
        DecisionKind.BattleReroll => "CombatDice",
        DecisionKind.BattleContinue => "ContinueOrStop",
        DecisionKind.BattleRetreat => "RetreatOrHold",
        DecisionKind.BattleAdvance => "AdvanceArmy",
        DecisionKind.StrongholdDefense => "FieldOrStronghold",
        DecisionKind.SiegeContinuation => "EliteOrStop",
        DecisionKind.FellowshipRoute => "MapRoute",
        DecisionKind.Guide => "PortraitChoice",
        DecisionKind.CompanionSeparation or DecisionKind.CharacterEvent => "PortraitRoute",
        DecisionKind.CouncilVersions => "PortraitVersions",
        DecisionKind.CouncilHomelands => "PortraitHomelands",
        DecisionKind.CouncilToken => "ShadowTokenChoice",
        DecisionKind.CorruptionTile => "EyePortraitTile",
        _ => null
    };
}
