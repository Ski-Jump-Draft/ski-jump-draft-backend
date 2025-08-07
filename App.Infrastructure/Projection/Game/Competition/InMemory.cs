using System.Collections.Concurrent;
using App.Application.Abstractions;
using App.Application.ReadModel.Projection;
using App.Domain.Shared;

namespace App.Infrastructure.Projection.Game.Competition;

// GamePreDraftStarted -> update mapa <Game.Id, PreDraft.Id>
// PreDraftCompetitionStarted -> update <Game.Id, Competition.Id>
// GameCompetitionStarted -> update mapa <Game.Id, PostDraft Competition.Id>
// 
// 
// 
// 
// 
// 
// 
// 

public class InMemory(ICompetitorToDraftSubjectMapper competitorToDraftMapping) : IGameCompetitionProjection,
    IEventHandler<Domain.Game.Event.GameEventPayload>,
    IEventHandler<Domain.PreDraft.Event.PreDraftEventPayload>,
    IEventHandler<Domain.SimpleCompetition.Event.CompetitionEventPayload>,
    IEventHandler<Domain.Draft.Event.DraftEventPayload>
{
    private readonly ConcurrentDictionary<Domain.PreDraft.Id.Id, Domain.Game.Id.Id> _gameByPreDraft = new();

    private readonly ConcurrentDictionary<Domain.SimpleCompetition.CompetitionId, GameCompetitionType>
        _gameCompetitionTypeByCompetition = new();

    private readonly ConcurrentDictionary<Domain.Game.Id.Id, Domain.SimpleCompetition.CompetitionId>
        _activeCompetitionByGame =
            new();

    private readonly ConcurrentDictionary<Domain.SimpleCompetition.CompetitionId, Domain.Game.Id.Id>
        _gameByCompetition = new();

    private readonly ConcurrentDictionary<Domain.Game.Id.Id, Domain.SimpleCompetition.CompetitionId>
        _postDraftCompetitionByGame = new();

    private readonly
        ConcurrentDictionary<Domain.SimpleCompetition.CompetitionId, Dictionary<Domain.Draft.Subject.Id, int>>
        _draftSubjectPositionsByCompetition = new();


    public Task<GameCompetitionDto?> GetActiveCompetitionByGameIdAsync(Domain.Game.Id.Id gameId)
    {
        _activeCompetitionByGame.TryGetValue(gameId, out var competitionId);
        if (competitionId is null) return Task.FromResult<GameCompetitionDto?>(null);
        var competitionType = _gameCompetitionTypeByCompetition[competitionId];
        return Task.FromResult(new GameCompetitionDto(gameId, competitionType, competitionId))!;
    }

    public Task<GamePostDraftCompetitionDto?> GetPostDraftCompetitionByGameIdAsync(Domain.Game.Id.Id gameId)
    {
        _postDraftCompetitionByGame.TryGetValue(gameId, out var postDraftCompetitionId);
        if (postDraftCompetitionId is null) return Task.FromResult<GamePostDraftCompetitionDto?>(null);
        return Task.FromResult(new GamePostDraftCompetitionDto(gameId, postDraftCompetitionId))!;
    }

    public Task<GameCompetitionDraftSubjectPositionsDto?> GetDraftSubjectPositionsByEndedGameIdAsync(
        Domain.Game.Id.Id gameId)
    {
        if (!_postDraftCompetitionByGame.TryGetValue(gameId, out var postDraftCompetitionId))
            return Task.FromResult<GameCompetitionDraftSubjectPositionsDto?>(null);

        if (_draftSubjectPositionsByCompetition.TryGetValue(postDraftCompetitionId, out var positions))
        {
            return Task.FromResult(
                new GameCompetitionDraftSubjectPositionsDto(gameId, postDraftCompetitionId, positions))!;
        }

        return Task.FromResult<GameCompetitionDraftSubjectPositionsDto?>(null);
    }

    public Task HandleAsync(DomainEvent<Domain.Game.Event.GameEventPayload> @event, CancellationToken ct)
    {
        var payload = @event.Payload;
        switch (payload)
        {
            case Domain.Game.Event.GameEventPayload.PreDraftPhaseStartedV1 preDraftPhaseStarted:
            {
                var gameId = preDraftPhaseStarted.Item.GameId;
                var preDraftId = preDraftPhaseStarted.Item.PreDraftId;
                var added = _gameByPreDraft.TryAdd(preDraftId, gameId);
                if (!added)
                {
                    throw new Exception($"A Game assigned to the PreDraftId ({preDraftId}) already exists");
                }

                break;
            }
            case Domain.Game.Event.GameEventPayload.CompetitionPhaseStartedV1 competitionPhaseStarted:
            {
                var gameId = competitionPhaseStarted.Item.GameId;
                var competitionId = competitionPhaseStarted.Item.GameCompetition.CompetitionId;
                _activeCompetitionByGame[gameId] = competitionId;
                if (!_postDraftCompetitionByGame.TryAdd(gameId, competitionId))
                {
                    throw new Exception($"A Game already has a PostDraft Competition ({competitionId})");
                }

                _gameCompetitionTypeByCompetition[competitionId] = GameCompetitionType.PostDraft;
                _gameByCompetition[competitionId] = gameId;
                break;
            }
            case Domain.Game.Event.GameEventPayload.CompetitionPhaseEndedV1 competitionPhaseEnded:
            {
                var gameId = competitionPhaseEnded.Item.GameId;

                if (_activeCompetitionByGame.TryRemove(gameId, out var competitionId))
                {
                    _gameCompetitionTypeByCompetition.TryRemove(competitionId, out _);
                    _gameByCompetition.TryRemove(competitionId, out _);
                }

                break;
            }
            case Domain.Game.Event.GameEventPayload.GameEndedV1 gameEnded:
            {
                var gameId = gameEnded.Item.GameId;

                if (_postDraftCompetitionByGame.TryRemove(gameId, out var postDraftCompetitionId))
                {
                    _draftSubjectPositionsByCompetition.TryRemove(postDraftCompetitionId, out _);
                }

                break;
            }
        }

        return Task.CompletedTask;
    }

    public Task HandleAsync(DomainEvent<Domain.PreDraft.Event.PreDraftEventPayload> @event, CancellationToken ct)
    {
        var payload = @event.Payload;
        if (payload is Domain.PreDraft.Event.PreDraftEventPayload.PreDraftCompetitionStartedV1
            preDraftCompetitionStarted)
        {
            _gameByPreDraft.TryGetValue(preDraftCompetitionStarted.Item.PreDraftId, out var gameId);
            if (gameId is null) return Task.CompletedTask;
            var competitionId = preDraftCompetitionStarted.Item.CompetitionId;
            _activeCompetitionByGame[gameId] = competitionId;
            _gameCompetitionTypeByCompetition[competitionId] = GameCompetitionType.PreDraft;
            _gameByCompetition[competitionId] = gameId;
        }

        return Task.CompletedTask;
    }

    public Task HandleAsync(DomainEvent<Domain.SimpleCompetition.Event.CompetitionEventPayload> @event,
        CancellationToken ct)
    {
        var payload = @event.Payload;
        if (payload is Domain.SimpleCompetition.Event.CompetitionEventPayload.CompetitionEndedV1 competitionEnded)
        {
            var competitionId = competitionEnded.Item.CompetitionId;
            if (!_gameByCompetition.TryGetValue(competitionId, out var gameId))
            {
                throw new Exception($"A Game not found for Competition Id: {competitionId}");
            }

            if (!_activeCompetitionByGame[gameId].Equals(competitionId))
            {
                throw new Exception($"The Competition (id: {competitionId
                } is not the active competition for the Game (id: {gameId})");
            }

            var finalResults = competitionEnded.Item.FinalIndividualResults;

            var rankBySubjectId = finalResults.ToDictionary(keySelector: competitorResultDto =>
            {
                var competitorId = competitorResultDto.CompetitorId;
                var draftSubjectId = competitorToDraftMapping.TryGetSubjectId(competitorId);
                if (draftSubjectId is null)
                {
                    throw new Exception($"Draft Subject not found for Competitor Id: {competitorId}");
                }

                return (Domain.Draft.Subject.Id)draftSubjectId;
            }, competitorResultDto => competitorResultDto.Rank);
            _draftSubjectPositionsByCompetition[competitionId] = rankBySubjectId;
        }

        return Task.CompletedTask;
    }

    public Task HandleAsync(DomainEvent<Domain.Draft.Event.DraftEventPayload> @event, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}