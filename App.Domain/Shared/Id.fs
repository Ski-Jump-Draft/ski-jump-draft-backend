namespace App.Domain.Shared

type IGuid =
    abstract member NewGuid: unit -> System.Guid