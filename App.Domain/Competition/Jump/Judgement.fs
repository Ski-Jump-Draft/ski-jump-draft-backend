module App.Domain.Competition.Jump.Judgement

open System

[<CustomComparison; CustomEquality>]
type JudgeMark =
    private
    | JudgeMark of double

    member this.Value =
        let (JudgeMark v) = this
        v

    // comparison + equality
    static member op_LessThan(JudgeMark a, JudgeMark b) = a < b
    static member op_LessThanOrEqual(JudgeMark a, JudgeMark b) = a <= b
    static member op_GreaterThan(JudgeMark a, JudgeMark b) = a > b
    static member op_GreaterThanOrEqual(JudgeMark a, JudgeMark b) = a >= b
    static member op_Equality(JudgeMark a, JudgeMark b) = a = b
    static member op_Inequality(JudgeMark a, JudgeMark b) = a <> b


    interface System.IComparable with
        member this.CompareTo other =
            match other with
            | :? JudgeMark as jm -> compare this.Value jm.Value
            | _ -> invalidArg "other" "Not a JudgeMark"
            
    interface IEquatable<JudgeMark> with
        member this.Equals other = this.Value = other.Value
        
    override this.Equals obj =
        match obj with
        | :? JudgeMark as jm -> this.Value = jm.Value
        | _ -> false

    override this.GetHashCode() = hash this.Value


module JudgeMark =
    type Error = LessThanZero of Mark: double

    let tryCreate v =
        if v < 0.0 then Error(LessThanZero v) else Ok(JudgeMark v)

    let value (JudgeMark v) = v

type JudgeMarksList = JudgeMarksList of JudgeMark list
