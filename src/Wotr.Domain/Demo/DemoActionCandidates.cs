using Wotr.Contracts;

namespace Wotr.Domain;

// DEMO-02: the CPU's action. Kinds of action come in a weighted random order: the Free Peoples mostly march the
// Fellowship, the Shadow mostly attacks; drawing, mustering, politics, characters and events fill in. When nothing
// fits its dice, the CPU sets a die aside or passes.
internal static class DemoActionCandidates
{
    private static readonly (CommandKind, int)[] FreePeoples =
    [
        (CommandKind.EnterMordor, 40), (CommandKind.HideFellowship, 6), (CommandKind.MoveFellowship, 8),
        (CommandKind.EnterCharacter, 4), (CommandKind.DrawCard, 4), (CommandKind.AdvancePolitics, 3),
        (CommandKind.Recruit, 3), (CommandKind.PlayEvent, 2), (CommandKind.AttackArmy, 2), (CommandKind.MoveArmies, 1)
    ];

    private static readonly (CommandKind, int)[] Shadow =
    [
        (CommandKind.AttackArmy, 8), (CommandKind.MoveArmies, 4), (CommandKind.Recruit, 4), (CommandKind.EnterCharacter, 4),
        (CommandKind.AdvancePolitics, 3), (CommandKind.DrawCard, 3), (CommandKind.PlayEvent, 2), (CommandKind.MoveShadowCharacters, 1)
    ];

    public static IEnumerable<(CommandKind, CommandPayload)> For(DemoContext context)
    {
        var weights = context.Side == Side.FreePeoples ? FreePeoples : Shadow;
        foreach (var kind in context.Choice.Weighted(weights))
            foreach (var candidate in Candidates(context, kind))
                yield return candidate;
        foreach (var candidate in Skip(context))
            yield return candidate;
    }

    // Moves that a card's army or character event asks for, then its optional end.
    public static IEnumerable<(CommandKind, CommandPayload)> EventMoves(DemoContext context) =>
        Marches(context).Concat(Attacks(context)).Concat(Moves(context)).Concat(CharacterMoves(context))
            .Concat(DemoCandidates.Plain(CommandKind.FinishArmyEvent));

    // A card may move several armies at once and require every one that can: as many as allowed, each army with a
    // random target of its own; fewer armies afterwards.
    private static IEnumerable<(CommandKind, CommandPayload)> Marches(DemoContext context)
    {
        const int attempts = 8;
        foreach (var option in context.Options(CommandKind.MoveArmies).Where(o => o.ArmyEvent is not null && o.Maximum > 1))
            for (var count = Math.Min(option.Maximum, option.Armies.Count); count > 1; count--)
                for (var attempt = 0; attempt < attempts; attempt++)
                    yield return (CommandKind.MoveArmies, new CommandPayload
                    {
                        Moves = context.Choice.Shuffled(option.Armies).Take(count).Select(army => March(context, army)).ToList()
                    });
    }

    private static ArmyMoveChoice March(DemoContext context, ArmyOption army)
    {
        var target = context.Choice.Shuffled(army.Targets)[0];
        return new()
        {
            From = army.RegionId, To = target.RegionId, Route = [.. target.Route],
            UnitIds = army.UnitIds.Where(target.EligibleIds.Contains).ToList(),
            CharacterIds = army.CharacterIds.Where(target.EligibleIds.Contains).ToList()
        };
    }

    private static IEnumerable<(CommandKind, CommandPayload)> Candidates(DemoContext context, CommandKind kind) => kind switch
    {
        CommandKind.EnterMordor => context.Options(kind).Select(_ => (kind, new CommandPayload())),
        CommandKind.HideFellowship or CommandKind.MoveFellowship => DiceOnly(context, kind),
        CommandKind.DrawCard => Draws(context),
        CommandKind.AdvancePolitics => Politics(context),
        CommandKind.Recruit => Recruits(context),
        CommandKind.EnterCharacter => Entries(context),
        CommandKind.PlayEvent => Events(context),
        CommandKind.AttackArmy => Attacks(context),
        CommandKind.MoveArmies => Moves(context),
        CommandKind.MoveShadowCharacters => CharacterMoves(context),
        _ => []
    };

    private static IEnumerable<(CommandKind, CommandPayload)> DiceOnly(DemoContext context, CommandKind kind) =>
        context.Options(kind).SelectMany(o => o.EligibleDice).Select(die => (kind, new CommandPayload { DieId = die }));

    private static IEnumerable<(CommandKind, CommandPayload)> Draws(DemoContext context) =>
        from option in context.Options(CommandKind.DrawCard)
        from deck in context.Choice.Shuffled(option.Targets)
        from cost in option.DrawCosts
        select (CommandKind.DrawCard, new CommandPayload { DieId = cost.DieId, AbilityId = cost.AbilityId, Deck = deck });

    private static IEnumerable<(CommandKind, CommandPayload)> Politics(DemoContext context) =>
        from option in context.Options(CommandKind.AdvancePolitics)
        from nation in context.Choice.Shuffled(option.Targets)
        from die in option.EligibleDice
        select (CommandKind.AdvancePolitics, new CommandPayload { DieId = die, NationId = nation });

    private static IEnumerable<(CommandKind, CommandPayload)> Recruits(DemoContext context) =>
        from option in context.Options(CommandKind.Recruit)
        from reserve in context.Choice.Shuffled(option.Recruitment)
        from region in context.Choice.Shuffled(reserve.Targets)
        from die in option.EligibleDice
        select (CommandKind.Recruit, new CommandPayload
        {
            DieId = die,
            Placements = [new() { UnitId = reserve.ReserveIds[0], RegionId = region }]
        });

    private static IEnumerable<(CommandKind, CommandPayload)> Entries(DemoContext context) =>
        from option in context.Options(CommandKind.EnterCharacter)
        from entry in context.Choice.Shuffled(option.CharacterActions)
        from region in entry.Targets
        from die in entry.Dice
        select (CommandKind.EnterCharacter, new CommandPayload { DieId = die, CharacterId = entry.CharacterId, RegionId = region });

    private static IEnumerable<(CommandKind, CommandPayload)> Events(DemoContext context) =>
        from option in context.Options(CommandKind.PlayEvent)
        from play in context.Choice.Shuffled(option.Events)
        from cost in play.Costs
        select (CommandKind.PlayEvent, new CommandPayload
        {
            CardId = play.CardId, DieId = cost.DieId, CardCost = cost.Face, AbilityId = cost.AbilityId,
            NationId = play.NationTargets.FirstOrDefault() ?? ""
        });

    // The whole army attacks; a variant leaves one army unit behind, for a rearguard the rules may require.
    private static IEnumerable<(CommandKind, CommandPayload)> Attacks(DemoContext context) =>
        from option in context.Options(CommandKind.AttackArmy)
        from army in context.Choice.Shuffled(option.Armies)
        from target in context.Choice.Shuffled(army.Targets)
        from cost in Costs(option)
        from units in new[] { army.UnitIds, army.UnitIds.Skip(1).ToList() }
        select (CommandKind.AttackArmy, Movement(cost, new()
        {
            From = army.RegionId, To = target.RegionId, Route = [.. target.Route], UnitIds = units, CharacterIds = army.CharacterIds
        }));

    private static IEnumerable<(CommandKind, CommandPayload)> Moves(DemoContext context) =>
        from option in context.Options(CommandKind.MoveArmies)
        from army in context.Choice.Shuffled(option.Armies)
        from target in context.Choice.Shuffled(army.Targets)
        from cost in Costs(option)
        from payload in Routed(cost, new()
        {
            From = army.RegionId, To = target.RegionId, Route = [.. target.Route],
            UnitIds = army.UnitIds.Where(target.EligibleIds.Contains).ToList(),
            CharacterIds = army.CharacterIds.Where(target.EligibleIds.Contains).ToList()
        })
        select (CommandKind.MoveArmies, payload);

    // A card's march carries its route either on the move or, for one army, on the whole command.
    private static IEnumerable<CommandPayload> Routed(MovementCostOption cost, ArmyMoveChoice move)
    {
        yield return Movement(cost, move);
        if (move.Route.Count == 0)
            yield break;
        var payload = Movement(cost, new() { From = move.From, To = move.To, UnitIds = move.UnitIds, CharacterIds = move.CharacterIds });
        payload.Route = [.. move.Route];
        yield return payload;
    }

    private static IEnumerable<(CommandKind, CommandPayload)> CharacterMoves(DemoContext context) =>
        from option in context.Options(CommandKind.MoveShadowCharacters)
        from travel in context.Choice.Shuffled(option.Travel)
        from target in context.Choice.Shuffled(travel.Targets)
        let route = DemoRoutes.Path(travel.Origin, target, Math.Max(travel.Range, 1))
        where route is not null
        from die in option.EligibleDice.DefaultIfEmpty("")
        select (CommandKind.MoveShadowCharacters, new CommandPayload
        {
            DieId = die, CompanionMoves = [new() { Ids = [travel.Id], Route = route }]
        });

    // An event's movement may have no die to pay: it then moves without a cost.
    private static IEnumerable<MovementCostOption> Costs(ActionOption option) =>
        option.MovementCosts.Count > 0 ? option.MovementCosts : [new MovementCostOption { Face = DieFace.Army }];

    private static CommandPayload Movement(MovementCostOption cost, ArmyMoveChoice move) => new()
    {
        DieId = cost.DieId, MovementCost = cost.Face, AbilityId = cost.AbilityId, Moves = [move]
    };

    private static IEnumerable<(CommandKind, CommandPayload)> Skip(DemoContext context) =>
        DiceOnly(context, CommandKind.DiscardDie).Concat(DemoCandidates.Plain(CommandKind.Pass));
}
