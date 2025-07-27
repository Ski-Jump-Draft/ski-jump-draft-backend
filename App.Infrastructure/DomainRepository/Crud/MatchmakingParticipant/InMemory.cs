using App.Domain.Matchmaking;
using App.Domain.Repositories;

namespace App.Infrastructure.DomainRepository.Crud.MatchmakingParticipant;

public class InMemory(InMemoryCrudDomainRepositoryStarter<ParticipantModule.Id, Participant>? starter)
    : InMemoryCrudDomainRepository<Domain.Matchmaking.ParticipantModule.Id,
        Participant>(starter), IMatchmakingParticipantRepository;