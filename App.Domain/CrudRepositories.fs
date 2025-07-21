namespace App.Domain.Repositories

open System.Threading
open App.Domain
open App.Domain.Competition
open App.Domain.Competition.Engine
open App.Domain.Game
open App.Domain.GameWorld
open App.Domain.Profile
open App.Domain.Profile.User

// --- Identity ---

type IUserRepository =
    abstract member GetByIdAsync: Id: User.Id -> Async<Option<User>>

// --- GameWorld ---

type IGameWorldHillRepository =
    abstract member GetByIdAsync: Id: GameWorld.Hill.Id * ct: CancellationToken -> Async<Option<Hill>>
    abstract member GetAllAsync: unit -> Async<Hill list>

type IGameWorldJumperRepository =
    abstract member GetByIdAsync: Id: GameWorld.Jumper.Id -> Async<Option<Jumper>>
    abstract member SaveAsync: Id: GameWorld.Jumper -> Async<unit>
// abstract member SaveLiveForm: Id: GameWorld.Jumper.Id * LiveForm: JumperSkills.LiveForm -> Async<Option<unit>> TODO ReadModel

// type ICompetitionRulesPresetRepository =
//     abstract member GetByIdAsync: Id: Rules.Preset.Preset.Id -> Async<Option<Rules.Preset.Preset>>
// //abstract member GetAll: Async<Rules.Preset.Preset list> TODO: Read model

// --- Hosting ---

type IHostRepository =
    abstract member GetByIdAsync: Id: Game.Hosting.Host.Id -> Async<Option<Game.Hosting.Host>>
    abstract member GetPermissionsById: Id: Game.Hosting.Host.Id -> Async<Option<Game.Hosting.Host>>

// TODO: Przenieść do projekcji/read repository
type IServerRepository =
    abstract member GetByIdAsync: Id: Game.Server.Id -> Async<Option<Server>>
    // abstract member GetByRegion: Server.Region -> Async<Server list>
    // abstract member GetAvailable: Async<Server list>
    // abstract member IsAvailable: Id: Game.Server.Id -> Async<Option<bool>>
    abstract member AddAsync: Server: Server -> Async<unit>
    abstract member RemoveAsync: Id: Game.Server.Id -> Async<unit>

//type IRegionRepository =
// abstract member GetAll: Async<Server.Region list> TODO: Read model

// --- Game ---

type IGameHillRepository =
    abstract member GetByIdAsync: Id: App.Domain.Game.Hill.Id * ct: CancellationToken -> Async<Option<App.Domain.Game.Hill.Hill>>
    abstract member SaveAsync: Hill: App.Domain.Game.Hill.Hill -> Async<unit>

type IGameParticipantRepository =
    abstract member GetByIdAsync: Id: Game.Participant.Id -> Async<Option<Game.Participant.Participant>>
    abstract member RemoveAsync: Id: Game.Participant.Id -> Async<unit>
    abstract member SaveAsync: Participant: Game.Participant.Participant -> Async<unit>
    abstract member SaveAsync: Participants: Game.Participant.Participant list -> Async<unit>

type IGameCompetitionRepository =
    abstract member GetByIdAsync: Id: Game.Competition.Id -> Async<Option<Game.Competition>>

// --- Matchmaking ---

type IMatchmakingParticipantRepository =
    abstract member GetByIdAsync: Id: Matchmaking.Participant.Id -> Async<Option<Matchmaking.Participant>>
    abstract member RemoveAsync: Id: Matchmaking.Participant.Id -> Async<unit>
    abstract member SaveAsync: Participant: Matchmaking.Participant -> Async<unit>

// --- Pre Draft ---

type IPreDraftCompetitionRepository =
    abstract member GetByIdAsync:
        Id: PreDraft.Competitions.Competition.Id -> Async<Option<PreDraft.Competitions.Competition>>

// --- Competition ---

type ICompetitionHillRepository =
    abstract member GetByIdAsync:
        Id: App.Domain.Competition.Hill.Id * ct: CancellationToken -> Async<Option<App.Domain.Competition.Hill>>

    abstract member SaveAsync: Hill: App.Domain.Competition.Hill -> Async<unit>

type ICompetitionResultsRepository =
    abstract member GetByIdAsync: Id: Competition.ResultsModule.Id -> Async<Option<Competition.ResultsModule.Results>>
    abstract member SaveAsync: Results: Competition.ResultsModule.Results -> Async<unit>
    abstract member UpdateAsync: Results: Competition.ResultsModule.Results -> Async<unit>

type ICompetitionStartlistRepository =
    abstract member GetByIdAsync: Id: Competition.Startlist.Id -> Async<Option<Competition.Startlist>>
    abstract member SaveAsync: Startlist: Competition.Startlist -> Async<unit>
    abstract member UpdateAsync: Startlist: Competition.Startlist -> Async<unit>

// --- Draft ---

type IDraftParticipantRepository =
    abstract member GetByIdAsync: Id: Draft.Participant.Id -> Async<Option<Draft.Participant.Participant>>
// abstract member GetByDraft: Id: Draft.Id.Id -> Async<Draft.Participant.Participant list> TODO: Do read modelu

type IDraftSubjectRepository =
    abstract member GetByIdAsync: Id: Draft.Subject.Id -> Async<Option<Draft.Subject.Subject>>
    abstract member GetByDraft: Id: Draft.Id.Id -> Async<Draft.Subject.Subject list>

// --- Competition Engine ---

type ICompetitionEngineSnapshotRepository =
    abstract member GetByIdAsync: Id: Engine.Id -> Async<Option<EngineSnapshotBlob>>
    abstract member SaveByIdAsync: Id: Engine.Id * Snapshot: Engine.EngineSnapshotBlob -> Async<unit>
