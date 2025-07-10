module App.Domain.Competition.Grouping.Abstractions

open App.Domain.Competition

type IGroupingStrategy =
    abstract CreateGroups: CompetitionEntityId list -> Group
