namespace App.Application.Factory;

public interface IPreDraftCompetitionFactory
{
    (Domain.SimpleCompetition.Competition, IEnumerable<Domain.SimpleCompetition.Event.CompetitionEventPayload>) Create(
        Domain.PreDraft.Id.Id preDraftId);
}