using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Json;
using App.Application.Messaging.Notifiers;

namespace Playground.Game.Notifier;

public class ConsoleGameNotifier : IGameNotifier
{
    private readonly StreamWriter _writer;

    public ConsoleGameNotifier()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "gnome-terminal",
            Arguments =
                "-- bash -c \"dotnet run --project /home/konrad/programming-projects/real_apps/sj_draft/Game/Playground.Game.CompetitionView/Playground.Game.CompetitionView.csproj; exec bash\"",
            UseShellExecute = true
        };
        Process.Start(startInfo);
        
        var client = new TcpClient();
        
        const int maxAttempts = 10;
        var attempt = 0;
        while (attempt < maxAttempts)
        {
            try
            {
                client.Connect("127.0.0.1", 12345);
                break; // połączono
            }
            catch (SocketException)
            {
                Thread.Sleep(500);
                attempt++;
            }
        }
        

        if (!client.Connected)
            throw new InvalidOperationException("Nie udało się połączyć z CompetitionView.");
        _writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
    }

    public async Task GameUpdated(GameUpdatedDto dto)
    {
        var json = JsonSerializer.Serialize(dto);
        await _writer.WriteLineAsync(json);
    }

    // pozostałe metody puste
    public Task GameStartedAfterMatchmaking(Guid matchmakingId, Guid gameId) => Task.CompletedTask;
    public Task GameEnded(Guid gameId) => Task.CompletedTask;
}