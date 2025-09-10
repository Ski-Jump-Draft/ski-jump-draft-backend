using App.Application.CompetitionEngine;
using App.Util;
using App.Domain.Competition;
using App.Domain.Competition.Results;
using App.Domain.Competition.Rules;
using App.Domain.Shared;
using App.Plugin.Competitions.AdvancementTieBreaker;
using App.Plugin.Competitions.GatePointsGrantor;
using App.Plugin.Competitions.JudgesPointsAggregator;
using App.Plugin.Competitions.JumpResultCreator;
using App.Plugin.Competitions.NextRoundStartDecider;
using App.Plugin.Competitions.Scorer.Classic;
using App.Plugin.Competitions.StartlistProvider.AdvancementByLimitDecider;
using App.Plugin.Competitions.WindPointsGrantor;
using IndividualParticipantModule = App.Domain.Competition.IndividualParticipantModule;
using ParticipantResultModule = App.Domain.Competition.Results.ParticipantResultModule;

namespace App.Plugin.Engine.Classic;

public class Factory(
    Func<Hill, double> getGatePoints,
    Func<Hill, double> getHeadwindPoints,
    Func<Hill, double> getTailwindPoints,
    IGuid guid) : ICompetitionEngineFactory
{
    public Domain.Competition.Engine.IEngine Create(CreationContext context)
    {
        // TODO: Uniemożliwić wymaganie klucza niepodanego w RequiredOptions

        var engineId = Domain.Competition.Engine.Id.NewId(guid.NewGuid());
        var rawOptions = context.RawOptions;

        var enableGatePoints = rawOptions["EnableGatePoints"] is bool;
        var enableWindPoints = rawOptions["EnableWindPoints"] is bool;
        var enableStylePoints = rawOptions["EnableStylePoints"] is bool;

        // TODO: Error handling dla tych dwóch poniżej.
        var categoryString = (string)rawOptions["Category"];
        var category = ComeptitionCategory.tryParse(categoryString).Value;

        var roundLimitsRaw = (List<string>)rawOptions["RoundLimits"];
        var roundLimits = roundLimitsRaw.Select((rawLimit, _) =>
        {
            var parts = rawLimit.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var kind = parts[0];

            return kind switch
            {
                "None" => (RoundParticipantsLimit)new RoundParticipantsLimit.None(),
                "Soft" when parts.Length == 2 && int.TryParse(parts[1], out var softLimit)
                    => new RoundParticipantsLimit.Soft(softLimit),
                "Exact" when parts.Length == 2 && int.TryParse(parts[1], out var exactLimit)
                    => throw new NotImplementedException(), // TODO: implement when ready
                _ => throw new InvalidOperationException($"Invalid limit: {rawLimit}")
            };
        }).ToList();

        var options = new Classic.Options(enableGatePoints, enableWindPoints, enableStylePoints,
            PointsPerGate(context.Hill),
            PointsPerHeadwind(context.Hill),
            PointsPerTailwind(context.Hill), roundLimits, category);

        var jumpScorer = BuildScorer(options);
        var jumpResultCreator = new DynamicGuid(guid);
        var nextRoundStartDecider = new ExactRoundsLimit(options.RoundLimits.Count - 1);
        var rankedResultsCreator =
            new Competitions.RankedResultsCreator.Default(RankedResults.ExAequoPolicy.AddMoreThanOne);

        // Na wypadek systemu KO (limitu Exact)
        var advancementTieBreaker = new ByBib(startlistEntityGuid => 1, howMany: 1, lowestFirst: false);
        var advancementByLimitDecider =
            new Default(advancementTieBreaker);

        var deferredResults = new DeferredProvider<ResultsModule.Results>();
        var deferredRoundIndex = new DeferredProvider<Phase.RoundIndex>();

        var startlistProvider = new Competitions.StartlistProvider.Classic(roundLimits, advancementByLimitDecider,
            category, deferredResults.Provide, deferredRoundIndex.Provide, rankedResultsCreator,
            MapParticipantResultIdToIndividualParticipantId);
        var teamIdByIndividualId = new Dictionary<Guid, Guid>(); // TODO

        if (context.RandomSeed is not ulong randomSeed)
        {
            throw new InvalidOperationException("Random seed must be a ulong but got: " + context.RandomSeed + "");
        }

        var engine = new ClassicEngine(options, jumpScorer, jumpResultCreator, nextRoundStartDecider,
            startlistProvider, teamIdByIndividualId, context.Hill,
            id: engineId, randomSeed);

        deferredResults.Set(() => engine.GenerateResults());

        deferredRoundIndex.Set(() =>
        {
            if (engine.Phase.IsEnded)
            {
                throw new InvalidOperationException("Engine already ended");
            }

            if (engine.Phase.IsRunning)
            {
                return ((Domain.Competition.Engine.Phase.Running)engine.Phase).RoundIndex;
            }

            if (engine.Phase.IsWaitingForNextRound)
            {
                var nextRoundIndexUint = ((Domain.Competition.Engine.Phase.WaitingForNextRound)engine.Phase)
                    .NextRoundIndex.Item;
                var currentRoundIndexUint = nextRoundIndexUint - 1u;
                return Phase.RoundIndex.NewRoundIndex(currentRoundIndexUint);
            }

            return Phase.RoundIndex.NewRoundIndex(0);
        });

        return engine;

        IEnumerable<IndividualParticipantModule.Id> MapParticipantResultIdToIndividualParticipantId(
            ParticipantResultModule.Id participantResultId)
        {
            var participantResult = 
        }
    }

    private static Abstractions.IJumpScorer BuildScorer(Options competitionOptions)
    {
        Abstractions.IWindPointsGrantor wind = competitionOptions.WindPointsEnabled
            ? new ClassicWindPointsGrantor(competitionOptions.HeadwindPoints!.Value,
                competitionOptions.TailwindPoints!.Value)
            : new NoWindPointsGrantor();

        Abstractions.IGatePointsGrantor gate = competitionOptions.GatePointsEnabled
            ? new ClassicGatePointsGrantor(competitionOptions.PointsPerGate!.Value,
                CoachGatePointsPolicy.RequireHsPercent, 0.95)
            : new NoGatePointsGrantor();

        Abstractions.IStylePointsAggregator style = competitionOptions.StylePointsEnabled
            ? new DropHighAndLowMarks()
            : new NoPoints();

        return new ClassicJumpScorer(wind, gate, style);
    }

    private double PointsPerGate(Hill hill)
    {
        // TODO: Przenieść w lepsze miejsce, może helper poza Classic Competitions Plugin
        return getGatePoints(hill);
    }

    private double PointsPerHeadwind(Hill hill)
    {
        return getHeadwindPoints(hill);
    }

    private double PointsPerTailwind(Hill hill)
    {
        return getTailwindPoints(hill);
    }

    public IEnumerable<Option> RequiredOptions =>
        new List<Option>()
        {
            new Option("Category", OptionType.String),
            new Option("EnableGatePoints", OptionType.Boolean), new Option("EnableWindPoints", OptionType.Boolean),
            new Option("EnableStylePoints", OptionType.Boolean),
            new Option("RoundLimits", OptionType.ListOfStrings)
        };
}