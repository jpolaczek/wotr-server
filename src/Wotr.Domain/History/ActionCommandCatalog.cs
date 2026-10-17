using Wotr.Contracts;

namespace Wotr.Domain;

// Deliberately exhaustive: adding a command requires an explicit history classification.
public static class ActionCommandCatalog
{
    public static ActionContextKind Context(CommandKind kind) => kind switch
    {
        CommandKind.ContinueFellowship or CommandKind.DeclareFellowship or CommandKind.RevealFellowship or
            CommandKind.HideFellowship or CommandKind.SelectGuide or CommandKind.SeparateCompanions or
            CommandKind.EnterMordor => ActionContextKind.Fellowship,
        CommandKind.MoveFellowship or CommandKind.RerollHunt or CommandKind.ResolveHuntDamage or
            CommandKind.AllocateHunt => ActionContextKind.Hunt,
        CommandKind.MoveArmies or CommandKind.Recruit or CommandKind.RemoveExcess or
            CommandKind.TakeEventLosses or CommandKind.FinishArmyEvent => ActionContextKind.Army,
        CommandKind.MoveCompanions or CommandKind.MoveShadowCharacters or CommandKind.EnterCharacter or
            CommandKind.UseCharacterAbility or CommandKind.ChooseCompanionVersions or CommandKind.ChooseCouncilHomelands or
            CommandKind.AwakenSovereign or CommandKind.RemoveCorruptedSovereign or CommandKind.AttemptCorruption or
            CommandKind.DrawCorruptionTile or CommandKind.ChooseCorruptionTile => ActionContextKind.Characters,
        CommandKind.AdvancePolitics or CommandKind.UseNarya => ActionContextKind.Politics,
        CommandKind.DiscardCards or CommandKind.DrawCard or CommandKind.PlayEvent or CommandKind.ChooseEffect or
            CommandKind.RespondEffect or CommandKind.DeclineReaction or CommandKind.DiscardProtection or
            CommandKind.DiscardPalantir or CommandKind.DiscardDenethor or CommandKind.DiscardLore or
            CommandKind.DiscardGates or CommandKind.DiscardWelcome or CommandKind.DiscardBaradPalantir => ActionContextKind.Cards,
        CommandKind.AttackArmy or CommandKind.CommitCombatCard or CommandKind.RerollBattle or CommandKind.TakeBattleLosses or
            CommandKind.ContinueBattle or CommandKind.StopBattle or CommandKind.RetreatBattle or CommandKind.HoldBattle or
            CommandKind.AdvanceBattle or CommandKind.FightFieldBattle or CommandKind.RetreatIntoSiege or CommandKind.ExtendSiege or
            CommandKind.UseEvilKnowledge or CommandKind.SetCombatCardPolicy => ActionContextKind.Battle,
        CommandKind.DiscardDie or CommandKind.ChooseSpecialDie or CommandKind.DiscardRulerDie or CommandKind.UseDismay or
            CommandKind.DeclineDismay or CommandKind.UseLoreBeforeActions => ActionContextKind.Dice,
        CommandKind.UseRing or CommandKind.UseVilya or CommandKind.DeclineVilya or
            CommandKind.ChooseCouncilToken or CommandKind.UseCouncilToken => ActionContextKind.Resources,
        CommandKind.Pass or CommandKind.FinishActionSequence => ActionContextKind.General,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Command has no history classification.")
    };

    public static bool PrivateSelection(CommandKind kind) => kind is CommandKind.ChooseCompanionVersions or
        CommandKind.DrawCorruptionTile or CommandKind.ChooseCorruptionTile or CommandKind.RerollBattle;

    public static bool StartsAction(MatchState before) => before.Pending?.Kind is DecisionKind.Action or
        DecisionKind.FellowshipPhase or DecisionKind.AllocateHunt or DecisionKind.CouncilVersions or
        DecisionKind.CouncilHomelands or DecisionKind.CouncilToken or DecisionKind.SpecialDieChoice or
        DecisionKind.RulerDiscard or DecisionKind.Dismay;
}
