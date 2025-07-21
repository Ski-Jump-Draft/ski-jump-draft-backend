using System.Collections.Concurrent;
using App.Domain.Matchmaking;
using App.Domain.Repositories;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;

namespace App.Infrastructure.DomainRepository.Crud.MatchmakingParticipant
{
    public class InMemory : IMatchmakingParticipantRepository
    {
        private readonly ConcurrentDictionary<ParticipantModule.Id, Participant> _store
            = new();

        public FSharpAsync<FSharpOption<Participant>> GetByIdAsync(ParticipantModule.Id id)
        {
            _store.TryGetValue(id, out var participant);

            FSharpOption<Participant> opt = participant is not null
                ? (participant)
                : null!;
            
            return FSharpAsync.AwaitTask(Task.FromResult(opt));
        }

        public FSharpAsync<Unit> RemoveAsync(ParticipantModule.Id id)
        {
            _store.TryRemove(id, out _);
            return FSharpAsync.AwaitTask(Task.CompletedTask);
        }

        public FSharpAsync<Unit> SaveAsync(Participant participant)
        {
            _store[participant.Id] = participant;
            return FSharpAsync.AwaitTask(Task.CompletedTask);
        }

        public FSharpAsync<Unit> SaveAsync(FSharpList<Participant> participants)
        {
            foreach (var participant in participants)
            {
                _store[participant.Id] = participant;
            }

            return FSharpAsync.AwaitTask(Task.CompletedTask);
        }
    }
}