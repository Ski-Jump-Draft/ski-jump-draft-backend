namespace App.Domain.Competitions

module Startlist =
    type Error =
        | StartlistEmpty
        | AlreadyCompleted of Participant.IndividualId
        | AlreadyOmitted of Participant.IndividualId

type Startlist =
    private
        { Order: Participant.IndividualId list
          Completed: Set<Participant.IndividualId>
          //Omitted: Set<Participant.Id> }
    }

    static member Create(participantsOrder: Participant.IndividualId list) =
        if participantsOrder.IsEmpty then
            Error(Startlist.Error.StartlistEmpty)
        else
            Ok
                { Order = participantsOrder
                  Completed = Set.empty }
                  //Omitted = Set.empty }
    
    member this.CompletedSet : Set<Participant.IndividualId> =
        this.Completed
      
    member this.NoOneHasCompleted =
        this.Completed |> Set.isEmpty

    member this.Next : Participant.IndividualId option =
        this.Order
        |> List.tryFind (fun pid -> not (Set.contains pid this.Completed))

    // member this.HasOmitted participantId =
    //     this.Omitted |> Set.contains participantId
    //
    // member this.Omit participantId =
    //     if this.HasOmitted participantId then
    //         Error(Startlist.Error.AlreadyOmitted participantId)
    //     else
    //         Ok(
    //             { this with
    //                 Omitted = this.Omitted |> Set.add participantId }
    //         )

    member this.HasCompleted participantId =
        this.Completed |> Set.contains participantId

    member this.Complete participantId =
        if this.HasCompleted participantId then
            Error(Startlist.Error.AlreadyCompleted participantId)
        else
            Ok(
                { this with
                    Completed = this.Completed |> Set.add participantId }
            )
