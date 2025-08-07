using App.Application.Commanding;
using App.Application.UseCase.Game.Exception;
using App.Domain.Matchmaking;
using App.Domain.Shared;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Controller;

[ApiController]
[Route("quickGame")]
public class QuickGameController(ICommandBus commandBus, IGuid guid) : ControllerBase
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
        var joinQuickMatchmakingCommand =
            new Application.UseCase.Handlers.JoinQuickMatchmaking.Command(matchmakingDto.Nick);
        var joinQuickMatchmakingEnvelope =
            new CommandEnvelope<Application.UseCase.Handlers.JoinQuickMatchmaking.Command,
                Application.UseCase.Handlers.JoinQuickMatchmaking.Result>(joinQuickMatchmakingCommand,
                MessageContext.New(guid.NewGuid()));
        var (matchmakingId, matchmakingParticipantId) = await commandBus
            .SendAsync(
                joinQuickMatchmakingEnvelope, ct);

        return Ok(new { matchmakingId = matchmakingId, participantId = matchmakingParticipantId });
    }

    [HttpPost("leaveMatchmaking")]
    public async Task<IActionResult> Quit([FromBody] LeaveMatchmakingDto dto, CancellationToken ct)
    {
        var matchmakingId = Id.NewId(dto.MatchmakingId);
        var matchmakingParticipantId = ParticipantModule.Id.NewId(dto.MatchmakingParticipantId);
        var leaveMatchmakingCommand =
            new Application.UseCase.Handlers.LeaveMatchmaking.Command(matchmakingId, matchmakingParticipantId);
        var leaveMatchmakingEnvelope = new
            CommandEnvelope<Application.UseCase.Handlers.LeaveMatchmaking.Command>(leaveMatchmakingCommand,
                MessageContext.New(guid.NewGuid()));
        try
        {
            await commandBus.SendAsync(leaveMatchmakingEnvelope, ct);
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