namespace App.Domain.Time

open System

type IClock =
    abstract member Now: DateTimeOffset
    abstract member UtcNow: DateTime