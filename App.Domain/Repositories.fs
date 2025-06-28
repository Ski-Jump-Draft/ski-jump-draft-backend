namespace App.Domain.Repositories

open App.Domain
open App.Domain.Competitions
open App.Domain.Game
open App.Domain.GameWorld

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

type IGameRepository =
    abstract member GetById: Game.Game.Id -> Async<Option<Game>>
    abstract member Add: Game -> Async<unit>
    abstract member Update: Game.Game.Id * Game -> Async<unit>
    abstract member GetByPhase: Game.PhaseTag -> Async<Game list>

type IDraftRepository =
    abstract member GetById: Draft.Id.Id -> Async<Option<Draft.Draft>>
    abstract member Add: Draft.Draft -> Async<unit>
    abstract member GetByPhase: Draft.PhaseTag -> Async<Draft.Draft list>

type IDraftParticipantRepository =
    abstract member GetById: Draft.Participant.Id -> Async<Option<Draft.Participant.Participant>>
    abstract member GetByDraft: Draft.Id.Id -> Async<Draft.Participant.Participant list>

type IDraftSubjectRepository =
    abstract member GetById: Draft.Subject.Id -> Async<Option<Draft.Subject.Subject>>
    abstract member GetByDraft: Draft.Id.Id -> Async<Draft.Subject.Subject list>
