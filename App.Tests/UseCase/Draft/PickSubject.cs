using App.Application.UseCase.Draft.PickSubject;
using App.Domain.Draft;
using App.Domain.Draft.Order;
using App.Domain.Repositories;
using App.Domain.Shared;
using App.Infrastructure.GuidAdapters;
using App.Tests.Fakes.Factory;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Moq;

namespace App.Tests.UseCase.Draft;

public class PickSubjectHandlerTests
{
    [Fact]
    public async Task PickSubjectHandler_should_pick_subject_and_save_event()
    {
        var draftId = App.Domain.Draft.Id.Id.NewId(Guid.NewGuid());
        var participantId = Participant.Id.NewId(Guid.NewGuid());
        var participant =
            new Participant.Participant(participantId, Participant.NameModule.tryCreate("Guest#213").ResultValue);

        var subjectId = Subject.Id.NewId(Guid.NewGuid());
        var subjectIdentity = new Subject.Jumper(Subject.JumperModule.Name.NewName("Kamil"),
            Subject.JumperModule.Surname.NewSurname("Stoch"), CountryCodeModule.tryCreate("POL").Value);
        var subject = new Subject.Subject(subjectId, Subject.Identity.NewJumper(subjectIdentity));

        var draft = FakeDraftFactory.CreateInRunningPhase(draftId, [participantId], Order.Classic);

        var draftRepo = new Mock<IDraftRepository>();
        draftRepo.Setup(x => x.LoadAsync(draftId, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(FSharpOption<App.Domain.Draft.Draft>.Some(draft)));
        draftRepo.Setup(x => x.SaveAsync(It.IsAny<App.Domain.Draft.Draft>(),
                It.IsAny<FSharpList<Event.DraftEventPayload>>(),
                It.IsAny<AggregateVersion.AggregateVersion>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var draftParticipantRepo = new Mock<IDraftParticipantRepository>();
        draftParticipantRepo.Setup(x => x.GetByIdAsync(participantId))
            .Returns(Task.FromResult(FSharpOption<Participant.Participant>.Some(participant)));

        var draftSubjectRepo = new Mock<IDraftSubjectRepository>();
        draftSubjectRepo.Setup(x => x.GetByIdAsync(subjectId))
            .Returns(Task.FromResult(FSharpOption<Subject.Subject>.Some(subject)));

        var handler = new Handler(draftRepo.Object, draftParticipantRepo.Object, draftSubjectRepo.Object,
            new SystemGuid());

        await handler.HandleAsync(new Command(draftId, participantId, subjectId), CancellationToken.None);

        draftRepo.Verify(repository =>
                repository.SaveAsync(
                    It.IsAny<App.Domain.Draft.Draft>(),
                    It.IsAny<FSharpList<Event.DraftEventPayload>>(),
                    It.IsAny<AggregateVersion.AggregateVersion>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }
}