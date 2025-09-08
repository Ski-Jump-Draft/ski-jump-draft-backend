using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Spectre.Console;
using System.Text.Json;
using App.Application.Messaging.Notifiers;
using App.Application.Utility;
using App.Infrastructure.Utility.Logger;
using Microsoft.Extensions.Logging;

namespace Playground.Game.CompetitionView;

class Program(IMyLogger logger)
{
    private static IMyLogger logger;

    static async Task Main()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Information)
                .AddConsole();
        });

        logger = new Dotnet(loggerFactory);
        var listener = new TcpListener(IPAddress.Loopback, 12345);
        listener.Start();
        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            await using var stream = client.GetStream();
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var dto = JsonSerializer.Deserialize<GameUpdatedDto>(line);
                    if (dto is null) continue;

                    if (ShouldClearConsole(dto))
                    {
                        AnsiConsole.Clear();
                    }

                    AnsiConsole.MarkupLine($"[bold]{GetCompetitionTitle(dto)}[/]");
                    AnsiConsole.Write(BuildTable(ExtractCompetitionDto(dto)));
                }
                catch (Exception error)
                {
                    AnsiConsole.WriteLine("ERROR: " + error.ToString());
                    /* ignoruj błędy deserializacji */
                }
            }

            // var line = Console.ReadLine();
            // if (string.IsNullOrEmpty(line)) continue;
            //
            // try
            // {
            //     var dto = JsonSerializer.Deserialize<GameUpdatedDto>(line);
            //     if (dto is null) continue;
            //
            //     var competitionDto = ExtractCompetitionDto(dto);
            //     AnsiConsole.Clear();
            //     AnsiConsole.MarkupLine($"[bold]{GetCompetitionTitle(dto)}[/]");
            //     AnsiConsole.Write(BuildTable(competitionDto));
            // }
            // catch
            // {
            //     /* ignoruj błędy deserializacji */
            // }
        }
    }

    private static CompetitionDto ExtractCompetitionDto(GameUpdatedDto gameUpdatedDto)
    {
        Console.WriteLine($"Status: {gameUpdatedDto.Status}, NotInCompetitionPhaseButHaveLastCompetitionState: {
            NotInCompetitionPhaseButHaveLastCompetitionState(gameUpdatedDto)} — {GetCompetitionTitle(gameUpdatedDto)}");
        Console.WriteLine($"Last competition state: {gameUpdatedDto.LastCompetitionState?.ToString() ?? "NONE"}");
        return gameUpdatedDto.Status switch
        {
            "PreDraft" => gameUpdatedDto.PreDraft!.Competition!,
            "MainCompetition" => gameUpdatedDto.MainCompetition!,
            _ when NotInCompetitionPhaseButHaveLastCompetitionState(gameUpdatedDto) => gameUpdatedDto
                .LastCompetitionState!,
            _ => throw new InvalidOperationException($"Unknown Game.Status case '{gameUpdatedDto.Status}'.")
        };
    }

    private static string GetCompetitionTitle(GameUpdatedDto gameUpdatedDto)
    {
        return gameUpdatedDto.Status switch
        {
            "PreDraft" => $"Faza obserwacji (trening nr. {gameUpdatedDto.PreDraft!.Index + 1})",
            "MainCompetition" => "Konkurs główny",
            "Break Draft" when NotInCompetitionPhaseButHaveLastCompetitionState(gameUpdatedDto) =>
                $"Faza obserwacji (trening nr. {gameUpdatedDto.PreDraftsCount
                })", // Draft jest po ostatnim konkursie fazy obserwacji
            "Break Ended" when NotInCompetitionPhaseButHaveLastCompetitionState(gameUpdatedDto) =>
                "Konkurs główny", // Zakończenie jest po konkursie głównym
            _ => throw new InvalidOperationException($"Unknown Game.Status case '{gameUpdatedDto.Status}'.")
        };
    }

    private static bool ShouldClearConsole(GameUpdatedDto gameUpdatedDto)
    {
        return gameUpdatedDto.Status switch
        {
            "PreDraft" => true,
            "MainCompetition" => true,
            _ when NotInCompetitionPhaseButHaveLastCompetitionState(gameUpdatedDto) => true,
            _ => false
        };
    }

    private static bool NotInCompetitionPhaseButHaveLastCompetitionState(GameUpdatedDto gameUpdatedDto)
    {
        return gameUpdatedDto.Status switch
        {
            not "PreDraft" and not "MainCompetition" when gameUpdatedDto.LastCompetitionState is not null => true,
            _ => false
        };
    }

    private static Table BuildTable(CompetitionDto competitionDto)
    {
        Console.WriteLine("Building a ");
        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Rank");
        table.AddColumn("Bib");
        table.AddColumn("Jumper");
        table.AddColumn("Country");
        table.AddColumn("Distance 1");
        table.AddColumn("Points 1");
        table.AddColumn("Distance 2");
        table.AddColumn("Points 2");
        table.AddColumn("Total");

        foreach (var competitionResult in competitionDto.Results)
        {
            var roundsCount = competitionResult.Rounds.Count;
            var distance1 = competitionResult.Rounds[0].Distance;
            var points1 = competitionResult.Rounds[0].Points;
            double? distance2 = roundsCount > 1 ? competitionResult.Rounds[1].Distance : null;
            double? points2 = roundsCount > 1 ? competitionResult.Rounds[1].Points : null;
            var totalPoints = competitionResult.Total;
            table.AddRow(
                competitionResult.Rank.ToString(CultureInfo.InvariantCulture),
                competitionResult.Bib.ToString(),
                $"{competitionResult.Jumper.Name} {competitionResult.Jumper.Surname}",
                competitionResult.Jumper.CountryFisCode,
                $"{distance1:F1}",
                $"{points1:F1}",
                $"{(distance2.HasValue ? distance2.Value.ToString("F1") : "---")}",
                $"{(points2.HasValue ? points2.Value.ToString("F1") : "---")}",
                $"{totalPoints:F1}"
            );
        }

        return table;
    }
}