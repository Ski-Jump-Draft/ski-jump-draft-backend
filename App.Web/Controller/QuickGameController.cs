using App.Application.Abstractions;
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
        var findOrCreateCommand =
            new Application.UseCase.Game.QuickGame.FindOrCreateMatchmaking.Command(matchmakingDto.Nick);
        var findOrCreateEnvelope =
            new CommandEnvelope<Application.UseCase.Game.QuickGame.FindOrCreateMatchmaking.Command,
                Domain.Matchmaking.Id>(findOrCreateCommand,
                MessageContext.New(guid.NewGuid()));
        var matchmakingId =
            await commandBus
                .SendAsync(
                    findOrCreateEnvelope, ct);

        var joinCommand =
            new Application.UseCase.Game.QuickGame.JoinMatchmaking.Command(matchmakingId,
                matchmakingDto.Nick);
        var joinEnvelope =
            new CommandEnvelope<Application.UseCase.Game.QuickGame.JoinMatchmaking.Command,
                Domain.Matchmaking.ParticipantModule.Id>(joinCommand, MessageContext.New(guid.NewGuid()));
        var matchmakingParticipantId =
            await commandBus
                .SendAsync(
                    joinEnvelope, ct);

        return Ok(new { matchmakingId = matchmakingId, participantId = matchmakingParticipantId });
    }

    [HttpPost("leaveMatchmaking")]
    public async Task<IActionResult> Quit([FromBody] LeaveMatchmakingDto dto, CancellationToken ct)
    {
        var matchmakingId = Id.NewId(dto.MatchmakingId);
        var matchmakingParticipantId = ParticipantModule.Id.NewId(dto.MatchmakingParticipantId);
        var leaveMatchmakingCommand =
            new Application.UseCase.Game.Matchmaking.Leave.Command(matchmakingId, matchmakingParticipantId);
        var leaveMatchmakingEnvelope = new
            CommandEnvelope<Application.UseCase.Game.Matchmaking.Leave.Command>(leaveMatchmakingCommand,
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