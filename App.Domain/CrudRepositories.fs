namespace App.Domain.Repositories

open App

type IDomainCrudRepository<'TId, 'T> =
    abstract member GetByIdAsync: id: 'TId -> System.Threading.Tasks.Task<'T option>
    abstract member SaveAsync: id: 'TId * value: 'T -> System.Threading.Tasks.Task

// --- User ---
type public IUserRepository =
    inherit IDomainCrudRepository<Domain.Profile.User.Id, Domain.Profile.User.User>

// --- GameWorld ---
type public IGameWorldHillRepository =
    inherit IDomainCrudRepository<Domain.GameWorld.HillId, Domain.GameWorld.Hill>

type public IGameWorldJumperRepository =
    inherit IDomainCrudRepository<Domain.GameWorld.Jumper.Id, Domain.GameWorld.Jumper>

// --- Hosting ---
type public IHostRepository =
    inherit IDomainCrudRepository<Domain.Game.Hosting.Host.Id, Domain.Game.Hosting.Host>

type public IServerRepository =
    inherit IDomainCrudRepository<Domain.Game.Server.Id, Domain.Game.Server>

// --- Game ---
// type public IGameHillRepository =
//     inherit IDomainCrudRepository<Domain.Game.Hill.Id, Domain.Game.Hill.Hill>

type public IGameParticipantRepository =
    inherit IDomainCrudRepository<Domain.Game.Participant.Id, Domain.Game.Participant.Participant>

type public IGameCompetitionRepository =
    inherit IDomainCrudRepository<Domain.Game.Competition.Id, Domain.Game.Competition>

// --- Matchmaking ---
type public IMatchmakingParticipantRepository =
    inherit IDomainCrudRepository<Domain.Matchmaking.Participant.Id, Domain.Matchmaking.Participant>

// --- Pre‑Draft ---
type public IPreDraftHillRepository =
    inherit IDomainCrudRepository<Domain.PreDraft.Competitions.Hill.Id, Domain.PreDraft.Competitions.Hill>

// type public IPreDraftCompetitionRepository =
//     inherit IDomainCrudRepository<Domain.PreDraft.Competitions.Competition.Id, Domain.PreDraft.Competitions.Competition>

// --- Competition ---
type public ICompetitionHillRepository =
    inherit IDomainCrudRepository<Domain.Competition.Hill.Id, Domain.Competition.Hill>

type public ICompetitionResultsRepository =
    inherit IDomainCrudRepository<Domain.Competition.ResultsModule.Id, Domain.Competition.ResultsModule.Results>

type public ICompetitionStartlistRepository =
    inherit IDomainCrudRepository<Domain.Competition.Startlist.Id, Domain.Competition.Startlist>

// --- Draft ---
type public IDraftParticipantRepository =
    inherit IDomainCrudRepository<Domain.Draft.Participant.Id, Domain.Draft.Participant.Participant>

type public IDraftSubjectRepository =
    inherit IDomainCrudRepository<Domain.Draft.Subject.Id, Domain.Draft.Subject.Subject>

// --- Competition Engine ---
type public ICompetitionEngineSnapshotRepository =
    inherit IDomainCrudRepository<Domain.Competition.Engine.Id, Domain.Competition.Engine.EngineSnapshotBlob>
