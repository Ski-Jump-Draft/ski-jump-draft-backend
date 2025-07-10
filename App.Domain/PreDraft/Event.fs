module App.Domain.PreDraft.Event

open App.Domain
open App.Domain.PreDraft.Competitions
open App.Domain.PreDraft.Phase

[<Struct; CLIMutable>]
type PreDraftStartedV1 = { PreDraftId: Id.Id }

[<Struct; CLIMutable>]
type CompetitionStartedV1 =
    { PreDraftId: Id.Id
      Index: CompetitionIndex
      CompetitionId: Competition.Id }

[<Struct; CLIMutable>]
type CompetitionEndedV1 =
    { PreDraftId: Id.Id
      Index: CompetitionIndex
      CompetitionId: Competition.Id }

[<Struct; CLIMutable>]
type PreDraftEndedV1 = { PreDraftId: Id.Id }

type PreDraftEventPayload =
    | PreDraftStartedV1 of PreDraftStartedV1
    | CompetitionStartedV1 of CompetitionStartedV1
    | CompetitionEndedV1 of CompetitionEndedV1
    | PreDraftEndedV1 of PreDraftEndedV1

module Versioning =
    let schemaVersion =
        function
        | PreDraftStartedV1 _ -> 1us
        | CompetitionStartedV1 _ -> 1us
        | CompetitionEndedV1 _ -> 1us
        | PreDraftEndedV1 _ -> 1us
