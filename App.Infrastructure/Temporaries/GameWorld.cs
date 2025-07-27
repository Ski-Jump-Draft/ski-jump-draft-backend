using App.Util;
using App.Domain.GameWorld;
using App.Domain.Shared;

namespace App.Infrastructure.Temporaries;

public static class GameWorld
{
    public static IReadOnlyCollection<Country> ConstructCountries()
    {
        return
        [
            new Country(CountryModule.Id.NewId(Guid.Parse("c4f3ffed-1576-425f-b84d-fc5cb37b362d")),
                CountryCodeModule.tryCreate("POL").Value)
        ];
    }

    private static CountryModule.Id GetCountry(string code)
    {
        return ConstructCountries().Single(c => CountryCodeModule.value(c.Code) == code).Id;
    }

    public static IReadOnlyCollection<Hill> ConstructHills()
    {
        // TODO: Licencyjna kwestia nazw skoczni

        return new[]
        {
            Hill.Create(HillId.NewHillId(Guid.Parse("11111111-1111-1111-1111-11111111111")),
                HillModule.Location.NewLocation("Zakopane"), HillModule.Name.NewName("Wielka Krokiew"),
                GetCountry("POL"), HillModule.KPointModule.tryCreate(125).Value,
                HillModule.HSPointModule.tryCreate(140).Value),
            Hill.Create(HillId.NewHillId(Guid.Parse("22222222-2222-2222-2222-22222222222")),
                HillModule.Location.NewLocation("Oberstdorf"), HillModule.Name.NewName("Orlen Arena"),
                GetCountry("POL"), HillModule.KPointModule.tryCreate(120).Value,
                HillModule.HSPointModule.tryCreate(137).Value),
            Hill.Create(HillId.NewHillId(Guid.Parse("22222223-3333-3333-3333-11111111111")),
                HillModule.Location.NewLocation("Vikersund"), HillModule.Name.NewName("Vikersundbakken"),
                GetCountry("NOR"), HillModule.KPointModule.tryCreate(200).Value,
                HillModule.HSPointModule.tryCreate(240).Value),
        }.Select(result => result.ResultValue.Item1).ToReadOnlyCollection();
    }
}