namespace App.Domain._2.Matchmaking

open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

type IMatchmakings =
    abstract member Add: matchmaking: Matchmaking * ct: CancellationToken -> Task
    abstract member GetById: matchmakingId: MatchmakingId * ct: CancellationToken -> Task<Matchmaking option>
    abstract member GetInProgress: ct: CancellationToken -> Task<IEnumerable<Matchmaking>>
    abstract member GetEnded: ct: CancellationToken -> Task<IEnumerable<Matchmaking>>
