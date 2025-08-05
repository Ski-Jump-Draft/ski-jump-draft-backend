namespace App.Domain.GameWorld

open System.Collections.Generic
open App.Domain.GameWorld.Event

open App.Domain.GameWorld.HillTypes.Record
open HillTypes

module Hill =
    type Error =
        | InGameRecordIsSimpleReference
        | HsPointNotGreaterThanKPoint of KPoint: double * HsPoint: double
// | InvalidGeometryUpdate of KPointOpt: KPoint option * HsPointOpt: HsPoint option * Message: string

open Hill

type Hill =
    private
        { Id: HillTypes.Id
          Status: Status
          Location: Location
          Name: Name
          CountryId: Country.Id
          KPoint: KPoint
          HsPoint: HsPoint
          RealRecords: HillTypes.RealRecords
          InGameRecords: HillTypes.InGameRecords }

    member this.Id_ = this.Id
    member this.Status_ = this.Status
    member this.Location_ = this.Location
    member this.Name_ = this.Name
    member this.CountryId_ = this.CountryId
    member this.KPoint_ = this.KPoint
    member this.HsPoint_ = this.HsPoint
    member this.RealRecords_ = this.RealRecords
    member this.InGameRecords_ = this.InGameRecords

    static member Create
        id
        status
        location
        name
        countryId
        kPoint
        hsPoint
        realRecords
        inGameRecords
        : Result<Hill * Event.HillEventPayload list, Error> =
        if KPoint.value kPoint > HsPoint.value hsPoint then
            Error(Error.HsPointNotGreaterThanKPoint(KPoint.value kPoint, HsPoint.value hsPoint))
        else
            let state =
                { Id = id
                  Status = status
                  Location = location
                  Name = name
                  CountryId = countryId
                  KPoint = kPoint
                  HsPoint = hsPoint
                  RealRecords = realRecords
                  InGameRecords = inGameRecords }

            let event: Event.HillCreatedV1 =
                { HillId = id
                  Status = status
                  Location = location
                  Name = name
                  CountryId = countryId
                  KPoint = kPoint
                  HsPoint = hsPoint
                  RealRecords = realRecords
                  InGameRecords = inGameRecords }

            Ok(state, [ Event.HillEventPayload.HillCreatedV1 event ])

    member this.TryUpdateInGameRecords
        (day: Record.Day)
        (distance: Record.Distance)
        (setter: Record.Setter)
        : Hill * HillEventPayload list =

        let dateTime = Day.value day

        let month =
            HillTypes.Record.Month.Create dateTime.Month dateTime.Year
            |> Option.defaultWith (fun () -> failwith "Invalid month number")

        let currentInGameRecords = this.InGameRecords
        let newRecord: HillTypes.Record = { Setter = setter; Distance = distance }

        let mutable globalChanged = false
        let mutable dailyChanged = false
        let mutable monthlyChanged = false

        // Global
        let updatedGlobalRecord =
            match currentInGameRecords.Global with
            | Some existingGlobalRecord when existingGlobalRecord.Distance >= distance -> currentInGameRecords.Global
            | _ ->
                globalChanged <- true
                Some newRecord

        // Daily
        let updatedDailyDictionary =
            Dictionary<Record.Day, Record>(currentInGameRecords.Daily)

        match updatedDailyDictionary.TryGetValue day with
        | true, existingDailyRecord when existingDailyRecord.Distance >= distance -> ()
        | _ ->
            updatedDailyDictionary.[day] <- newRecord
            dailyChanged <- true

        // Monthly
        let updatedMonthlyDictionary =
            Dictionary<Record.Month, Record>(currentInGameRecords.Monthly)

        match updatedMonthlyDictionary.TryGetValue month with
        | true, existingMonthlyRecord when existingMonthlyRecord.Distance >= distance -> ()
        | _ ->
            updatedMonthlyDictionary.[month] <- newRecord
            monthlyChanged <- true

        let updatedInGameRecords: HillTypes.InGameRecords =
            { Global = updatedGlobalRecord
              Daily = updatedDailyDictionary
              Monthly = updatedMonthlyDictionary }

        let updatedHill: Hill =
            { this with
                InGameRecords = updatedInGameRecords }

        let events =
            if globalChanged || dailyChanged || monthlyChanged then
                let gameWorldJumperId =
                    match setter with
                    | GameWorldJumper jumperId -> jumperId
                    | Simple _ -> failwith "Global in-game records require a GameWorldJumper setter"

                [ Event.HillEventPayload.HillInGameRecordUpdatedV1
                      { HillId = this.Id_
                        Day = day
                        Month = month
                        GameWorldJumperId = gameWorldJumperId
                        Distance = distance.Value
                        GlobalChanged = globalChanged
                        DailyChanged = dailyChanged
                        MonthlyChanged = monthlyChanged } ]
            else
                []

        updatedHill, events
