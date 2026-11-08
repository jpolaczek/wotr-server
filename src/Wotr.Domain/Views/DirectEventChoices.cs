namespace Wotr.Domain;

// Choices in a directly paid event that are reviewed for reconstruction from
// public facts. Review is by step type: each listed step takes its candidates
// from the public board and the event's own earlier choices, so a position is
// fully described by the card, its payment and the owner's trail and options.
// Every later step runs through the engine. A new step type stays unmarked until
// it is reviewed here.
public static class DirectEventChoices
{
    public static bool IsReviewed(string cardId, int nextStep, EffectDefinition program)
    {
        if (cardId == KingsCards02.Service)
            return nextStep is 1 or 2 && program.Steps is [ServiceSovereignStep, ServiceRecruitStep, ServiceCorruptionStep];
        if (nextStep < 1 || nextStep > program.Steps.Count)
            return false;
        var earlier = program.Steps.Take(nextStep - 1).ToArray();
        var step = program.Steps[nextStep - 1];
        return earlier.All(IsAutomaticPublicStep) && OpensFromPublicBoard(step) ||
            step is MadnessRecruitStep && earlier is [MadnessAttackStep] ||
            step is RecruitEffectStep { Region: "", Leader: false } && earlier.All(LeavesAPublicRecruitRegion) ||
            step is DunlendingMoveStep && earlier is [DunlendingRegionStep, RecruitEffectStep { Region: "", Leader: false }];
    }

    // A reviewed step that reads only the public region an earlier step of its event left.
    public static bool ReadsAnEarlierRegion(EffectStep step) => step is RecruitEffectStep { Region: "" } or DunlendingMoveStep;

    // The one reviewed step a pending choice with this presentation can be at, or 0.
    public static int StepFor(string cardId, string presentation, EffectDefinition program)
    {
        for (var index = 0; index < program.Steps.Count; index++)
            if (program.Steps[index] is EffectChoiceStep choice && choice.Presentation == presentation &&
                IsReviewed(cardId, index + 1, program))
                return index + 1;
        return 0;
    }

    // The step whose roll leaves event losses pending, or 0. These steps apply plain
    // Event casualties; only Madness reads its target again, from the region the loss names.
    public static int LossStep(EffectDefinition program)
    {
        for (var index = 0; index < program.Steps.Count; index++)
            if (program.Steps[index] is ArmyEventDamageStep or EnemyArmyStep or MadnessAttackStep)
                return index + 1;
        return 0;
    }

    // Madness and Horror rolls against a Free Peoples army near a Sauron Stronghold, then
    // recruits beside it, or inside it when that army was besieged there before its losses.
    public static bool IsMadness(EffectDefinition program) => program.Steps is [MadnessAttackStep, MadnessRecruitStep];

    // Return to Valinor repeats its one step for each Elven Stronghold it strikes, each roll
    // leaving losses; the execution's TargetIds hold the regions already struck.
    public static bool RepeatsOnTargets(EffectDefinition program) =>
        program.Steps is [ValinorRegionStep or KingsValinorStep];

    // The owner's recorded choices are the struck regions in order, without the lone options
    // the engine took by itself.
    public static bool RecordsStruckRegions(EffectExecution execution) =>
        execution.TargetIds.Distinct(StringComparer.Ordinal).Count() == execution.TargetIds.Count &&
        execution.TargetIds.All(BoardRules.Regions.ContainsKey) &&
        IsOrderedSubset(execution.Committed.Skip(1).ToArray(), execution);

    private static bool IsOrderedSubset(IReadOnlyList<EffectSelection> chosen, EffectExecution execution)
    {
        var next = 0;
        foreach (var selection in chosen)
        {
            if (selection.Owner != execution.Owner || selection.SourceId != execution.SourceId ||
                selection.ChoiceId != selection.TargetId)
                return false;
            while (next < execution.TargetIds.Count && execution.TargetIds[next] != selection.ChoiceId)
                next++;
            if (next++ == execution.TargetIds.Count)
                return false;
        }
        return true;
    }

    // How many armies Madness could strike now; before its losses, one means it chose alone.
    public static int MadnessTargets(MatchState state) =>
        new MadnessAttackStep().Candidates(state, new EffectExecution()).Count;

    // Whether the army Madness targets is besieged in a Sauron Stronghold, read before its losses.
    public static bool MadnessBesieged(MatchState state, string region, bool garrison) =>
        garrison && BoardRules.Region(state, region) is { Nation: "sauron", Settlement: "Stronghold" } &&
        BoardRules.Besieged(state, region, Wotr.Contracts.Side.FreePeoples);

    // An army event moves or attacks once, with options from the public board. The
    // steps before it are automatic or leave their choice in the owner's trail.
    public static bool LeadsToArmyOrder(EffectDefinition program, int nextStep) =>
        nextStep >= 1 && nextStep <= program.Steps.Count && program.Steps[nextStep - 1] is ArmyOrderStep &&
        program.Steps.Take(nextStep - 1).All(step => step is BlackCaptainStep or MoveNazgulStep or DolGuldurRecruitStep);

    // The army order a reviewed event waits on at this step, or null. The Balrog's
    // own choice opens its order inside its one step.
    public static ArmyEventOrder? ArmyOrderAt(EffectDefinition program, int nextStep,
        IReadOnlyList<EffectSelection> committed) =>
        LeadsToArmyOrder(program, nextStep) && program.Steps[nextStep - 1] is ArmyOrderStep step ? step.Order
            : ChoseBalrog(program, nextStep, committed, BalrogEventStep.Army) ? ArmyEventOrder.BalrogManeuver : null;

    // The Balrog event has one step. Once its owner chooses to travel alone or with an
    // army, the trail ends with that choice, after the card and any Spear of Terror.
    public static bool ChoseBalrog(EffectDefinition program, int nextStep, IReadOnlyList<EffectSelection> committed,
        string choice) =>
        program.Steps is [BalrogEventStep] && nextStep == 1 && committed.Count is 2 or 3 &&
        committed[^1] == new EffectSelection(Wotr.Contracts.Side.Shadow, committed[0].SourceId, choice) &&
        committed.Skip(1).SkipLast(1).All(IsSpearOfTerror);

    // Stormcrow's last step asks the opponent to remove a figure of the nation it struck. The
    // steps before it move that nation's political counter and, in Kings, give its sovereign a
    // face-down tile; both are already on the board, and the offered units name the nation.
    public static bool IsOpponentFigureLoss(EffectDefinition program, int nextStep) =>
        nextStep == program.Steps.Count && program.Steps is [RetreatPoliticalCounterStep, .., var last] &&
        last is StormcrowLossStep or KingsStormcrowLossStep &&
        program.Steps.Skip(1).Take(program.Steps.Count - 2).All(step => step is StormcrowCorruptionStep);

    // A Kings inspection names a nation and then a sovereign in its owner's trail, and shows
    // that sovereign's tiles to the owner alone while they choose one to return.
    // The one sovereign an inspection could read now, or null. A lone sovereign is taken
    // without a choice in the trail, and its tiles stay put until the inspection returns one.
    public static string? LoneInspectedSovereign(MatchState state) =>
        new SelectInspectedSovereignStep().Candidates(state, new EffectExecution()) is [var only] ? only : null;

    public static bool IsInspection(EffectDefinition program) =>
        program.Steps is [KingsNationStep { Activate: false }, SelectInspectedSovereignStep, InspectCorruptionStep];

    public static bool TravelsFirst(EffectDefinition program) => program.Steps.Count > 0 &&
        program.Steps[0] is MoveNazgulStep or BlackCaptainStep;

    public static bool IsCaptainFlight(EffectSelection selection) => selection ==
        new EffectSelection(Wotr.Contracts.Side.Shadow, ShadowCharacterCards04.Captain, BlackCaptainStep.Fly);

    // Accepting Spear of Terror adds this one entry to the event's trail.
    public static bool IsSpearOfTerror(EffectSelection selection) =>
        selection == new EffectSelection(Wotr.Contracts.Side.Shadow, LordsCharacters.WitchKing);

    public static bool BelongsToEvent(EffectSelection selection, string cardId) =>
        selection.SourceId == cardId || IsSpearOfTerror(selection);

    // A Shadow event paid with an Event die draws from the Palantir when it ends,
    // unless Spear of Terror replaced that draw.
    public static bool PalantirDrawExpected(MatchState state, EffectExecution execution) =>
        execution.Owner == Wotr.Contracts.Side.Shadow && execution.PaidFace == Wotr.Contracts.DieFace.Event &&
        state.Player(execution.Owner).Table.Contains(ShadowCharacterCards04.Palantir) &&
        !execution.Committed.Any(IsSpearOfTerror);

    private static bool IsAutomaticPublicStep(EffectStep step) =>
        step is AdvanceFixedNationStep or WitchKingReturnStep;

    // A Kings national card nests its branch and recruitment in programs of their own.
    public static bool IsNationalCard(EffectDefinition program, out string sovereign, out string otherwise,
        out string recruit)
    {
        sovereign = otherwise = recruit = "";
        if (program.Steps is not [KingsNationalCardStep national] ||
            !EffectCatalog.All.Programs.TryGetValue(national.OtherwiseProgram, out var branchProgram) ||
            branchProgram.Steps is not [KingsRecruitOrCleanseStep branch, DrawCardsStep] ||
            branch.Sovereign != national.Sovereign ||
            !EffectCatalog.All.Programs.TryGetValue(branch.RecruitProgram, out var recruitProgram) ||
            recruitProgram.Steps is not [var recruitment] || !OpensFromPublicBoard(recruitment))
            return false;
        sovereign = national.Sovereign;
        otherwise = national.OtherwiseProgram;
        recruit = branch.RecruitProgram;
        return true;
    }

    public static bool OpensFromPublicBoard(EffectStep step) => step is
        RecruitmentBatchStep or RecruitEffectStep { Region.Length: > 0 } or RecruitTogetherStep or RecruitRegionStep or
        HillTrollsStep or CruelWeatherStep or BlackCaptainStep or ArmyEventDamageStep or EnemySovereignStep or
        MadnessAttackStep or
        FateOrcsRegionStep or ValinorRegionStep or KingsValinorStep or WisdomBranchStep or BalrogEventStep or
        LidlessEyeStep;

    private static bool LeavesAPublicRecruitRegion(EffectStep step) => step is
        RecruitRegionStep or ArmyEventDamageStep or RangersRecruitStep or StewardsRegionStep;
}
