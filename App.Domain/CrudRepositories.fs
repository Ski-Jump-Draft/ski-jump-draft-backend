namespace App.Domain.Repositories

open App.Domain
open App.Domain.Competition
open App.Domain.Competition.Engine
open App.Domain.Game
open App.Domain.GameWorld
open App.Domain.Profile
open App.Domain.Profile.User

type IUserRepository =
    abstract member GetByIdAsync: User.Id -> Async<Option<User>>

type IHillRepository =
    abstract member GetById: GameWorld.Hill.Id -> Async<Option<Hill>>

type IJumpersRepository =
    abstract member GetById: GameWorld.Jumper.Id -> Async<Option<Jumper>>
    abstract member Save: GameWorld.Jumper.Id -> Async<unit>
    abstract member SaveLiveForm: GameWorld.Jumper.Id * JumperSkills.LiveForm -> Async<Option<unit>>
    abstract member Add: Jumper -> Async<unit>

type ICompetitionRulesPresetRepository =
    abstract member GetById: Rules.Preset.Preset.Id -> Async<Option<Rules.Preset.Preset>>
    abstract member GetAll: Async<Rules.Preset.Preset list>

type IHostRepository =
    abstract member GetById: Game.Hosting.Host.Id -> Async<Option<Game.Hosting.Host>>
    abstract member GetPermissionsById: Game.Hosting.Host.Id -> Async<Option<Game.Hosting.Host>>

type IServerRepository =
    abstract member GetById: Game.Server.Id -> Async<Option<Server>>
    abstract member GetByRegion: Server.Region -> Async<Server list>
    abstract member GetAvailable: Async<Server list>
    abstract member IsAvailable: Game.Server.Id -> Async<Option<bool>>
    abstract member Add: Server -> Async<unit>
    abstract member Remove: Game.Server.Id -> Async<unit>

type IRegionRepository =
    abstract member GetAll: Async<Server.Region list>

// type IGameRepository =
//     abstract member GetByIdAsync: Game.Game.Id -> Async<Option<Game>>
//     abstract member Add: Game -> Async<unit>
//     abstract member Update: Game.Game.Id * Game -> Async<unit>
//     abstract member GetByPhase: Game.PhaseTag -> Async<Game list>

type IGameParticipantRepository =
    abstract member GetByIdAsync: Game.Participant.Id -> Async<Option<Game.Participant.Participant>>
    abstract member RemoveAsync: Game.Participant.Id -> Async<unit>
    abstract member SaveAsync: Game.Participant.Participant -> Async<unit>

type IGameCompetitionRepository =
    abstract member GetById: Game.Competition.Id -> Async<Option<Game.Competition>>

type IPreDraftCompetitionRepository =
    abstract member GetById: PreDraft.Competitions.Competition.Id -> Async<Option<PreDraft.Competitions.Competition>>

type ICompetitionResultsRepository =
    abstract member GetById: Competition.ResultsModule.Id -> Async<Option<Competition.ResultsModule.Results>>
    abstract member Save: Competition.ResultsModule.Results -> Async<unit>
    abstract member Update: Competition.ResultsModule.Results -> Async<unit>

type ICompetitionStartlistRepository =
    abstract member GetById: Competition.Startlist.Id -> Async<Option<Competition.Startlist>>
    abstract member Save: Competition.Startlist -> Async<unit>
    abstract member Update: Competition.Startlist -> Async<unit>

// type IDraftRepository =
//     abstract member GetById: Draft.Id.Id -> Async<Option<Draft.Draft>>
//     abstract member Add: Draft.Draft -> Async<unit>
//     abstract member GetByPhase: Draft.PhaseTag -> Async<Draft.Draft list>

type IDraftParticipantRepository =
    abstract member GetById: Draft.Participant.Id -> Async<Option<Draft.Participant.Participant>>
    abstract member GetByDraft: Draft.Id.Id -> Async<Draft.Participant.Participant list>

type IDraftSubjectRepository =
    abstract member GetById: Draft.Subject.Id -> Async<Option<Draft.Subject.Subject>>
    abstract member GetByDraft: Draft.Id.Id -> Async<Draft.Subject.Subject list>

type ICompetitionEngineSnapshotRepository =
    abstract member GetSnapshotById: Engine.Id -> Async<EngineSnapshotBlob>
    abstract member SaveSnapshotById: Id: Engine.Id * Snapshot: Engine.EngineSnapshotBlob -> Async<unit>
