using System.Text.Json;
using App.Application.Abstractions;
using App.Application.UseCase.Game.Exception;
using App.Domain.Game;
using App.Web.Hub;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Controller;

[ApiController]
[Route("game")]
public class GameController(ICommandBus commandBus, MatchmakingNotifier notifier) : ControllerBase
{
    /// <summary>
    /// Quick Join na potrzeby MVP.
    /// Istnieje jedna globalna gra lub nie istnieją żadne. Jeśli nie istnieje, spróbuj stworzyć "na konto" globalnego hosta.
    /// </summary>
    /// <param name="dto"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpPost("join")]
    public async Task<IActionResult> Join([FromBody] JoinDto dto, CancellationToken ct)
    {
        var findOrCreateCommand = new Application.UseCase.Game.QuickGame.FindOrCreate.Command(dto.Nick);
        var gameId =
            await commandBus.SendAsync<Application.UseCase.Game.QuickGame.FindOrCreate.Command, Guid>(
                findOrCreateCommand, ct);
        var joinCommand = new Application.UseCase.Game.QuickGame.Join.Command(gameId, dto.Nick);
        var participantId =
            await commandBus.SendAsync<Application.UseCase.Game.QuickGame.Join.Command, App.Domain.Game.Participant.Id>(
                joinCommand, ct);

        return Ok(new { gameId, participantId });
    }

    [HttpPost("leave")]
    public async Task<IActionResult> Quit([FromBody] QuitDto dto, CancellationToken ct)
    {
        var gameId = Id.Id.NewId(dto.GameId);
        var participantId = Participant.Id.NewId(dto.ParticipantId);
        var leaveGameCommand = new Application.UseCase.Game.Leave.Command(gameId, participantId);
        try
        {
            await commandBus.SendAsync(leaveGameCommand, ct);
            return Ok();
        }
        catch (ParticipantNotInGameException)
        {
            return NotFound();
        }
        catch (Exception exception)
        {
            return Problem(
                detail: exception.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    [HttpGet("matchmaking")]
    public async Task Matchmaking([FromQuery] Guid gameId, CancellationToken ct)
    {
        Response.ContentType = "text/event-stream";

        await foreach (var ev in notifier.Subscribe(gameId, ct))
        {
            var json = JsonSerializer.Serialize(ev.Data);
            await Response.WriteAsync($"event: {ev.Type}\n", cancellationToken: ct);
            await Response.WriteAsync($"data: {json}\n\n", cancellationToken: ct);
            await Response.Body.FlushAsync(ct);
        }
    }
}

public record JoinDto(string Nick);

public record QuitDto(Guid GameId, Guid ParticipantId);