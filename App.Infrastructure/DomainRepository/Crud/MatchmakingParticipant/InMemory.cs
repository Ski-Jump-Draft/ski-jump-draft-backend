using App.Domain.Repositories;

namespace App.Infrastructure.DomainRepository.Crud.MatchmakingParticipant;

public class InMemory : InMemoryCrudDomainRepository<Domain.Matchmaking.ParticipantModule.Id,
    Domain.Matchmaking.Participant>, IMatchmakingParticipantRepository;