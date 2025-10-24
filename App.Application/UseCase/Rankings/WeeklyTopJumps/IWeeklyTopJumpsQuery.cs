using System.Collections.Immutable;

namespace App.Application.UseCase.Rankings.WeeklyTopJumps;

public record WeeklyTopJumpDto(
    Guid GameId,
    DateTimeOffset GameCreatedAt,
    Guid HillId,
    double KPoint,
    double HsPoint,
    string HillLocation,
    string HillCountryCode,
    Guid CompetitionJumperId,
    Guid GameWorldJumperId,
    string Name,
    string Surname,
    string JumperCountryCode,
    double Distance,
    double WindAverage,
    int Gate,
    IReadOnlyList<string> DraftPlayerNicks
);

public interface IWeeklyTopJumpsQuery
{
    Task<IReadOnlyList<WeeklyTopJumpDto>> GetTop20Last7Days(CancellationToken ct);
}