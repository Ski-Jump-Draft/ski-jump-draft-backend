namespace App.Domain.SimpleCompetition

type DisqualificationReason =
    | SuitViolation
    | SkiLength
    | FalseStart
    | Other of string
