namespace App.Domain.Repositories

open App.Domain
open App.Domain.Competition
open App.Domain.Draft
open App.Domain.Game
open App.Domain.Matchmaking
open App.Domain.PreDraft
open App.Domain.Shared
open App.Domain.Shared.AggregateVersion


type IEventSourcedRepository<'TAggregate, 'TId, 'TPayload> =
    abstract LoadAsync: Id: 'TId * CancellationToken: System.Threading.CancellationToken -> Async<'TAggregate option>

    abstract SaveAsync:
        Aggregate: 'TAggregate *
        Events: 'TPayload list *
        ExpectedVersion: AggregateVersion *
        CorrelationId: System.Guid *
        CausationId: System.Guid *
        CancellationToken: System.Threading.CancellationToken ->
            Async<unit>

    abstract ExistsAsync: Id: 'TId -> Async<bool>

    abstract GetVersionAsync: Id: 'TId -> Async<AggregateVersion>

    abstract LoadHistoryAsync: Id: 'TId -> Async<'TPayload list>

type ICompetitionRepository =
    inherit IEventSourcedRepository<
        Competition.Competition,
        Competition.Id.Id,
        Competition.Event.CompetitionEventPayload
     >

type IDraftRepository =
    inherit IEventSourcedRepository<Draft, Draft.Id.Id, Draft.Event.DraftEventPayload>

type IPreDraftRepository =
    inherit IEventSourcedRepository<PreDraft, PreDraft.Id.Id, PreDraft.Event.PreDraftEventPayload>

type IGameRepository =
    inherit IEventSourcedRepository<Game, Game.Id.Id, Game.Event.GameEventPayload>

type IMatchmakingRepository =
    inherit IEventSourcedRepository<
        App.Domain.Matchmaking.Matchmaking,
        Matchmaking.Id,
        App.Domain.Matchmaking.Event.MatchmakingEventPayload
     >
