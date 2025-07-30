using App.Domain.Game;
using App.Domain.Repositories;

namespace App.Infrastructure.DomainRepository.Crud.GameParticipant;

public class InMemory(InMemoryCrudDomainRepositoryStarter<Participant.Id, Participant.Participant>? starter = null)
    : InMemoryCrudDomainRepository<App.Domain.Game.Participant.Id,
        App.Domain.Game.Participant.Participant>(starter), IGameParticipantRepository;