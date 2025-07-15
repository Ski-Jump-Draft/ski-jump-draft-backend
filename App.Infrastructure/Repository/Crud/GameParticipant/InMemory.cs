using System.Collections.Concurrent;
using App.Domain.Game;
using App.Domain.Repositories;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;

namespace App.Infrastructure.Repository.Crud.GameParticipant
{
    public class InMemory : IGameParticipantRepository
    {
        // przechowujemy po Guid (zakładam, że Participant.Id.Item to Guid)
        private readonly ConcurrentDictionary<System.Guid, Participant.Participant> _store
            = new();

        public FSharpAsync<FSharpOption<Participant.Participant>> GetByIdAsync(Participant.Id id)
        {
            _store.TryGetValue(id.Item, out var participant);

            // F# Option.None == null, Some == new FSharpSome<T>(value)
            FSharpOption<Participant.Participant> opt = participant is not null
                ? (participant)
                : null!;

            // zwracamy od razu ukończone Async
            return FSharpAsync.AwaitTask(Task.FromResult(opt));
        }

        public FSharpAsync<Unit> RemoveAsync(Participant.Id id)
        {
            _store.TryRemove(id.Item, out _);
            return FSharpAsync.AwaitTask(Task.CompletedTask);
        }
        
        public FSharpAsync<Unit> SaveAsync(Participant.Participant participant)
        {
            _store[participant.Id.Item] = participant;
            // zwracamy od razu ukończone Async<Unit>
            return FSharpAsync.AwaitTask(Task.CompletedTask);
        }
    }
}