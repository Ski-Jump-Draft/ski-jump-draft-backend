namespace App.Domain.Repositories

open System.Threading
open System.Threading.Tasks
open App

type IDomainCrudRepository<'TId, 'T> =
    abstract member GetByIdAsync: id: 'TId -> System.Threading.Tasks.Task<'T option>
    abstract member SaveAsync: id: 'TId * value: 'T -> System.Threading.Tasks.Task

type IDomainCrudEventsRepository<'T, 'TId, 'TPayload> =
    abstract member GetByIdAsync: id: 'TId -> System.Threading.Tasks.Task<'T option>

    abstract SaveAsync:
        id: 'TId *
        value: 'T *
        events: 'TPayload list *
        correlationId: System.Guid *
        causationId: System.Guid *
        ct: CancellationToken ->
            Task

// --- User ---
type public IUserRepository =
    inherit IDomainCrudRepository<Domain.Profile.User.Id, Domain.Profile.User.User>

// --- GameWorld ---
type public IGameWorldCountryRepository =
    inherit IDomainCrudRepository<Domain.GameWorld.Country.Id, Domain.GameWorld.Country>

type public IGameWorldHillRepository =
    inherit IDomainCrudEventsRepository<
        Domain.GameWorld.Hill,
        Domain.GameWorld.HillTypes.Id,
        Domain.GameWorld.Event.HillEventPayload
     >

type public IGameWorldJumperRepository =
    inherit IDomainCrudRepository<Domain.GameWorld.JumperTypes.Id, Domain.GameWorld.Jumper>

// --- Hosting ---
type public IHostRepository =
    inherit IDomainCrudRepository<Domain.Game.Hosting.Host.Id, Domain.Game.Hosting.Host>

type public IServerRepository =
    inherit IDomainCrudRepository<Domain.Game.Server.Id, Domain.Game.Server>

// --- Game ---
// type public IGameHillRepository =
//     inherit IDomainCrudRepository<Domain.Game.Hill.Id, Domain.Game.Hill.Hill>

// TODO: Co z IGameCompetitionRepository? Co z koncepcją Game Competition i co to jesT?

// type public IGameCompetitionRepository =
//     inherit IDomainCrudRepository<Domain.Game.Competition.Id, Domain.Game.Competition>

// --- Matchmaking ---

// --- Pre‑Draft ---
type public IPreDraftHillRepository =
    inherit IDomainCrudRepository<Domain.PreDraft.Competition.Hill.Id, Domain.PreDraft.Competition.Hill>

// --- Competition ---

// TODO: Może przywrócić te repozytorium?
//
// type public ICompetitionHillRepository =
//     inherit IDomainCrudRepository<Domain.Competition.Hill.Id, Domain.Competition.Hill>

// type public ICompetitionResultsRepository =
//     inherit IDomainCrudRepository<Domain.Competition.ResultsModule.Id, Domain.Competition.ResultsModule.Results>

// --- Draft ---
// type public IDraftParticipantRepository =
//     inherit IDomainCrudRepository<Domain.Draft.Participant.Id, Domain.Draft.Participant.Participant>

// --- Competition Engine ---
type public ICompetitionEngineSnapshotRepository =
    inherit IDomainCrudRepository<Domain.Competition.Engine.Id, Domain.Competition.Engine.Snapshot>
