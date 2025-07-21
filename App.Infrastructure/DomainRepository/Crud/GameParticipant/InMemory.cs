using System.Collections.Concurrent;
using App.Domain.Game;
using App.Domain.Repositories;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;

namespace App.Infrastructure.DomainRepository.Crud.GameParticipant
{
    public class InMemory : IGameParticipantRepository
    {
        private readonly ConcurrentDictionary<System.Guid, Participant.Participant> _store
            = new();

        public FSharpAsync<FSharpOption<Participant.Participant>> GetByIdAsync(Participant.Id id)
        {
            _store.TryGetValue(id.Item, out var participant);

            FSharpOption<Participant.Participant> opt = participant is not null
                ? (participant)
                : null!;
            
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
            return FSharpAsync.AwaitTask(Task.CompletedTask);
        }

        public FSharpAsync<Unit> SaveAsync(FSharpList<Participant.Participant> participants)
        {
            foreach (var participant in participants)
            {
                _store[participant.Id.Item] = participant;
            }

            return FSharpAsync.AwaitTask(Task.CompletedTask);
        }
    }
}