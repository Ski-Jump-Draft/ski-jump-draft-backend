using App.Domain.Draft;
using App.Domain.SimpleCompetition;

namespace App.Application.Abstractions;

public interface ICompetitorToDraftSubjectMapper
{
    Subject.Id? TryGetSubjectId(CompetitorModule.Id competitorId);
}