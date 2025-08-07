module App.Domain.Game.Participant

open System

type Id = Id of System.Guid
type Nick = private Nick of string

module Nick =
    type Error = InvalidLength of Length: int * Range: (int * int)

    let tryCreate (v: string) =
        if v.Length > 20 then
            Error(InvalidLength(v.Length, (3, 20)))
        else
            Ok(Nick v)

    let value (Nick v) = v

type Participant = { Id: Id; Nick: Nick }

type Participants = private Participants of Participant list

module Participants =
    let from (participants: Participant list) = Participants participants
    let empty = Participants []

    // let add participant (Participants participants) =
    //     if List.contains participant participants then
    //         Error(ParticipantAlreadyJoined participant)
    //     else
    //         Ok(Participants(participant :: participants))

    let removeById id (Participants participants) =
        Participants(List.filter (fun p -> p.Id <> id) participants)

    let contains participant (Participants participants) = List.contains participant participants

    let count (Participants participants) : uint = uint (List.length participants)
    let value (Participants participants) = participants
