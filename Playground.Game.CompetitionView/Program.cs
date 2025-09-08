namespace Playground.Game.CompetitionView;

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Spectre.Console;
using App.Application.Messaging.Notifiers;

class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length < 1 || !Guid.TryParse(args[0], out var gameId))
        {
            Console.WriteLine("Usage: dotnet run -- <gameId>");
            return 1;
        }

        var hubUrl = "http://127.0.0.1:5150/game/hub"; // dopasuj adres WebAPI
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        var renderLock = new object();

        connection.On<GameUpdatedDto>("GameUpdated", dto =>
        {
            // uruchom render synchronicznie, blokując wątki, bo AnsiConsole nie jest thread-safe
            lock (renderLock)
            {
                try
                {
                    var compDto = ExtractCompetitionDto(dto);
                    if (ShouldClearConsole(dto)) AnsiConsole.Clear();
                    AnsiConsole.MarkupLine($"[bold]{GetCompetitionTitle(dto)}[/]");
                    AnsiConsole.Write(BuildTable(compDto));
                }
                catch (Exception ex)
                {
                    // log minimalnie na stdout — SignalR klient jest w tym samym procesie
                    Console.Error.WriteLine($"Render error: {ex.Message}");
                }
            }
        });

        await connection.StartAsync();
        await connection.InvokeAsync("JoinGame", gameId);

        Console.CancelKeyPress += async (_, e) =>
        {
            e.Cancel = true;
            try
            {
                await connection.InvokeAsync("LeaveGame", gameId);
            }
            catch
            {
            }

            await connection.StopAsync();
            Environment.Exit(0);
        };

        Console.WriteLine($"Connected to {hubUrl}, joined game {gameId}. Waiting for updates...");

        await Task.Delay(Timeout.Infinite);
        return 0;
    }

    // ------------ helpers: copy your existing implementations ------------
    private static CompetitionDto ExtractCompetitionDto(GameUpdatedDto dto)
    {
        return dto.Status switch
        {
            "PreDraft" => dto.PreDraft!.Competition!,
            "MainCompetition" => dto.MainCompetition!,
            _ when NotInCompetitionPhaseButHaveLastCompetitionState(dto) => dto.LastCompetitionState!,
            _ => throw new InvalidOperationException($"Unknown Game.Status case '{dto.Status}'.")
        };
    }

    private static bool NotInCompetitionPhaseButHaveLastCompetitionState(GameUpdatedDto dto)
        => dto.Status != "PreDraft" && dto.Status != "MainCompetition" && dto.LastCompetitionState is not null;

    private static bool ShouldClearConsole(GameUpdatedDto dto)
        => dto.Status == "PreDraft" || dto.Status == "MainCompetition" ||
           NotInCompetitionPhaseButHaveLastCompetitionState(dto);

    private static string GetCompetitionTitle(GameUpdatedDto dto)
    {
        return dto.Status switch
        {
            "PreDraft" => $"Faza obserwacji (trening nr. {dto.PreDraft!.Index + 1})",
            "MainCompetition" => "Konkurs główny",
            _ when NotInCompetitionPhaseButHaveLastCompetitionState(dto) =>
                dto.Status.StartsWith("Break")
                    ? (dto.Status.Contains("Draft")
                        ? $"Faza obserwacji (trening nr. {dto.PreDraftsCount})"
                        : "Konkurs główny")
                    : "Konkurs (ostatni znany stan)",
            _ => throw new InvalidOperationException($"Unknown Game.Status case '{dto.Status}'.")
        };
    }

    private static Spectre.Console.Table BuildTable(CompetitionDto competitionDto)
    {
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

        foreach (var r in competitionDto.Results)
        {
            var rounds = r.Rounds;
            var d1 = rounds.Count > 0 ? rounds[0].Distance : 0.0;
            var p1 = rounds.Count > 0 ? rounds[0].Points : 0.0;
            double? d2 = rounds.Count > 1 ? rounds[1].Distance : null;
            double? p2 = rounds.Count > 1 ? rounds[1].Points : null;
            table.AddRow(
                r.Rank.ToString(CultureInfo.InvariantCulture),
                r.Bib.ToString(),
                $"{r.Jumper.Name} {r.Jumper.Surname}",
                r.Jumper.CountryFisCode,
                $"{d1:F1}", $"{p1:F1}",
                $"{(d2.HasValue ? d2.Value.ToString("F1") : "---")}",
                $"{(p2.HasValue ? p2.Value.ToString("F1") : "---")}",
                $"{r.Total:F1}"
            );
        }

        return table;
    }
}