namespace App.Domain.Matchmaking

open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

type MatchmakingType =
    | Normal
    | Premium

type IMatchmakings =
    abstract member Add: matchmaking: Matchmaking * ct: CancellationToken -> Task
    abstract member GetById: matchmakingId: MatchmakingId * ct: CancellationToken -> Task<Matchmaking option>

    abstract member GetInProgress:
        matchmakingType: MatchmakingType option * ct: CancellationToken -> Task<IEnumerable<Matchmaking>>

    abstract member GetEnded: matchmakingType: MatchmakingType option * ct: CancellationToken -> Task<IEnumerable<Matchmaking>>
