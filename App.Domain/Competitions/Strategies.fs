namespace App.Domain.Competitions

open App.Domain.GameWorld

module Strategies =
    module RawRules =
        type AdvancementSolverParam = { Jumpers: Single.Id list }
        type AdvancementSolverResult = { JumpersWhichAdvance: Single.Id list }
        type IAdvancementSolver =
            abstract member Solve: state: AdvancementSolverParam -> AdvancementSolverResult
    module Preset =
        module OneVsOneKo =
            type ExAequoState =
                { Participants: Single.Id list
                // result details
                }

            type TieBreakerResult = { Winners: Single.Id list }

            type ITieBreaker =
                abstract member Apply: state: ExAequoState -> TieBreakerResult
