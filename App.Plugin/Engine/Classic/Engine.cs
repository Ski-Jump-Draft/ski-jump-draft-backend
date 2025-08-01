using System.Text.Json;
using App.Domain.Competition;
using App.Domain.Competition.Jump;
using App.Domain.Competition.Results;
using App.Domain.Competition.Rules;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using App.Domain;
using App.Infrastructure.Utility.Rng;
using static App.Domain.Competition.Phase;
using static Microsoft.FSharp.Collections.ListModule;
using Abstractions = App.Domain.Competition.Results.Abstractions;
using IndividualParticipantModule = App.Domain.Competition.IndividualParticipantModule;
using ResultsModule = App.Domain.Competition.ResultsModule;
using Competition = App.Domain.Competition;
using ParticipantResultModule = App.Domain.Competition.Results.ParticipantResultModule;

namespace App.Plugin.Engine.Classic;

// TODO: Może participant result creator ??????????//

public sealed class ClassicEngine : Competition.Engine.IEngine
{
    private readonly Options _options;

    private readonly Abstractions.IJumpScorer _jumpScorer;
    private readonly Abstractions.IJumpResultCreator _jumpResultCreator;
    private readonly Advancement.INextRoundStartDecider _nextRoundStartDecider;
    private readonly IStartlistProvider _startlistProvider;
    private readonly Dictionary<Guid, Guid>? _teamIdByIndividualId;
    private readonly Hill _hill;
    private readonly ulong _seed;

    private ClassicState _state;
    private Competition.Engine.IRng<ulong> _rng;

    public ClassicEngine(Options options, Abstractions.IJumpScorer jumpScorer,
        Abstractions.IJumpResultCreator jumpResultCreator, Advancement.INextRoundStartDecider nextRoundStartDecider,
        IStartlistProvider startlistProvider,
        Dictionary<Guid, Guid>? teamIdByIndividualId, Hill hill, Competition.Engine.Id id, ulong seed)
    {
        _options = options;
        _jumpScorer = jumpScorer;
        _teamIdByIndividualId = teamIdByIndividualId;
        _jumpResultCreator = jumpResultCreator;
        _hill = hill;
        Id = id;
        _seed = seed;
        _rng = new XorShift64Star(seed);
        _nextRoundStartDecider = nextRoundStartDecider;
        _startlistProvider = startlistProvider;
        _state = ClassicState.CreateInitial();
        _state.RoundResults.Add(new RoundResults { Index = 0 });
    }

    public Competition.Engine.Id Id { get; }

    public CompetitionCategory Category => _options.Category;

    public bool ShouldEndCompetition
    {
        get
        {
            var isLastRound = _state.CurrentRoundIndex + 1 >= _options.RoundLimits.Count;
            var noMoreJumpers =
                !FSharpOption<IndividualParticipantModule.Id>.get_IsSome(_state.Startlist?.NextParticipant);
            return isLastRound && noMoreJumpers;
        }
    }

    public Competition.Engine.Phase Phase => _state.Phase;

    public Tuple<Competition.Engine.Snapshot, FSharpList<Competition.Engine.Event>> RegisterJump(Jump jump)
    {
        if (!jump.Hill.Equals(_hill))
        {
            throw new InvalidOperationException("ClassicEngine requires same hill for every jump");
        }

        var individualParticipantId = jump.IndividualParticipantId;
        if (_state.Phase.IsEnded)
            throw new InvalidOperationException("Competition already ended");

        var round = _state.CurrentRound;
        if (round.Contains(individualParticipantId))
            throw new InvalidOperationException("Jump already registered");

        var jumpScore = _jumpScorer.Evaluate(jump);
        var jumpResult = _jumpResultCreator.Create(jumpScore, this.CurrentRoundIndex, jump.IndividualParticipantId);

        round.RegisterJump(individualParticipantId, jumpResult);

        var events = new List<Competition.Engine.Event>()
        {
            Competition.Engine.Event.NewJumpRegistered(
                individualParticipantId,
                jumpResult.Id
            )
        };

        return new Tuple<Competition.Engine.Snapshot, FSharpList<Competition.Engine.Event>>(ToSnapshot(),
            OfSeq(events));
    }

    private FSharpOption<Competition.Engine.Snapshot> SetUpNextRound()
    {
        if (!_state.Phase.IsWaitingForNextRound)
            throw new InvalidOperationException("Engine must be in WaitingForNextRound state to set up next round");

        var nextRoundIndex = _state.CurrentRoundIndex + 1;
        _state = _state with
        {
            Startlist = _state.Startlist,
            CurrentRoundIndex = nextRoundIndex,
            RoundResults = _state.RoundResults
                .Append(new RoundResults { Index = nextRoundIndex })
                .ToList()
        };
        return ToSnapshot();
    }

    private FSharpOption<Competition.Engine.Snapshot> EndRound()
    {
        if (!_state.Phase.IsRunning)
            throw new InvalidOperationException("Engine must be in Running state to end the round");

        if (ShouldEndCompetition)
        {
            _state = _state with { Phase = Competition.Engine.Phase.Ended };
            return FSharpOption<Competition.Engine.Snapshot>.Some(ToSnapshot());
        }

        var nextRoundStartDecision = _nextRoundStartDecider.Decide(CurrentRoundIndex);

        return nextRoundStartDecision.StartNextRound
            ? SetUpNextRound()
            : FSharpOption<Competition.Engine.Snapshot>.None;
    }

    private Subround Subround =>
        Subround.NewSubround(
            RoundIndex.NewRoundIndex((uint)_state.CurrentRoundIndex),
            SubroundIndex.NewSubroundIndex(0u));

    private RoundIndex CurrentRoundIndex =>
        RoundIndex.NewRoundIndex((uint)_state.CurrentRoundIndex);

    private RoundParticipantsLimit GetLimitForRound(int roundIndex)
    {
        if (roundIndex < 0 || roundIndex >= _options.RoundLimits.Count)
            throw new InvalidOperationException("No round limit for this round");

        return _options.RoundLimits[roundIndex];
    }

    public ResultsModule.Results GenerateResults()
    {
        var creationResult = ResultsModule.Results.FromState(State());
        if (creationResult.IsError)
        {
            throw new InvalidOperationException("Failed to create results");
        }

        return creationResult.ResultValue;

        FSharpList<ParticipantResult> State()
        {
            if (Category.IsIndividual && _teamIdByIndividualId is not null)
                throw new InvalidOperationException("Individual competition should not define teamIdByIndividualId.");

            var allJumpResults = _state.RoundResults
                .SelectMany(round =>
                    round.JumpResultsByRound.Select(kvp =>
                        new { ParticipantId = kvp.Key, JumpResult = kvp.Value }))
                .ToList();

            var jumpResults = allJumpResults.Select(r => r.JumpResult).ToList();

            if (Category.IsIndividual)
            {
                if (_teamIdByIndividualId is not null)
                    throw new InvalidOperationException(
                        "Individual competition should not define teamIdByIndividualId.");

                return OfSeq(BuildIndividualResults(jumpResults));
            }

            if (Category.IsTeam)
            {
                if (_teamIdByIndividualId is null)
                    throw new InvalidOperationException("Team competition requires teamIdByIndividualId mapping.");

                return OfSeq(BuildTeamResults(jumpResults));
            }

            if (Category.IsMixed)
            {
                if (_teamIdByIndividualId is null)
                    throw new InvalidOperationException("Mixed competition requires teamIdByIndividualId mapping.");

                var individual = BuildIndividualResults(jumpResults);
                var team = BuildTeamResults(jumpResults);

                return OfSeq(individual.Concat(team));
            }

            throw new ArgumentOutOfRangeException();
        }
    }

    public Startlist GenerateStartlist()
    {
        if (_state.Startlist is null)
        {
            throw new InvalidOperationException("Startlist is not generated yet");
        }

        return _state.Startlist;
    }

    private IEnumerable<ParticipantResult> BuildIndividualResults(IEnumerable<JumpResult> jumpResults)
    {
        return jumpResults
            .GroupBy(jr => jr.IndividualParticipantId.Item)
            .Select(group =>
            {
                var ordered = group.OrderBy(jr => jr.RoundIndex.Item).ToList();
                var individualId = IndividualParticipantModule.Id.NewId(group.Key);

                var cumulative = 0.0;
                var roundPoints = new SortedDictionary<RoundIndex, ParticipantResultModule.TotalPoints>();

                foreach (var byRound in ordered.GroupBy(jr => jr.RoundIndex).OrderBy(g => g.Key.Item))
                {
                    cumulative += byRound.Sum(jr => jr.Score.Points.Item);
                    roundPoints[byRound.Key] = ParticipantResultModule.TotalPoints.NewTotalPoints(cumulative);
                }

                var individualResult = new IndividualResult(individualId, ListModule.OfSeq(ordered));

                return new ParticipantResult(
                    ParticipantResultModule.Id.NewId(group.Key),
                    MapModule.OfSeq(roundPoints.Select(kv => Tuple.Create(kv.Key, kv.Value))),
                    ParticipantResultModule.Details.NewIndividualResultDetails(individualResult)
                );
            });
    }

    private IEnumerable<ParticipantResult> BuildTeamResults(IEnumerable<JumpResult> jumpResults)
    {
        if (_teamIdByIndividualId is null)
            throw new InvalidOperationException("Team mapping is required");

        var groupedByIndividual = jumpResults
            .GroupBy(jr => jr.IndividualParticipantId.Item)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(jr => jr.RoundIndex.Item).ToList()
            );

        var groupedByTeam = groupedByIndividual
            .GroupBy(kv => _teamIdByIndividualId[kv.Key]);

        foreach (var teamGroup in groupedByTeam)
        {
            var teamId = TeamParticipantModule.Id.NewId(teamGroup.Key);

            var memberResults = teamGroup.Select(kvp =>
            {
                var individualId = IndividualParticipantModule.Id.NewId(kvp.Key);
                return new IndividualResult(individualId, ListModule.OfSeq(kvp.Value));
            }).ToList();

            var teamResult = new TeamResult(teamId, ListModule.OfSeq(memberResults));

            var cumulative = 0.0;
            var roundPoints = new SortedDictionary<RoundIndex, ParticipantResultModule.TotalPoints>();

            foreach (var byRound in memberResults
                         .SelectMany(ir => ir.JumpResults)
                         .GroupBy(jr => jr.RoundIndex)
                         .OrderBy(g => g.Key.Item))
            {
                cumulative += byRound.Sum(jr => jr.Score.Points.Item);
                roundPoints[byRound.Key] = ParticipantResultModule.TotalPoints.NewTotalPoints(cumulative);
            }

            yield return new ParticipantResult(
                ParticipantResultModule.Id.NewId(teamGroup.Key),
                MapModule.OfSeq(roundPoints.Select(kv => Tuple.Create(kv.Key, kv.Value))),
                ParticipantResultModule.Details.NewTeamResultDetails(teamResult)
            );
        }
    }

    public Competition.Engine.Snapshot ToSnapshot()
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(_state);
        return Competition.Engine.Snapshot.NewSnapshot(bytes);
    }

    public void LoadSnapshot(Competition.Engine.Snapshot blob)
    {
        var bytes = blob.Item;
        _state = JsonSerializer.Deserialize<ClassicState>(bytes)
                 ?? throw new InvalidOperationException("Corrupted snapshot");
        _rng = new XorShift64Star(_state.RngState);
    }
}

internal sealed record RoundResults
{
    public int Index { get; init; }

    public Dictionary<IndividualParticipantModule.Id, JumpResult> JumpResultsByRound { get; init; } =
        new();

    public void RegisterJump(IndividualParticipantModule.Id individualParticipantId, JumpResult score)
    {
        JumpResultsByRound[individualParticipantId] = score;
    }

    public bool Contains(IndividualParticipantModule.Id individualParticipantId) =>
        JumpResultsByRound.ContainsKey(individualParticipantId);
}

internal sealed record ClassicState
{
    public ulong RngState;
    public int CurrentRoundIndex { get; init; }
    public List<RoundResults> RoundResults { get; init; } = new();
    public Competition.Engine.Phase Phase { get; init; } = Competition.Engine.Phase.Ended;
    public Startlist? Startlist { get; init; }

    public double? GetIndividualPointsByRound(IndividualParticipantModule.Id individualParticipantId, int roundIndex)
    {
        if (roundIndex < 0 || roundIndex >= RoundResults.Count) return null;
        var roundResults = RoundResults[roundIndex];
        roundResults.JumpResultsByRound.TryGetValue(individualParticipantId, out var result);
        return result?.Score.Points.Item;
    }

    public double GetIndividualTotalPoints(IndividualParticipantModule.Id individualParticipantId) =>
        RoundResults
            .Select((_, roundIndex) => GetIndividualPointsByRound(individualParticipantId, roundIndex) ?? 0)
            .Sum();

    public static ClassicState CreateInitial() =>
        new() { CurrentRoundIndex = 0, RoundResults = [], Startlist = null };

    public RoundResults CurrentRound => RoundResults.FirstOrDefault(r => r.Index == CurrentRoundIndex)
                                        ?? throw new InvalidOperationException("No current round");
}