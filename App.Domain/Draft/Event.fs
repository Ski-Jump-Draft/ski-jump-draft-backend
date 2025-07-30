module App.Domain.Draft.Event

open System
open App.Domain

type DraftParticipantDto = { Id: Participant.Id }

type DraftSubjectJumperDto =
    { Name: Subject.Jumper.Name
      Surname: Subject.Jumper.Surname
      CountryCode: Subject.CountryCode }

type DraftSubjectTeamDto =
    { Name: Subject.Team.Name
      CountryCode: Subject.CountryCode }

type DraftSubjectIdentityDto =
    | Jumper of DraftSubjectJumperDto
    | Team of DraftSubjectTeamDto

type DraftSubjectDto =
    { Id: Subject.Id
      Identity: DraftSubjectIdentityDto }

type DraftSettingsDto =
    { Order: Order.Order
      MaxJumpersPerPlayer: uint
      UniqueJumpers: bool
      PickTimeout: Picks.PickTimeout }

[<Struct; CLIMutable>]
type DraftCreatedV1 =
    { DraftId: Draft.Id.Id
      Settings: DraftSettingsDto
      Participants: DraftParticipantDto list
      Subjects: DraftSubjectDto list
      Seed: uint64 }

[<Struct; CLIMutable>]
type DraftStartedV1 = { DraftId: Draft.Id.Id }

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
    | DraftCreatedV1 of DraftCreatedV1
    | DraftSubjectPickedV1 of DraftSubjectPickedV1
    | DraftSubjectPickedV2 of DraftSubjectPickedV2
    | DraftStartedV1 of DraftStartedV1
    | DraftEndedV1 of DraftEndedV1

module Versioning =
    let schemaVersion =
        function
        | DraftCreatedV1 _ -> 1us
        | DraftStartedV1 _ -> 1us
        | DraftSubjectPickedV1 _ -> 1us
        | DraftSubjectPickedV2 _ -> 2us
        | DraftEndedV1 _ -> 1us
