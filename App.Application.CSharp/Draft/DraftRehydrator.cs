using App.Domain.Draft;
using App.Domain.Shared.EventHelpers;

namespace App.Application.CSharp.Draft;

public static class DraftRehydrator
{
    public static App.Domain.Draft.Draft Rehydrate(
        IEnumerable<DomainEvent<Event.DraftEventPayload>> stream)
    {
        
        var draft = Domain.Draft.Draft.Create();

        // 1) składasz stan przez fold/apply
        foreach (var de in stream)
        {
            draft = de.Payload switch
            {
                Event.DraftEventPayload.DraftStartedV1 p => draft.ApplyStarted(p),
                Event.DraftEventPayload.DraftSubjectPickedV2 p => draft.ApplyPicked(p),
                Event.DraftEventPayload.DraftEndedV1 p => draft.ApplyEnded(p),
                _ => throw new InvalidOperationException("Nieznany payload")
            };
        }

        return draft;
    }
}