namespace App.Domain.GameWorld

open System.Collections
open System.Collections.Generic

module HillTypes =
    type Id = Id of System.Guid

    [<Struct>]
    type KPoint = private KPoint of double

    module KPoint =
        let tryCreate (v: double) =
            if v >= 0.0 then Some(KPoint v) else None

        let value (KPoint v) = v

    [<Struct>]
    type HsPoint = private HsPoint of double

    module HsPoint =
        let tryCreate (v: double) =
            if v >= 0.0 then Some(HsPoint v) else None

        let value (HsPoint v) = v

    module Record =
        type Setter =
            | Simple of string
            | GameWorldJumper of JumperId: JumperTypes.Id

        [<Struct>]
        type Distance =
            private
            | Distance of double

            member this.Value = let (Distance v) = this in v

            static member op_LessThan(Distance a, Distance b) = a < b
            static member op_GreaterThan(Distance a, Distance b) = a > b
            static member op_Equality(Distance a, Distance b) = a = b


        module Distance =
            type Error = ZeroOrBelow of Value: double

            let tryCreate (v: double) =
                if v > 0 then Ok(Distance v) else Error(ZeroOrBelow v)

            let value (Distance v) = v

        type Day = Day of System.DateTime

        module Day =
            let value (Day v) = v

        type Month =
            private
                { Number: int
                  Year: int }

            member this.Number_ = this.Number
            member this.Year_ = this.Year

            static member Create number year =
                if number < 1 || number > 12 then
                    Some { Number = number; Year = year }
                else
                    None


    type Record =
        { Setter: Record.Setter
          Distance: Record.Distance }

    type RealRecords = { Summer: Record option; Winter: Record option }

    type InGameRecords =
        { Global: Record option
          Daily: Dictionary<Record.Day, Record>
          Monthly: Dictionary<Record.Month, Record> }

        static member Empty =
            { Global = None
              Daily = Dictionary<_, _>()
              Monthly = Dictionary<_, _>() }

    [<Struct>]
    type Name = Name of string

    [<Struct>]
    type Location = Location of string

    type Status =
        | Operational
        | Retired
