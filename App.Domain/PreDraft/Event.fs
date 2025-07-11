module App.Domain.PreDraft.Event

open App.Domain
open App.Domain.PreDraft.Competitions
open App.Domain.PreDraft.Phase
open App.Domain.PreDraft.Settings

[<Struct; CLIMutable>]
type PreDraftCreatedV1 =
    { PreDraftId: Id.Id
      Settings: Settings }

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
    | PreDraftCreatedV1 of PreDraftCreatedV1
    | PreDraftStartedV1 of PreDraftStartedV1
    | CompetitionStartedV1 of CompetitionStartedV1
    | CompetitionEndedV1 of CompetitionEndedV1
    | PreDraftEndedV1 of PreDraftEndedV1

module Versioning =
    let schemaVersion =
        function
        | PreDraftCreatedV1 _ -> 1us
        | PreDraftStartedV1 _ -> 1us
        | CompetitionStartedV1 _ -> 1us
        | CompetitionEndedV1 _ -> 1us
        | PreDraftEndedV1 _ -> 1us
