module App.Domain.Draft.Event

open App.Domain
// open App.Domain.Draft

[<Struct; CLIMutable>]
type DraftStartedV1 =
    { DraftId: Draft.Id.Id
      Settings: Draft.Settings.Settings
      Participants: Draft.Participant.Id
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

// [<Struct; CLIMutable>]
// type DraftStartedV1 =
//     { Header: EventHeader
//       DraftId: Draft.Id.Id }
//
// type DraftStarted = DraftStartedV1
//
// [<Struct; CLIMutable>]
// type DraftEndedV1 =
//     { Header: EventHeader
//       DraftId: Draft.Id.Id }
//
// type DraftEnded = DraftEndedV1
//
// [<Struct; CLIMutable>]
// type DraftSubjectPickedV1 =
//     { Header: EventHeader
//       DraftId: Draft.Id.Id
//       ParticipantId: Participant.Id
//       SubjectId: Subject.Id }
//
// [<Struct; CLIMutable>]
// type DraftSubjectPickedV2 =
//     { Header: EventHeader
//       DraftId: Draft.Id.Id
//       ParticipantId: Participant.Id
//       SubjectId: Subject.Id
//       PickIndex: uint } // Np. czwarty pick danego gracza
//
// type DraftSubjectPicked = DraftSubjectPickedV2
//
// module DraftEventFactory =
//     let startedV1 eventId occuredAt corr caus draftId : DraftStarted =
//         { Header = EventHeader.create eventId 1us occuredAt corr caus
//           DraftId = draftId }
//
//     let subjectPickedV1 eventId occuredAt corr caus draftId participantId subjectId : DraftSubjectPickedV1 =
//         { Header = EventHeader.create eventId 1us occuredAt corr caus
//           DraftId = draftId
//           ParticipantId = participantId
//           SubjectId = subjectId }
//
//     let subjectPickedV2 eventId occuredAt corr caus draftId participantId subjectId pickIndex : DraftSubjectPickedV2 =
//         { Header = EventHeader.create eventId 2us occuredAt corr caus
//           DraftId = draftId
//           ParticipantId = participantId
//           SubjectId = subjectId
//           PickIndex = pickIndex }
