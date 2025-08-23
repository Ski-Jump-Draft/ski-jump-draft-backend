namespace App.Domain._2.Matchmaking

open System.Collections.Generic
open System.Threading.Tasks

type IMatchmakingRepository =
    abstract member Add: matchmaking: Matchmaking -> Task
    abstract member GetById: matchmakingId: MatchmakingId -> Task<Matchmaking>
    abstract member GetInProgress: unit -> Task<IEnumerable<Matchmaking>>
    abstract member GetEnded: unit -> Task<IEnumerable<Matchmaking>>
