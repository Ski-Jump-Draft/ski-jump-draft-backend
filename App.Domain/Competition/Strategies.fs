namespace App.Domain.Competition

open App.Domain.GameWorld

module Strategies =
    module RawRules =
        type AdvancementSolverParam = { Jumpers: Jumper.Id list }
        type AdvancementSolverResult = { JumpersWhichAdvance: Jumper.Id list }
        type IAdvancementSolver =
            abstract member Solve: state: AdvancementSolverParam -> AdvancementSolverResult
    module Preset =
        module OneVsOneKo =
            type ExAequoState =
                { Participants: Jumper.Id list
                // result details
                }

            type TieBreakerResult = { Winners: Jumper.Id list }

            type ITieBreaker =
                abstract member Apply: state: ExAequoState -> TieBreakerResult
