module App.Domain.Competition.Grouping

type Group = Group of CompetitionEntityId list

type IGroupingStrategy =
    abstract CreateGroups: CompetitionEntityId list -> Group
