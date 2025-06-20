namespace App.Domain.Time

open System

type IClock =
    //abstract member Now: DateTime
    abstract member UtcNow: DateTimeOffset