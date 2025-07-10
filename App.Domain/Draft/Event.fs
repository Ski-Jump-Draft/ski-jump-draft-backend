module App.Domain.Draft.Event

open App.Domain
// open App.Domain.Draft

[<Struct; CLIMutable>]
type DraftStartedV1 =
    { DraftId: Draft.Id.Id
      Settings: Draft.Settings.Settings
      Participants: Draft.Participant.Id list
      Seed: uint64 }

[<Struct; CLIMutable>]
type DraftEndedV1 = { DraftId: Draft.Id.Id }

[<Struct; CLIMutable>]
type DraftSubjectPickedV1 =
    { DraftId: Draft.Id.Id
      ParticipantId: Participant.Id
      SubjectId: Subject.Id }

[<Struct; CLIMutable>]
type DraftSubjectPickedV2 =
    { DraftId: Draft.Id.Id
      ParticipantId: Participant.Id
      SubjectId: Subject.Id
      PickIndex: uint }

type DraftEventPayload =
    | DraftSubjectPickedV1 of DraftSubjectPickedV1
    | DraftSubjectPickedV2 of DraftSubjectPickedV2
    | DraftStartedV1 of DraftStartedV1
    | DraftEndedV1 of DraftEndedV1

module Versioning =
    let schemaVersion =
        function
        | DraftStartedV1 _ -> 1us
        | DraftSubjectPickedV1 _ -> 1us
        | DraftSubjectPickedV2 _ -> 2us
        | DraftEndedV1 _ -> 1us