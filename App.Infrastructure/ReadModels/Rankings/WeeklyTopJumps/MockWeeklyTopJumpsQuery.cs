using App.Application.UseCase.Rankings.WeeklyTopJumps;

namespace App.Infrastructure.ReadModels.Rankings.WeeklyTopJumps;

/// <summary>
/// Mock/fake repository returning pseudo-random top jump data for local/dev UI.
/// </summary>
public sealed class MockWeeklyTopJumpsQuery : IWeeklyTopJumpsQuery
{
    private static readonly string[] Names = ["Kamil", "Stefan", "Ryoyu", "Anze", "Dawid", "Peter"];
    private static readonly string[] Surnames = ["Stoch", "Kraft", "Kobayashi", "Lanisek", "Kubacki", "Prevc"];
    private static readonly string[] Countries = ["POL", "AUT", "JPN", "SLO", "GER", "NOR"];

    private static readonly string[] Locations =
        ["Zakopane", "Planica", "Bischofshofen", "Sapporo", "Lillehammer", "Lahti"];

    private static readonly Random Rng = new();

    public Task<IReadOnlyList<WeeklyTopJumpDto>> GetTop20Last7Days(CancellationToken ct)
    {
        var list = new List<WeeklyTopJumpDto>(20);
        var now = DateTimeOffset.UtcNow;

        for (int i = 0; i < 20; i++)
        {
            var name = Names[Rng.Next(Names.Length)];
            var surname = Surnames[Rng.Next(Surnames.Length)];
            var country = Countries[Rng.Next(Countries.Length)];
            var loc = Locations[Rng.Next(Locations.Length)];

            list.Add(new WeeklyTopJumpDto(
                GameId: Guid.NewGuid(),
                GameCreatedAt: now.AddHours(-Rng.Next(1, 160)),
                HillId: Guid.NewGuid(),
                KPoint: 120 + Rng.NextDouble() * 20,
                HsPoint: 135 + Rng.NextDouble() * 25,
                HillLocation: loc,
                HillCountryCode: country,
                CompetitionJumperId: Guid.NewGuid(),
                GameWorldJumperId: Guid.NewGuid(),
                Name: name,
                Surname: surname,
                JumperCountryCode: country,
                Distance: 115 + Rng.NextDouble() * 70,
                WindAverage: Math.Round(-2 + Rng.NextDouble() * 4, 2),
                Gate: Rng.Next(5, 20),
                DraftPlayerNicks: [$"User{Rng.Next(1, 100)}"]
            ));
        }

        var top = list
            .OrderByDescending(x => x.Distance)
            .ThenBy(x => x.GameCreatedAt)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<WeeklyTopJumpDto>>(top);
    }
}