module App.Domain.Draft.Picks

open System
open App.Domain
open App.Domain.Draft
open App.Domain.Shared.Random
open App.Domain.Shared.Utils.Range

module PickTimeout =
    type FixedTime = private FixedTime of TimeSpan

    module FixedTime =
        type Error = OutsideRange of OutsideRangeError<int>

        let tryCreate (v: TimeSpan) =
            if v >= TimeSpan.FromSeconds(10L) && v <= TimeSpan.FromSeconds(60L) then
                Ok(FixedTime v)
            else
                Error(
                    OutsideRange
                        { Min = Some 10
                          Max = Some 60
                          Current = v.Seconds }
                )

type PickTimeout =
    | Unlimited
    | Fixed of PickTimeout.FixedTime

type Picks private (picksMap: Map<Participant.Id, Subject.Id list>) =
    member private _.Inner = picksMap

    member this.Total() : int =
        this.Inner |> Seq.sumBy (fun kv -> List.length kv.Value)

    member this.PicksNumberOf(participant: Participant.Id) : int =
        this.Inner
        |> Map.tryFind participant
        |> Option.map List.length
        |> Option.defaultValue 0

    member this.CurrentRound(participants: Participant.Id list) : int = this.Total() / List.length participants

    member this.ContainsSubject(subjectId: Subject.Id) : bool =
        this.Inner |> Map.exists (fun _ -> List.contains subjectId)

    member this.AddPick(participant: Participant.Id, subjectId: Subject.Id) : Picks =
        let updatedMap =
            this.Inner
            |> Map.change participant (function
                | Some xs -> Some(subjectId :: xs)
                | None -> Some [ subjectId ])

        Picks(updatedMap)

    member this.NextParticipant
        (order: Order.OrderOption, participants: Participant.Id list, turn: Participant.Id, random: IRandom)
        : Participant.Id =
        let roundIdx = this.CurrentRound(participants)

        let inRound =
            match order with
            | Order.OrderOption.RandomSeed seed -> random.ShuffleList (Convert.ToInt32 seed + roundIdx) participants
            | _ -> participants

        let idx = inRound |> List.findIndex ((=) turn)

        match order with
        | Order.OrderOption.Classic
        | Order.OrderOption.RandomSeed _ -> inRound.[(idx + 1) % inRound.Length]
        | Order.OrderOption.Snake ->
            let picksInRound = participants.Length + 1
            let revRound = this.Total() / picksInRound
            let dir = if revRound % 2 = 0 then 1 else -1
            let edge = if dir = 1 then participants.Length - 1 else 0
            if idx = edge then inRound.[idx] else inRound.[idx + dir]

    static member Empty(participants: Participant.Id list) : Picks =
        participants |> List.map (fun p -> p, []) |> Map.ofList |> Picks
