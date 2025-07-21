using App.Application.Abstractions;
using App.Application.UseCase.Game.Exception;
using App.Domain.Matchmaking;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Controller;

[ApiController]
[Route("quickGame")]
public class QuickGameController(ICommandBus commandBus) : ControllerBase
{
    /// <summary>
    /// Quick Join na potrzeby MVP.
    /// Istnieje jedna globalna gra lub nie istnieją żadne. Jeśli nie istnieje, spróbuj stworzyć "na konto" globalnego hosta.
    /// </summary>
    /// <param name="matchmakingDto"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpPost("joinMatchmaking")]
    public async Task<IActionResult> Join([FromBody] JoinMatchmakingDto matchmakingDto, CancellationToken ct)
    {
        var findOrCreateCommand =
            new Application.UseCase.Game.QuickMatchmaking.FindOrCreate.Command(matchmakingDto.Nick);
        var matchmakingId =
            await commandBus.SendAsync<Application.UseCase.Game.QuickMatchmaking.FindOrCreate.Command, Guid>(
                findOrCreateCommand, ct);
        var joinCommand =
            new Application.UseCase.Game.QuickMatchmaking.Join.Command(matchmakingId, matchmakingDto.Nick);
        var matchmakingParticipantId =
            await commandBus
                .SendAsync<Application.UseCase.Game.QuickMatchmaking.Join.Command,
                    App.Domain.Matchmaking.ParticipantModule.Id>(
                    joinCommand, ct);

        return Ok(new { matchmakingId, participantId = matchmakingParticipantId });
    }

    [HttpPost("leaveMatchmaking")]
    public async Task<IActionResult> Quit([FromBody] LeaveMatchmakingDto dto, CancellationToken ct)
    {
        var matchmakingId = Id.NewId(dto.MatchmakingId);
        var matchmakingParticipantId = ParticipantModule.Id.NewId(dto.MatchmakingParticipantId);
        var leaveMatchmakingCommand =
            new Application.UseCase.Game.Matchmaking.Leave.Command(matchmakingId, matchmakingParticipantId);
        try
        {
            await commandBus.SendAsync(leaveMatchmakingCommand, ct);
            return Ok();
        }
        catch (MatchmakingParticipantNotInMatchmakingException)
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
}

public record JoinMatchmakingDto(string Nick);

public record LeaveMatchmakingDto(Guid MatchmakingId, Guid MatchmakingParticipantId);