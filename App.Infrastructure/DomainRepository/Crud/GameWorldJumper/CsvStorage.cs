using App.Domain.GameWorld;
using App.Domain.Repositories;
using App.Infrastructure.DataSourceHelpers;
using Microsoft.FSharp.Core;
using JumperSkillsModule = App.Domain.GameWorld.JumperSkillsModule;

namespace App.Infrastructure.DomainRepository.Crud.GameWorldJumper;

public class CsvStorage(string path) : IGameWorldJumperRepository
{
    public async Task<FSharpOption<Jumper>> GetByIdAsync(JumperTypes.Id id, CancellationToken ct = default)
    {
        var all = await new GameWorldJumpersLoader(path).LoadAllAsync(ct);
        var dto = all.Single(jumperDto => jumperDto.Id == id.Item);
        var skills = new JumperSkills(JumperSkillsModule.BigSkillModule.tryCreate(dto.Takeoff).ResultValue,
            JumperSkillsModule.BigSkillModule.tryCreate(dto.Flight).ResultValue,
            JumperSkillsModule.LandingSkillModule.tryCreate(dto.Landing).ResultValue,
            JumperSkillsModule.LiveFormModule.tryCreate(dto.LiveForm).ResultValue);
        var jumper = new Jumper(JumperTypes.Id.NewId(dto.Id), JumperTypes.NameModule.tryCreate(dto.Name).Value,
            JumperTypes.SurnameModule.tryCreate(dto.Surname).Value, CountryModule.Id.NewId(dto.CountryId), skills);
        return FSharpOption<Jumper>.Some(jumper);
    }

    public Task SaveAsync(JumperTypes.Id id, Jumper value, CancellationToken ct = default)
    {
        throw new NotSupportedException();
        throw new NotImplementedException();
    }
}