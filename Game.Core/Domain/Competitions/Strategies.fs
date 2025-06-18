namespace Game.Core.Domain.Competitions

open Game.Core.Domain.Shared.Ids

module Strategies =
    module Preset =
        module OneVsOneKo =
            type ExAequoState =
                { Participants: JumperId list
                // result details
                }

            type TieBreakerResult = { Winners: JumperId list }

            type ITieBreaker =
                abstract member Apply: state: ExAequoState -> TieBreakerResult
