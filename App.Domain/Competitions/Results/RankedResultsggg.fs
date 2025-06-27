// namespace App.Domain.Competitions
//
// open App.Domain.Competitions
// open App.Domain.Competitions.Results
// open App.Domain.CustomStrategies
//
// // module RankedResults = Ok
//
//     /// W domenie Results dajemy tylko polityke
//     /// Jest interfejs Domain Service z implementację niżej
//     /// Ten Domain Service używa jednego z 5 rodzajów ChooseOnePolicy
//     /// Np. sprawdza czy jest MoreRecentPoints i wywołuje kolejny handler
//     /// Tworzy int na bazie porównania ostatnich not, gdzie jest wiele danych do użycia, skoro to Application/Infra layer
//
//
// module RankedResults =
//     type Position = private Position of int
//
//     module Position =
//         type Error = | ZeroOrNegative
//         let tryCreate (v: int) =
//             if v >= 1 then
//                 Ok(Position v)
//             else
//                 Error(Error.ZeroOrNegative)
//         let value (Position v) = v
//     
//     type Participant =
//         { Id: ParticipantResultId
//           Position: Position }
//         
//     module Policy =
//         type ChooseOnePolicy =
//             | MoreRecentPoints
//             | FurtherLastDistance
//             | Alphabetical
//             | Random
//             | Custom of CustomStrategy.Ref
//         type ExAequoPolicy =
//             | AddOneAfterExAequo
//             | ContinueNormallyAfterExAequo
//         type TieBreakPolicy =
//             | ExAequo of ExAequoPolicy
//             | ChooseOne of ChooseOnePolicy
//             
//         type IChooseOneSolver =
//             abstract member Compare: ChooseOnePolicy -> ParticipantResult -> ParticipantResult -> int
//                 
//     type Policy =
//         {
//             TieBreakPolicy: Policy.TieBreakPolicy
//         }
//
//
// type RankedResultsggg =
//     private
//         { Ranked: RankedResults.Participant list }
//
//     static member Create (policy: RankedResults.Policy) =
//         
//         let ranked = None
//         { Ranked = ranked }
