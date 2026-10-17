using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public class MusterTests
{
    private static readonly GameEngine Engine = new(new TurnCommands(), new MusterCommands());

    [Theory]
    [InlineData(false, 3, true, 2)]
    [InlineData(false, 2, true, 1)]
    [InlineData(false, 1, false, 1)]
    [InlineData(true, 1, true, 0)]
    [InlineData(true, 0, false, 0)]
    public void DiplomacyRespectsTheIndependentActivationGate(bool active, int steps, bool accepted, int expected)
    {
        var state = Muster();
        var nation = state.Nations.Single(n => n.Id == "gondor");
        nation.Active = active;
        nation.StepsToWar = steps;
        var result = Execute(state, CommandKind.AdvancePolitics, new() { NationId = nation.Id });
        Assert.Equal(accepted, result.Receipt.Accepted);
        Assert.Equal(expected, result.State.Nations.Single(n => n.Id == nation.Id).StepsToWar);
        Assert.Equal(active, result.State.Nations.Single(n => n.Id == nation.Id).Active);
        Assert.Equal(accepted ? DieLocation.Spent : DieLocation.Available, result.State.Dice[0].Location);
    }

    [Fact]
    public void AnAttackAndACaptureAdvanceTwiceButMultipleUnitsDoNotMultiplyTheAttack()
    {
        var state = Muster();
        var north = state.Nations.Single(n => n.Id == "north");
        var defenders = state.Units.Where(u => u.Nation == "north" && BoardRules.IsArmyUnit(u)).Take(2).ToArray();
        PoliticalRules.ArmyAttacked(state, defenders, PoliticalAttack.ArmyBattle);
        Assert.False(north.Active);
        Assert.Equal(3, north.StepsToWar);
        PoliticalReactions.Complete(state);
        Assert.True(north.Active);
        Assert.Equal(2, north.StepsToWar);
        PoliticalRules.EnemyEntered(state, "dale", Side.Shadow);
        PoliticalRules.SettlementCaptured(state, "dale", Side.FreePeoples, Side.Shadow);
        PoliticalReactions.Complete(state);
        Assert.Equal(1, north.StepsToWar);
        Assert.Equal(2, state.Events.Count(e => e.Kind == "PoliticsAdvanced"));
    }

    [Fact]
    public void ActivationTriggersDoNotAdvanceAndIconsAndSettlementTypesMatter()
    {
        var state = Muster();
        var gondor = state.Nations.Single(n => n.Id == "gondor");
        PoliticalRules.CompanionArrived(state, "minas-tirith", "elves");
        Assert.False(gondor.Active);
        PoliticalRules.CompanionArrived(state, "minas-tirith", "free-peoples");
        PoliticalReactions.Complete(state);
        Assert.True(gondor.Active);
        Assert.Equal(2, gondor.StepsToWar);
        var rohan = state.Nations.Single(n => n.Id == "rohan");
        PoliticalRules.FellowshipDeclared(state, "eastemnet");
        Assert.False(rohan.Active);
        PoliticalRules.EnemyEntered(state, "eastemnet", Side.Shadow);
        PoliticalReactions.Complete(state);
        Assert.True(rohan.Active);
        Assert.Equal(3, rohan.StepsToWar);
    }

    [Theory]
    [InlineData(PoliticalAttack.Ents)]
    [InlineData(PoliticalAttack.DeadMenOfDunharrow)]
    public void NamedCardAttacksHaveTheSamePoliticalTrigger(PoliticalAttack attack)
    {
        var state = Muster(Side.Shadow);
        var sauron = state.Nations.Single(n => n.Id == "sauron");
        PoliticalRules.ArmyAttacked(state, state.Units.Where(u => u.Nation == "sauron").Take(2), attack);
        PoliticalReactions.Complete(state);
        Assert.Equal(0, sauron.StepsToWar);
    }

    [Fact]
    public void FortificationsAreNotRecruitmentSettlements()
    {
        var state = Muster();
        AtWar(state, "rohan");
        var result = Execute(state, CommandKind.Recruit,
            new() { Placements = [Place(Reserve(state, "rohan"), "fords-of-isen")] });
        Assert.False(result.Receipt.Accepted);
        Assert.Equal(DieLocation.Available, result.State.Dice[0].Location);
        var options = new MusterOptions().GetOptions(state, Side.FreePeoples).Single(o => o.Kind == CommandKind.Recruit);
        Assert.DoesNotContain("fords-of-isen", options.Targets);
    }

    [Fact]
    public void TwoNationsCanRecruitWithOneDieAndTheCostIsPaidOnlyOnce()
    {
        var state = Muster();
        AtWar(state, "gondor", "rohan");
        var one = Reserve(state, "gondor");
        var two = Reserve(state, "rohan");
        var result = Execute(state, CommandKind.Recruit, new() { Placements = [Place(one, "pelargir"), Place(two, "edoras")] });
        Assert.True(result.Receipt.Accepted);
        Assert.Equal("pelargir", result.State.Units.Single(u => u.Id == one.Id).Location);
        Assert.Equal("edoras", result.State.Units.Single(u => u.Id == two.Id).Location);
        Assert.Equal(1, result.State.Dice.Count(d => d.Location == DieLocation.Spent));
        Assert.Equal(Side.Shadow, result.State.Pending!.Owner);
        Assert.Equal("reserve", one.Location);
    }

    [Theory]
    [InlineData("same-settlement")]
    [InlineData("same-figure")]
    [InlineData("elite-plus-regular")]
    [InlineData("not-at-war")]
    [InlineData("enemy-control")]
    [InlineData("wrong-nation")]
    [InlineData("not-in-reserve")]
    [InlineData("besieged")]
    [InlineData("missing-region")]
    public void IllegalRecruitmentIsAtomic(string fault)
    {
        var state = Muster();
        AtWar(state, "gondor");
        var one = Reserve(state, "gondor");
        var two = state.Units.First(u => u.Nation == "gondor" && u.Kind == UnitKind.Regular && u.Location == "reserve" && u.Id != one.Id);
        var choices = new List<UnitPlacementChoice> { Place(one, "pelargir"), Place(two, "minas-tirith") };
        switch (fault)
        {
            case "same-settlement":
                choices[1].RegionId = "pelargir";
                break;
            case "same-figure":
                choices[1].UnitId = one.Id;
                break;
            case "elite-plus-regular":
                choices[0].UnitId = Reserve(state, "gondor", UnitKind.Elite).Id;
                break;
            case "not-at-war":
                state.Nations.Single(n => n.Id == "gondor").StepsToWar = 1;
                break;
            case "enemy-control":
                state.Control["pelargir"] = Side.Shadow;
                break;
            case "wrong-nation":
                choices[0].RegionId = "edoras";
                break;
            case "not-in-reserve":
                one.Location = "casualties";
                break;
            case "besieged":
                state.Units.First(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular).Location = "minas-tirith";
                break;
            case "missing-region":
                choices[0].RegionId = null!;
                break;
        }
        var before = StateCodec.Write(state);
        var result = Execute(state, CommandKind.Recruit, new() { Placements = choices });
        Assert.False(result.Receipt.Accepted);
        Assert.Same(state, result.State);
        Assert.Equal(before, StateCodec.Write(result.State));
    }

    [Fact]
    public void NazgulRequireSauronAtWarAndItsStronghold()
    {
        var state = Muster(Side.Shadow);
        var nazgul = Reserve(state, "sauron", UnitKind.Nazgul);
        Assert.False(RecruitmentRules.CanPlaceMuster(state, Side.Shadow, nazgul, "barad-dur"));
        AtWar(state, "sauron");
        Assert.True(RecruitmentRules.CanPlaceMuster(state, Side.Shadow, nazgul, "barad-dur"));
        Assert.False(RecruitmentRules.CanPlaceMuster(state, Side.Shadow, nazgul, "nurn"));
        Assert.False(RecruitmentRules.CanPlaceMuster(state, Side.Shadow, nazgul, "orthanc"));
    }

    [Fact]
    public void OverRecruitingKeepsTheCostResolvingAcrossSaveUntilChosenExcessReturnsToReserve()
    {
        var state = Muster();
        AtWar(state, "gondor");
        var units = state.Units.Where(u => u.Nation == "gondor" && u.Kind == UnitKind.Regular).Take(10).ToList();
        foreach (var unit in state.Units.Where(u => u.Location == "pelargir"))
            unit.Location = "reserve";
        foreach (var unit in units)
            unit.Location = "pelargir";
        var elite = Reserve(state, "gondor", UnitKind.Elite);
        var result = Execute(state, CommandKind.Recruit, new() { Placements = [Place(elite, "pelargir")] });
        Assert.True(result.Receipt.Accepted);
        Assert.Equal(DecisionKind.RemoveExcess, result.State.Pending!.Kind);
        Assert.Equal(DieLocation.Resolving, result.State.Dice[0].Location);
        var reloaded = StateCodec.Read(StateCodec.Write(result.State));
        var finish = Execute(reloaded, CommandKind.RemoveExcess, new() { Ids = [units[0].Id] });
        Assert.True(finish.Receipt.Accepted);
        Assert.Equal("reserve", finish.State.Units.Single(u => u.Id == units[0].Id).Location);
        Assert.Equal("pelargir", finish.State.Units.Single(u => u.Id == elite.Id).Location);
        Assert.Equal(DieLocation.Spent, finish.State.Dice[0].Location);
        Assert.Equal(Side.Shadow, finish.State.Pending!.Owner);
    }

    [Fact]
    public void ALeaderRecruitedWithoutAnArmyIsAnExplicitKnownLoss()
    {
        var state = Muster();
        AtWar(state, "gondor");
        foreach (var unit in state.Units.Where(u => u.Location == "pelargir"))
            unit.Location = "reserve";
        var leader = Reserve(state, "gondor", UnitKind.Leader);
        var command = KernelTests.Command(state, CommandKind.Recruit);
        command.Payload = new() { DieId = state.Dice[0].Id, Placements = [Place(leader, "pelargir")] };
        var preview = new PreviewService(Engine).Preview(state, Side.FreePeoples, command);
        Assert.True(preview.Legal);
        Assert.Equal([leader.Id], preview.RemovedIds);
        var result = Engine.Execute(state, Side.FreePeoples, command, new SequenceRandom());
        Assert.True(result.Receipt.Accepted);
        Assert.Equal("casualties", result.State.Units.Single(u => u.Id == leader.Id).Location);
    }

    [Fact]
    public void CardExceptionsAreTrustedParametersAndNeverRelaxMuster()
    {
        var state = Muster();
        var unit = Reserve(state, "gondor");
        state.Units.First(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular).Location = "minas-tirith";
        var policy = new RecruitmentPolicy(RequireAtWar: false, AllowBesiegedStronghold: true);
        Assert.True(RecruitmentRules.CanPlace(state, Side.FreePeoples, unit, "minas-tirith", policy));
        Assert.False(RecruitmentRules.CanPlaceMuster(state, Side.FreePeoples, unit, "minas-tirith"));
        state.Control["minas-tirith"] = Side.Shadow;
        Assert.False(RecruitmentRules.CanPlace(state, Side.FreePeoples, unit, "minas-tirith", policy));
    }

    [Fact]
    public void OptionsHaveGroupedReservesAndCasualtyPoolsRemainDifferent()
    {
        var state = Muster();
        AtWar(state, "gondor");
        var fp = Reserve(state, "gondor");
        var sh = Reserve(state, "sauron");
        BoardRules.RemoveCasualty(fp);
        BoardRules.RemoveCasualty(sh);
        Assert.Equal("casualties", fp.Location);
        Assert.Equal("reserve", sh.Location);
        var option = new MusterOptions().GetOptions(state, Side.FreePeoples).Single(o => o.Kind == CommandKind.Recruit);
        Assert.DoesNotContain(fp.Id, option.EligibleIds);
        Assert.All(option.Recruitment, r => Assert.Equal("gondor", r.Nation));
        Assert.True(option.Recruitment.Count < option.EligibleIds.Count);
        Assert.Empty(new MusterOptions().GetOptions(state, Side.Shadow));
    }

    private static MatchState Muster(Side side = Side.FreePeoples)
    {
        var state = TurnTests.Actions(actor: side);
        foreach (var die in state.Dice.Where(d => d.Owner == side))
            die.Face = DieFace.Muster;
        return state;
    }
    private static void AtWar(MatchState state, params string[] nations)
    {
        foreach (var nation in state.Nations.Where(n => nations.Contains(n.Id)))
        {
            nation.Active = true;
            nation.StepsToWar = 0;
        }
    }
    private static UnitState Reserve(MatchState state, string nation, UnitKind kind = UnitKind.Regular) =>
        state.Units.First(u => u.Nation == nation && u.Kind == kind && u.Location == "reserve");
    private static UnitPlacementChoice Place(UnitState unit, string region) => new() { UnitId = unit.Id, RegionId = region };
    private static EngineResult Execute(MatchState state, CommandKind kind, CommandPayload payload)
    {
        payload.DieId = state.Dice.First(d => d.Owner == state.Pending!.Owner).Id;
        var command = KernelTests.Command(state, kind);
        command.Payload = payload;
        return Engine.Execute(state, state.Pending!.Owner, command, new SequenceRandom());
    }
}
