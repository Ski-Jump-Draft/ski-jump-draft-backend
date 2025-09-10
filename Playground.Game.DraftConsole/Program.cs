using System.Globalization;
using Microsoft.AspNetCore.SignalR.Client;
using Spectre.Console;
using App.Application.Messaging.Notifiers;

namespace Playground.Game.DraftConsole;

class Program
{
    static Guid _gameId, _playerId;
    static string _baseUrl = "http://127.0.0.1:5150";
    static HttpClient _http = null!;
    static HubConnection _conn = null!;
    static readonly Lock _ui = new();
    static volatile bool _promptOpen = false;
    static volatile int _lastHandledPickCount = -1;

    private static readonly Dictionary<Guid, (string Name, string Surname, string Country)> JumperCache = new();

    static CancellationTokenSource? _countdownCts;


    static async Task<int> Main(string[] args)
    {
        if (args.Length < 2 || !Guid.TryParse(args[0], out _gameId) || !Guid.TryParse(args[1], out _playerId))
        {
            Console.WriteLine("Usage: dotnet run -- <gameId> <playerId> [baseUrl]");
            return 1;
        }

        if (args.Length >= 3) _baseUrl = args[2];

        _http = new HttpClient { BaseAddress = new Uri(_baseUrl) };

        var hubUrl = $"{_baseUrl.TrimEnd('/')}/game/hub";
        _conn = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _conn.On("GameUpdated", (Func<GameUpdatedDto, Task>)HandleUpdateAsync);

        await _conn.StartAsync();
        await _conn.InvokeAsync("JoinGame", _gameId);

        AnsiConsole.MarkupLine($"Połączono z [bold]{hubUrl}[/]. Gra: [bold]{_gameId}[/]. Gracz: [bold]{_playerId}[/].");
        await Task.Delay(-1);
        return 0;
    }

    static Task HandleUpdateAsync(GameUpdatedDto dto)
    {
        lock (_ui)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold]Game {_gameId}[/]").Justify(Justify.Left));
            AnsiConsole.MarkupLine($"Status: [bold]{dto.Status}[/] • Change: {dto.ChangeType}");
        }

        if (dto.Draft is null || dto.Status != "Draft" || dto.Draft.Ended)
            return Task.CompletedTask;

        var picksCount = dto.Draft.Picks.Sum(p => p.JumperIds.Count);

        // mapy pomocnicze
        var playerNickById = dto.Header.Players.ToDictionary(p => p.PlayerId, p => p.Nick);
        var takenBy = new Dictionary<Guid, Guid>(); // jumperId -> owner playerId
        foreach (var p in dto.Draft.Picks)
        foreach (var j in p.JumperIds)
            takenBy[j] = p.PlayerId;

        // -------- NEXT PICKS (OrderPolicy + NextPlayers) --------
        var orderPolicy = dto.Draft.OrderPolicy ?? "Classic";
        var nextPlayers = dto.Draft.NextPlayers ?? [];
        int? picksUntilMe = null;
        if (nextPlayers.Count > 0)
        {
            var list = nextPlayers.ToList();
            var idx = list.IndexOf(_playerId);
            if (idx >= 0) picksUntilMe = idx; // 0 => teraz
        }

        var toShow = (orderPolicy == "Random" ? nextPlayers.Take(2) : nextPlayers.Take(10)).ToList();
        if (toShow.Count > 0)
        {
            var tt = new Table().Border(TableBorder.Rounded);
            tt.AddColumn("#");
            tt.AddColumn("Player");
            for (int i = 0; i < toShow.Count; i++)
            {
                var pid = toShow[i];
                var nick = playerNickById.TryGetValue(pid, out var n) ? n : pid.ToString();
                var you = pid == _playerId ? " [bold green](YOU)[/]" : "";
                tt.AddRow(i.ToString(CultureInfo.InvariantCulture), nick + you);
            }

            lock (_ui)
            {
                var eta = (picksUntilMe is { } k && dto.Draft.TimeoutInSeconds is { } t && t > 0)
                    ? $" (≈{k * t}s)"
                    : "";
                if (picksUntilMe is { } kVal)
                    AnsiConsole.MarkupLine(kVal == 0
                        ? "[bold green]Twoja tura![/]"
                        : $"Twoja tura za [bold]{kVal}[/] picków{eta}");

                var title = orderPolicy == "Random"
                    ? "[bold]Kolejka (losowa: bieżący + następny)[/]"
                    : "[bold]Następne picki (max 10)[/]";
                AnsiConsole.Write(new Rule(title).Justify(Justify.Left));
                AnsiConsole.Write(tt);
                AnsiConsole.WriteLine();
            }
        }
        // --------------------------------------------------------

        var draftPickOptions = dto.Draft.AvailableJumpers ?? Array.Empty<DraftPickOptionDto>();

        // aktualizuj cache nazw (dla wyszarzania)
        foreach (var o in draftPickOptions)
            JumperCache[o.GameJumperId] = (o.Name, o.Surname, o.CountryFisCode);

        // Widok: available + taken
        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("#");
        table.AddColumn("Jumper");
        table.AddColumn("Country");
        table.AddColumn("Owner");

        for (int i = 0; i < draftPickOptions.Count; i++)
        {
            var o = draftPickOptions[i];
            var ranks = (o.TrainingRanks ?? Enumerable.Empty<int>()).Any()
                ? string.Join(", ", o.TrainingRanks!)
                : "-";
            table.AddRow(
                i.ToString(CultureInfo.InvariantCulture),
                $"{o.Name} {o.Surname} [grey]({ranks})[/]",
                o.CountryFisCode,
                "[grey]-[/]"
            );
        }

        foreach (var (gameJumperId, ownerPlayerId) in takenBy)
        {
            if (draftPickOptions.Any(opt => opt.GameJumperId == gameJumperId)) continue;
            var owner = playerNickById.TryGetValue(ownerPlayerId, out var nick) ? nick : ownerPlayerId.ToString();
            var (name, surname, country) = JumperCache.TryGetValue(gameJumperId, out var info)
                ? info
                : ($"JUMPER {gameJumperId.ToString()[..8]}", "", "??");

            table.AddRow("—", $"[grey]{name} {surname}[/]", $"[grey]{country}[/]", $"[yellow]{owner}[/]");
        }

        lock (_ui) AnsiConsole.Write(table);

        // tylko mój ruch -> prompt
        if (dto.Draft.CurrentPlayerId is null || dto.Draft.CurrentPlayerId.Value != _playerId)
        {
            _countdownCts?.Cancel();
            return Task.CompletedTask;
        }

        if (_promptOpen || picksCount == _lastHandledPickCount) return Task.CompletedTask;

        var snapshotOpts = draftPickOptions.ToArray();
        var snapshotPicksCount = picksCount;

        _ = Task.Run(async () =>
        {
            _promptOpen = true;

            var timeout = dto.Draft.TimeoutInSeconds; // int?
            if (timeout is { } t && t > 0) StartCountdown(t);

            try
            {
                int? chosenIndex;
                if (timeout is { } t2 && t2 > 0)
                    chosenIndex = await ReadIndexWithTimeout(snapshotOpts.Length, TimeSpan.FromSeconds(t2));
                else
                {
                    lock (_ui) AnsiConsole.MarkupLine("[yellow]Brak limitu czasu – czekam na Twój wybór.[/]");
                    var text = await Console.In.ReadLineAsync() ?? "";
                    chosenIndex = int.TryParse(text, out var parsedIdx) &&
                                  parsedIdx >= 0 && parsedIdx < snapshotOpts.Length
                        ? parsedIdx
                        : null;
                }

                _countdownCts?.Cancel();

                if (chosenIndex is { } validIdx)
                {
                    if (snapshotPicksCount != dto.Draft.Picks.Sum(p => p.JumperIds.Count))
                    {
                        lock (_ui) AnsiConsole.MarkupLine("[yellow]Stan uległ zmianie – spróbuj ponownie.[/]");
                        return;
                    }

                    var gameJumperId = snapshotOpts[validIdx].GameJumperId;
                    var url = $"/game/{_gameId}/pick?playerId={_playerId}&jumperId={gameJumperId}";
                    var resp = await _http.PostAsync(url, new StringContent(""));

                    lock (_ui)
                    {
                        if (!resp.IsSuccessStatusCode)
                            AnsiConsole.MarkupLine($"[red]Pick failed:[/] {(int)resp.StatusCode} {resp.ReasonPhrase}");
                        else
                            AnsiConsole.MarkupLine($"[green]OK[/] → {snapshotOpts[validIdx].Name} {
                                snapshotOpts[validIdx].Surname}");
                    }

                    if (resp.IsSuccessStatusCode) _lastHandledPickCount = snapshotPicksCount + 1;
                }
                else
                {
                    if (timeout is { } t3 && t3 > 0)
                    {
                        lock (_ui) AnsiConsole.MarkupLine($"[yellow]Timeout ({t3}s).[/]");
                        _lastHandledPickCount = snapshotPicksCount + 1;
                    }
                    else
                    {
                        lock (_ui) AnsiConsole.MarkupLine("[red]Nieprawidłowy input.[/]");
                    }
                }
            }
            catch (Exception ex)
            {
                await _countdownCts?.CancelAsync()!;
                lock (_ui) AnsiConsole.MarkupLine($"[red]Input error:[/] {ex.Message}");
            }
            finally
            {
                _promptOpen = false;
            }
        });

        return Task.CompletedTask;
    }


    private static void StartCountdown(int seconds)
    {
        _countdownCts?.Cancel();
        var cts = new CancellationTokenSource();
        _countdownCts = cts;

        _ = Task.Run(async () =>
        {
            for (int s = seconds; s >= 0 && !cts.IsCancellationRequested; s--)
            {
                lock (_ui)
                {
                    Console.Write($"\rTime left: {s}s   ");
                }

                try
                {
                    await Task.Delay(1000, cts.Token);
                }
                catch
                {
                    break;
                }
            }

            lock (_ui) Console.WriteLine();
        }, cts.Token);
    }

    private static async Task<int?> ReadIndexWithTimeout(int max, TimeSpan timeout)
    {
        if (max <= 0) return null;
        lock (_ui) AnsiConsole.Markup($"\n[bold]Wybierz index (0..{max - 1}): [/]");
        var readTask = Console.In.ReadLineAsync();
        var done = await Task.WhenAny(readTask, Task.Delay(timeout));
        if (done != readTask) return null; // timeout
        var text = await readTask ?? "";
        if (!int.TryParse(text, out var idx)) return null;
        if (idx < 0 || idx >= max) return null;
        return idx;
    }
}