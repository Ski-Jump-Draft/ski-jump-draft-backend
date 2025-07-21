// using App.Application.Abstractions;
// using App.Application.Exception;
// using App.Application.Ext;
// using App.Application.UseCase.Game.Exception;
// using App.Domain.Game;
// using App.Domain.Shared;
// using App.Domain.Repositories;
// using App.Domain.Repositories;
// using GameErrors = App.Domain.Game.GameModule.Error; // taki fajny myk
//
// namespace App.Application.UseCase.Game.JoinGame;
//
// public record Command(
//     App.Domain.Profile.User.Id UserId,
//     App.Domain.Game.Id.Id GameId
// ) : ICommand;
//
// public class Handler(
//     IGameRepository games,
//     IUserTranslator<Participant.Participant> translateUserToGameParticipant,
//     IGuid guid
// ) : IApplicationHandler<Command>
// {
//     public async Task HandleAsync(Command command, CancellationToken ct)
//     {
//         var userId = command.UserId;
//         var gameParticipant = await translateUserToGameParticipant.CreateTranslatedAsync(userId);
//
//         var gameId = command.GameId;
//
//         var game = await FSharpAsyncExt.AwaitOrThrow(games.LoadAsync(gameId, ct), new IdNotFoundException<Guid>(gameId.Item),
//             ct);
//
//         var joinResult = game.Join(gameParticipant.Id);
//
//         if (joinResult.IsOk)
//         {
//             var gameAndEvents = joinResult.ResultValue;
//
//             var correlationId = guid.NewGuid();
//             var causationId = correlationId;
//             var expectedVersion = game.Version_;
//
//             await FSharpAsyncExt.AwaitOrThrow(
//                 games.SaveAsync(gameAndEvents.Item1, gameAndEvents.Item2, expectedVersion, correlationId, causationId,
//                     ct),
//                 new JoiningGameFailedUnknownException(userId, gameAndEvents.Item1),
//                 ct
//             );
//         }
//         else
//         {
//             var error = joinResult.ErrorValue;
//
//             throw error switch
//             {
//                 GameErrors.ParticipantAlreadyJoined => new MatchmakingRoomFullException(game),
//                 GameErrors.EndingMatchmakingTooFewParticipants => new ParticipantAlreadyJoinedException(game, gameParticipant.Id),
//                 GameErrors.InvalidPhase invalidPhaseError => new JoiningMatchmakingInvalidPhaseException(
//                     invalidPhaseError.Expected.ToList(), invalidPhaseError.Actual),
//                 _ => new JoiningGameFailedUnknownException(userId, game)
//             };
//         }
//     }
// }