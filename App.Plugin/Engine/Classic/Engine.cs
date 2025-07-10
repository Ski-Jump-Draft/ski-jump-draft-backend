using System.Text.Json;
using App.Domain.Competition;
using App.Domain.Competition.Jump;
using App.Domain.Competition.Results.ResultObjects;
using App.Domain.Competition.Rules;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using App.Domain;
using static App.Domain.Competition.Phase;
using static Microsoft.FSharp.Collections.ListModule;
using Abstractions = App.Domain.Competition.Results.Abstractions;
using IndividualParticipantModule = App.Domain.Competition.IndividualParticipantModule;
using ResultsModule = App.Domain.Competition.ResultsModule;
using Competition = App.Domain.Competition;
using ParticipantResultModule = App.Domain.Competition.Results.ResultObjects.ParticipantResultModule;

namespace App.Plugin.Engine.Classic;

// TODO: Może participant result creator ??????????//

public sealed class ClassicEngine : Competition.Engine.IEngine
{
    private readonly Options _options;

    //private readonly ClassicConfig _config;
    private readonly Abstractions.IJumpScorer _jumpScorer;
    private readonly Abstractions.IJumpResultCreator _jumpResultCreator;
    private readonly Advancement.INextRoundStartDecider _nextRoundStartDecider;
    private readonly IStartlistProvider _startlistProvider;
    private readonly Dictionary<Guid, Guid>? _teamIdByIndividualId;
    private readonly Guid _hillId;

    private ClassicState _state;

    public ClassicEngine(Options options, Abstractions.IJumpScorer jumpScorer,
        Abstractions.IJumpResultCreator jumpResultCreator, Advancement.INextRoundStartDecider nextRoundStartDecider,
        IStartlistProvider startlistProvider,
        Dictionary<Guid, Guid>? teamIdByIndividualId, Guid hillId, Competition.Engine.Id id)
    {
        _options = options;
        _jumpScorer = jumpScorer;
        _teamIdByIndividualId = teamIdByIndividualId;
        _jumpResultCreator = jumpResultCreator;
        _hillId = hillId;
        Id = id;
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
            var noMoreJumpers = !FSharpOption<StartlistModule.Entity>.get_IsSome(Startlist?.Next);
            return isLastRound && noMoreJumpers;
        }
    }

    public Competition.Engine.Phase Phase => _state.Phase;

    public Competition.Engine.EngineSnapshotBlob RegisterJump(Jump jump)
    {
        if (jump.HillId.Item != _hillId)
        {
            throw new InvalidOperationException("ClassicEngine requires same hill for every jump");
        }

        var individualParticipantId = jump.IndividualParticipantId.Item;
        if (_state.Phase.IsEnded)
            throw new InvalidOperationException("Competition already ended");

        var round = _state.CurrentRound;
        if (round.Contains(individualParticipantId))
            throw new InvalidOperationException("Jump already registered");

        var jumpScore = _jumpScorer.Evaluate(jump);
        var jumpResult = _jumpResultCreator.Create(jumpScore, this.CurrentRoundIndex, jump.IndividualParticipantId);

        round.RegisterJump(individualParticipantId, jumpResult);

        return ToSnapshot();
    }

    public FSharpOption<Competition.Engine.EngineSnapshotBlob> SetUpNextRound()
    {
        if (!_state.Phase.IsWaitingForNextRound)
            throw new InvalidOperationException("Engine must be in WaitingForNextRound state to set up next round");

        var nextRoundIndex = _state.CurrentRoundIndex + 1;
        _state = _state with
        {
            Startlist = this.Startlist,
            CurrentRoundIndex = nextRoundIndex,
            RoundResults = _state.RoundResults
                .Append(new RoundResults { Index = nextRoundIndex })
                .ToList()
        };
        return ToSnapshot();
    }

    public FSharpOption<Competition.Engine.EngineSnapshotBlob> EndRound()
    {
        if (!_state.Phase.IsRunning)
            throw new InvalidOperationException("Engine must be in Running state to end the round");

        if (ShouldEndCompetition)
        {
            _state = _state with { Phase = Competition.Engine.Phase.Ended };
            return FSharpOption<Competition.Engine.EngineSnapshotBlob>.Some(ToSnapshot());
        }

        var nextRoundStartDecision = _nextRoundStartDecider.Decide(CurrentRoundIndex);

        return nextRoundStartDecision.StartNextRound
            ? SetUpNextRound()
            : FSharpOption<Competition.Engine.EngineSnapshotBlob>.None;
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

    public FSharpList<ParticipantResult> ResultsState
    {
        get
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
            var teamId = Domain.Competition.TeamModule.Id.NewId(teamGroup.Key);

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

    public Startlist? Startlist => _state.Startlist;

    public Competition.Engine.EngineSnapshotBlob ToSnapshot()
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(_state);
        return Competition.Engine.EngineSnapshotBlob.NewEngineSnapshotBlob(bytes);
    }

    public void LoadSnapshot(Competition.Engine.EngineSnapshotBlob blob)
    {
        var bytes = blob.Item;
        _state = JsonSerializer.Deserialize<ClassicState>(bytes)
                 ?? throw new InvalidOperationException("Corrupted snapshot");
    }
}

internal sealed record RoundResults
{
    public int Index { get; init; }
    public Dictionary<Guid, JumpResult> JumpResultsByRound { get; init; } = new();

    public void RegisterJump(Guid participantId, JumpResult score)
    {
        JumpResultsByRound[participantId] = score;
    }

    public bool Contains(Guid participantId) => JumpResultsByRound.ContainsKey(participantId);
}

internal sealed record ClassicState
{
    public int CurrentRoundIndex { get; init; }
    public List<RoundResults> RoundResults { get; init; } = new();
    public Competition.Engine.Phase Phase { get; init; } = Competition.Engine.Phase.Ended;
    public Startlist? Startlist { get; init; }

    public double? GetPointsByRound(Guid participantId, int roundIndex)
    {
        if (roundIndex < 0 || roundIndex >= RoundResults.Count) return null;
        var roundResults = RoundResults[roundIndex];
        roundResults.JumpResultsByRound.TryGetValue(participantId, out var result);
        return result?.Score.Points.Item;
    }

    public double GetTotalPoints(Guid participantId) =>
        RoundResults
            .Select((_, roundIndex) => GetPointsByRound(participantId, roundIndex) ?? 0)
            .Sum();

    public static ClassicState CreateInitial() =>
        new() { CurrentRoundIndex = 0, RoundResults = new(), Startlist = null };

    public RoundResults CurrentRound => RoundResults.FirstOrDefault(r => r.Index == CurrentRoundIndex)
                                        ?? throw new InvalidOperationException("No current round");
}