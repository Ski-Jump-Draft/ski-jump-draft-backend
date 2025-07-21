using App.Domain.Repositories;

namespace App.Infrastructure.DomainRepository.Crud.GameParticipant;

public class InMemory : InMemoryCrudDomainRepository<App.Domain.Game.Participant.Id,
    App.Domain.Game.Participant.Participant>, IGameParticipantRepository;