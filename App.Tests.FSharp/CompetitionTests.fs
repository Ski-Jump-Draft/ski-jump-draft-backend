module App.Domain.Tests.CompetitionTests

open App
open System
open App.Domain.Shared
open App.Domain.SimpleCompetition.Jump
open FsUnit.CustomMatchers
open Xunit
open FsUnit.Xunit
open App.Domain.SimpleCompetition
open App.Domain.SimpleCompetition.Event

let newId () = Guid.NewGuid()

// ---------- helpers ----------------------------------------------------

let makeHill () =
    { Hill.Id = Hill.Id(newId ())
      KPoint = (Hill.KPoint.tryCreate 90.0).Value
      HsPoint = (Hill.HsPoint.tryCreate 100.0).Value
      GatePoints = (Hill.GatePoints.tryCreate 6.8).Value
      HeadwindPoints = (Hill.WindPoints.tryCreate 10.8).Value
      TailwindPoints = (Hill.WindPoints.tryCreate 16.2).Value }

let makeGateState () =
    { Starting = Gate 12
      CurrentJury = Gate 10 }
    : Domain.SimpleCompetition.GateState

let makeVersion () = AggregateVersion.zero

let makeCompetitor () =
    let id = Competitor.Id(newId ())
    Competitor.Create id None

let makeTeam nMembers =
    let comps = [ for _ in 1..nMembers -> makeCompetitor () ]
    let id = Team.Id(newId ())
    Team.Create id comps

let basicRoundSettings =
    { RoundLimit = RoundLimit.NoneLimit
      SortStartlist = false
      ResetPoints = false
      GroupSettings = None }

let makeSettings rounds =
    let ok = List.replicate rounds basicRoundSettings
    Settings.Create ok |> Result.toOption |> Option.get

let makeJump (competitorId) =
    { Jump.Id = Jump.Id(newId ())
      CompetitorId = competitorId
      Distance = (Distance.tryCreate 95.0) |> Result.toOption |> Option.get
      Gate = Jump.Gate 15
      GatesLoweredByCoach = Jump.GatesLoweredByCoach 0
      WindAverage = WindAverage.Zero
      JudgeNotes =
        JudgeNotes.tryCreate [ 18.0; 18.5; 19.0; 18.5; 18.0 ]
        |> Result.toOption
        |> Option.get }

let referenceGate = Jump.Gate 15

// ---------- TESTS ------------------------------------------------------

[<Fact>]
let ``Individual flow – 2 rundy, Soft limit + reset`` () =
    // przygotowanie
    let hill = makeHill ()

    let settings =
        let rs1 =
            { basicRoundSettings with
                RoundLimit = RoundLimit.Soft(RoundLimitValue.tryCreate 30 |> Result.toOption |> Option.get) }

        let rs2 =
            { basicRoundSettings with
                ResetPoints = true }

        Settings.Create [ rs1; rs2 ] |> Result.toOption |> Option.get

    let competitors = [ for _ in 1..50 -> makeCompetitor () ]

    // create
    let comp =
        Domain.SimpleCompetition.Competition.CreateIndividual(
            CompetitionId(newId ()),
            makeVersion (),
            settings,
            hill,
            competitors,
            makeGateState ()
        )
        |> Result.toOption
        |> Option.get

    // start + pierwszy skok
    let firstJumper = comp.NextCompetitor |> Option.get
    let jump = makeJump (firstJumper.Id_)

    let competition, events =
        comp.<AddJump(JumpResult.Id(newId ()), firstJumper.Id_, jump)
        |> Result.toOption
        |> Option.get

    // asercje
    events
    |> List.exists (function
        | CompetitionStartedV1 _ -> true
        | _ -> false)
    |> should be True

    events
    |> List.exists (function
        | JumpAddedV1 _ -> true
        | _ -> false)
    |> should be True

    competition.Status_
    |> should equal (Competition.Status.RoundInProgress(makeGateState (), RoundIndex 0u, None))

[<Fact>]
let ``DSQ kończy grupę, sortuje wg punktów i przechodzi do następnej grupy`` () =
    // przygotowanie 4 drużyny x 4 zawodników
    let hill = makeHill ()

    let settings =
        let gs = { GroupIndexesToSort = set [ GroupIndex 1u ] }

        let r1 =
            { basicRoundSettings with
                GroupSettings = Some gs }

        Settings.Create [ r1 ] |> Result.toOption |> Option.get

    let teams = [ for _ in 1..4 -> makeTeam 4 ]

    let comp =
        Competition.CreateTeam(CompetitionId(newId ()), makeVersion (), settings, hill, teams, makeGateState ())
        |> Result.toOption
        |> Option.get

    // start – pierwszy jumper z team 1
    let firstEntry = comp.Startlist_.NextJumper().Value

    let competition1, _ =
        comp.AddJump(JumpResult.Id(newId ()), firstEntry.CompetitorId, makeJump firstEntry.CompetitorId)
        |> Result.toOption
        |> Option.get

    // DSQ kolejnych 3 zawodników – kończy 1. grupę
    let jumpersGrp1 = competition1.Startlist_.Remaining_ |> List.map _.CompetitorId

    let competition =
        jumpersGrp1
        |> List.fold
            (fun (state: Competition) competitorId ->
                state.Disqualify(competitorId, DisqualificationReason.Other "other")
                |> Result.toOption
                |> Option.get
                |> fst)
            competition1 // <-- bez tuple


    let gateState = makeGateState ()
    // asercje
    match competition.Status_ with
    | Competition.RoundInProgress(gateState, _, Some(GroupIndex 1u)) -> ()
    | _ -> failwith "powinna rozpocząć się grupa 2"

[<Fact>]
let ``Exact limit + tie-breaker HighestBib odrzuca nadmiarowych`` () =
    let hill = makeHill ()

    let roundSettings =
        let roundLimit =
            RoundLimit.Exact(
                RoundLimitValue.tryCreate 2 |> Result.toOption |> Option.get,
                TieBreakerCriteria.HighestBib
            )

        { basicRoundSettings with
            RoundLimit = roundLimit }

    let settings =
        Settings.Create [ roundSettings; basicRoundSettings ] // <-- było tylko [ roundSettings ]
        |> Result.toOption
        |> Option.get


    let competitors = [ makeCompetitor (); makeCompetitor (); makeCompetitor () ]

    let competition0 =
        Competition.CreateIndividual(
            CompetitionId(newId ()),
            makeVersion (),
            settings,
            hill,
            competitors,
            makeGateState ()
        )
        |> Result.toOption
        |> Option.get

    // oba skoki z identycznymi punktami
    let competition1, _ =
        competition0.AddJump(JumpResult.Id(newId ()), competitors[0].Id_, makeJump (competitors[0].Id_))
        |> Result.toOption
        |> Option.get

    let competition2, _ =
        competition1.AddJump(JumpResult.Id(newId ()), competitors[1].Id_, makeJump (competitors[1].Id_))
        |> Result.toOption
        |> Option.get

    let competition3, _ =
        competition2.AddJump(JumpResult.Id(newId ()), competitors[2].Id_, makeJump (competitors[2].Id_))
        |> Result.toOption
        |> Option.get

    let gateState = makeGateState ()

    match competition3.Status_ with
    | Competition.RoundInProgress(gateState, RoundIndex 1u, _) ->
        competition3.Startlist_.Remaining_.Length |> should equal 2
    | _ -> failwith "powinna wystartować 2 runda"

[<Fact>]
let ``Suspend i Continue blokują dodawanie skoków`` () =
    let hill = makeHill ()
    let settings = makeSettings 2
    let competitors = [ makeCompetitor (); makeCompetitor () ]

    let competition0 =
        Competition.CreateIndividual(
            CompetitionId(newId ()),
            makeVersion (),
            settings,
            hill,
            competitors,
            makeGateState ()
        )
        |> Result.toOption
        |> Option.get

    let firstCompetitorId = competition0.Startlist_.NextJumper().Value.CompetitorId

    let competition1, _ =
        competition0.AddJump(JumpResult.Id(newId ()), firstCompetitorId, makeJump (firstCompetitorId))
        |> Result.toOption
        |> Option.get

    let competitionSuspended, _ =
        competition1.Suspend "wiatr" |> Result.toOption |> Option.get

    // próba skoku – błąd
    let res =
        competitionSuspended.AddJump(JumpResult.Id(newId ()), competitors.Head.Id_, makeJump (firstCompetitorId))

    res.IsError |> should equal true

    let compContinued, _ =
        competitionSuspended.Continue() |> Result.toOption |> Option.get

    let secondCompetitorId = competitors.Tail.Head.Id_
    let jump2 = makeJump secondCompetitorId

    let res = compContinued.AddJump(JumpResult.Id(newId ()), secondCompetitorId, jump2)

    res.IsOk |> should equal true

    res |> Result.isOk |> should equal true
