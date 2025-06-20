namespace App.Domain.Competition

open App.Domain.Shared.Ids

module Strategies =
    module RawRules =
        type AdvancementSolverParam = { Jumpers: JumperId list }
        type AdvancementSolverResult = { JumpersWhichAdvance: JumperId list }
        type IAdvancementSolver =
            abstract member Solve: state: AdvancementSolverParam -> AdvancementSolverResult
    module Preset =
        module OneVsOneKo =
            type ExAequoState =
                { Participants: JumperId list
                // result details
                }

            type TieBreakerResult = { Winners: JumperId list }

            type ITieBreaker =
                abstract member Apply: state: ExAequoState -> TieBreakerResult
