namespace App.Domain._2.Game

open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

type IGames =
    abstract member Add: game: Game * ct: CancellationToken -> Task
    abstract member GetById: gameId: GameId * ct: CancellationToken -> Task<Game option>
    abstract member GetNotStarted: ct: CancellationToken -> Task<IEnumerable<Game>>
    abstract member GetInProgress: ct: CancellationToken -> Task<IEnumerable<Game>>
    abstract member GetEnded: ct: CancellationToken -> Task<IEnumerable<Game>>
