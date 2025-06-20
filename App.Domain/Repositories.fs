namespace App.Domain.Repositories

open App.Domain
open App.Domain.Competition
open App.Domain.Game
open App.Domain.GameWorld
open App.Domain.Player
open App.Domain.Shared.Ids

type IHillRepository =
    abstract member GetById: HillId -> Async<Option<Hill>>

type IJumpersRepository =
    abstract member GetById: JumperId -> Async<Option<Jumper>>
    abstract member Save: JumperId -> Async<unit>
    abstract member SaveLiveForm: JumperId * JumperSkills.LiveForm -> Async<Option<unit>>
    abstract member Add: Jumper -> Async<unit>
    
type ICompetitionRulesPresetRepository =
    abstract member GetById: CompetitionRulesPresetId -> Async<Option<Competition.Rules.Preset.Preset>>
    abstract member GetAll: Async<Competition.Rules.Preset.Preset list>
    
type IHostRepository =
    abstract member GetById: HostId -> Async<Option<Host>>
    abstract member GetPermissionsById: HostId -> Async<Option<Host>>
    
type IServerRepository =
    abstract member GetById: ServerId -> Async<Option<Server>>
    abstract member GetByRegion: Server.Region -> Async<Server list>
    abstract member GetAvailable: Async<Server list>
    abstract member IsAvailable: ServerId -> Async<Option<bool>>
    abstract member Add: Server -> Async<unit>
    abstract member Remove: ServerId -> Async<unit>

type IRegionRepository =
    abstract member GetAll: Async<Server.Region list>
    
type IGameRepository =
    abstract member GetById: GameId -> Async<Option<Game>>
    abstract member Add: Game -> Async<unit>
    abstract member GetByPhase: Game.PhaseTag -> Async<Game list>
    abstract member Join: gameId: GameId * playerId: PlayerId -> Async<bool>
    
type IPlayerRepository =
    abstract member GetById: PlayerId -> Async<Option<Player>>
    abstract member Add: Player -> Async<unit>
    abstract member IsActive: PlayerId -> Async<Option<bool>>
    abstract member GetActive: Async<Player list>