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
    private static readonly HashSet<string> SeenJumpers = new();

    static async Task<int> Main(string[] args)
    {
        if (args.Length < 1 || !Guid.TryParse(args[0], out var gameId))
        {
            Console.WriteLine("Usage: dotnet run -- <gameId>");
            return 1;
        }

        const string hubUrl = "http://127.0.0.1:5150/game/hub";
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        var renderLock = new object();

        connection.On<GameUpdatedDto>("GameUpdated", dto =>
        {
            lock (renderLock)
            {
                try
                {
                    var compDto = ExtractCompetitionDto(dto);
                    var current = compDto.Results.Select(UniqueKey).ToList();
                    var newOnes = current.Where(k => !SeenJumpers.Contains(k)).ToHashSet();
                    SeenJumpers.UnionWith(current);
                    if (ShouldClearConsole(dto)) AnsiConsole.Clear();
                    AnsiConsole.MarkupLine($"[bold]{GetCompetitionTitle(dto)}[/]");
                    AnsiConsole.Write(BuildTable(compDto, newOnes));
                }
                catch (Exception ex)
                {
                    if (!ex.Message.Contains("Unknown"))
                    {
                        Console.Error.WriteLine($"Render error: {ex.Message}");
                    }
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

    private static Table BuildTable(CompetitionDto competitionDto, ISet<string> newOnes)
    {
        var t = new Table().Border(TableBorder.Rounded);
        t.AddColumn("Rank");
        t.AddColumn("Bib");
        t.AddColumn("Jumper");
        t.AddColumn("Country");
        t.AddColumn("Distance 1");
        t.AddColumn("Points 1");
        t.AddColumn("Distance 2");
        t.AddColumn("Points 2");
        t.AddColumn("Total");

        foreach (var r in competitionDto.Results)
        {
            var k = UniqueKey(r);
            var hl = newOnes.Contains(k);

            var rounds = r.Rounds;
            var d1 = rounds.Count > 0 ? rounds[0].Distance : 0.0;
            var p1 = rounds.Count > 0 ? rounds[0].Points : 0.0;
            double? d2 = rounds.Count > 1 ? rounds[1].Distance : null;
            double? p2 = rounds.Count > 1 ? rounds[1].Points : null;

            t.AddRow(
                Cell(r.Rank.ToString(CultureInfo.InvariantCulture), hl),
                Cell(r.Bib.ToString(), hl),
                Cell($"{r.Jumper.Name} {r.Jumper.Surname}", hl),
                Cell(r.Jumper.CountryFisCode, hl),
                Cell($"{d1:F1}", hl), Cell($"{p1:F1}", hl),
                Cell($"{(d2.HasValue ? d2.Value.ToString("F1") : "---")}", hl),
                Cell($"{(p2.HasValue ? p2.Value.ToString("F1") : "---")}", hl),
                Cell($"{r.Total:F1}", hl)
            );
        }

        return t;
    }

    private static string UniqueKey(CompetitionResultDto r)
        => r.Jumper.CompetitionJumperId != Guid.Empty
            ? r.Jumper.CompetitionJumperId.ToString()
            : $"{r.Jumper.Name}|{r.Jumper.Surname}|{r.Jumper.CountryFisCode}";

    private static string Cell(string text, bool hl)
        => hl ? $"[bold yellow]{Markup.Escape(text)}[/]" : Markup.Escape(text);
}