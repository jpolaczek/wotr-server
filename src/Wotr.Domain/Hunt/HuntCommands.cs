using Wotr.Contracts;

namespace Wotr.Domain;

public sealed class HuntCommands(EffectCatalog? catalog = null) : ICommandHandler, IResolutionContinuation
{
    private readonly HuntRuntime runtime = new(catalog ?? EffectCatalog.All);
    public bool Handles(CommandKind kind) => kind is CommandKind.MoveFellowship or CommandKind.RerollHunt or CommandKind.ResolveHuntDamage;

    public static bool CanMove(MatchState state, Side actor) => actor == Side.FreePeoples && state.Phase == GamePhase.ActionResolution &&
        state.Pending?.Kind == DecisionKind.Action && state.Fellowship.Hidden;

    public string? Validate(MatchState state, Side actor, GameCommand command)
    {
        var hunt = HuntRuntime.Current(state)?.Hunt;
        var payload = command.Payload;
        var legal = command.Kind switch
        {
            CommandKind.MoveFellowship => CanMove(state, actor) && KingsFarSighted.Valid(state, payload) &&
                DiceRules.AvailableDie(state, actor, payload.DieId) is { } die && DiceRules.CanPay(die, DieFace.Character),
            CommandKind.RerollHunt => actor == Side.Shadow && hunt is not null && state.Pending?.Kind == DecisionKind.HuntReroll &&
                DraftSelection.IsSubset(payload.Ids, HuntRoll.Failed(hunt), 0, hunt.RerollSources.Count),
            CommandKind.ResolveHuntDamage => actor == Side.FreePeoples && hunt is { Stage: HuntStage.Damage, CasualtyTaken: false, Damage: > 0 } &&
                state.Pending?.Kind == DecisionKind.HuntDamage && Enum.IsDefined(payload.HuntResponse) &&
                (payload.HuntResponse == HuntResponse.Ring || state.Fellowship.Companions.Count > 0) && payload.Ids.Count == 0,
            _ => false
        };
        return legal ? null : "IllegalChoice";
    }

    public void Apply(MatchState state, Side actor, GameCommand command, IRandomSource random)
    {
        switch (command.Kind)
        {
            case CommandKind.MoveFellowship:
                DiceRules.Pay(state, actor, command.Payload.DieId, DieFace.Character);
                CouncilStrider.RecordMove(state, command.Payload.DieId);
                HuntRuntime.BeginMove(state, random, diceReduction: KingsFarSighted.Bonus(command.Payload));
                break;
            case CommandKind.RerollHunt:
                HuntRoll.Reroll(state, HuntRuntime.Current(state)!.Hunt!, command.Payload.Ids, random);
                break;
            case CommandKind.ResolveHuntDamage:
                HuntDamage.Resolve(state, HuntRuntime.Current(state)!.Hunt!, command.Payload.HuntResponse, random);
                break;
        }
    }

    public bool CanContinue(MatchState state) => state.Pending is null &&
        (HuntRuntime.Current(state) is not null || FellowshipFlow.Current(state)?.Fellowship is { AwaitingHunts: true });
    public void Continue(MatchState state, IRandomSource random) => runtime.Continue(state, random);
}
