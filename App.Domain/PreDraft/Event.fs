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
type PreDraftCompetitionStartedV1 =
    { PreDraftId: Id.Id
      Index: CompetitionIndex
      CompetitionId: Competition.Id }

[<Struct; CLIMutable>]
type PreDraftEndedV1 = { PreDraftId: Id.Id }

type PreDraftEventPayload =
    | PreDraftCreatedV1 of PreDraftCreatedV1
    | PreDraftCompetitionStartedV1 of PreDraftCompetitionStartedV1
    | PreDraftEndedV1 of PreDraftEndedV1

module Versioning =
    let schemaVersion =
        function
        | PreDraftCreatedV1 _ -> 1us
        | PreDraftCompetitionStartedV1 _ -> 1us
        | PreDraftEndedV1 _ -> 1us
