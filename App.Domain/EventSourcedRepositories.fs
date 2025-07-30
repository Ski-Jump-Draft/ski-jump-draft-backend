namespace App.Domain.Repositories

open System.Threading
open System.Threading.Tasks
open App.Domain
open App.Domain.Competition
open App.Domain.Draft
open App.Domain.Game
open App.Domain.PreDraft
open App.Domain.Shared.AggregateVersion


type IEventSourcedRepository<'TAggregate, 'TId, 'TPayload> =
    abstract LoadAsync: Id: 'TId * Ct: CancellationToken -> Task<'TAggregate option>

    abstract SaveAsync:
        AggregateId: 'TId *
        Events: 'TPayload list *
        ExpectedVersion: AggregateVersion *
        CorrelationId: System.Guid *
        CausationId: System.Guid *
        Ct: CancellationToken ->
            Task

    abstract ExistsAsync: Id: 'TId * Ct: CancellationToken -> Task<bool>

    abstract GetVersionAsync: Id: 'TId * Ct: CancellationToken -> Task<AggregateVersion>

    abstract LoadHistoryAsync: Id: 'TId * Ct: CancellationToken -> Task<'TPayload list>

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
