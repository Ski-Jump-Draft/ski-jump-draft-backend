namespace App.Domain.SimpleCompetition

module Hill =
    type Id = Id of System.Guid

    type KPoint = private KPoint of double

    module KPoint =
        let tryCreate (v: double) =
            if v >= 0.0 then Some(KPoint v) else Option.None

        let value (KPoint v) = v

    type HsPoint = private HsPoint of double

    module HsPoint =
        let tryCreate (v: double) =
            if v >= 0.0 then Some(HsPoint v) else Option.None

        let value (HsPoint v) = v

    type GatePoints = private GatePoints of double

    module GatePoints =
        let tryCreate (v: double) =
            if v > 0 then Some(GatePoints v) else Option.None

        let value (GatePoints v) = v

    type WindPoints = private WindPoints of double

    module WindPoints =
        let tryCreate (v: double) =
            if v > 0 then Some(WindPoints v) else Option.None

        let value (WindPoints v) = v

    type Kind =
        | Small
        | Medium
        | Normal
        | Large
        | Big
        | SkiFlying

        static member FromHs(hsPoint: HsPoint) =
            match HsPoint.value hsPoint with
            | v when v < 50.0 -> Small
            | v when v < 85.0 -> Medium
            | v when v < 110.0 -> Normal
            | v when v < 150.0 -> Large
            | v when v < 200.0 -> Big
            | _ -> SkiFlying

type Hill =
    { Id: Hill.Id
      KPoint: Hill.KPoint
      HsPoint: Hill.HsPoint
      GatePoints: Hill.GatePoints
      HeadwindPoints: Hill.WindPoints
      TailwindPoints: Hill.WindPoints }

    member this.Kind = Hill.Kind.FromHs this.HsPoint
