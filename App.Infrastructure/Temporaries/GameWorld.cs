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
                CountryCodeModule.tryCreate("POL").Value),
            new Country(CountryModule.Id.NewId(Guid.Parse("d2117abc-1576-425f-b84d-fc5cb37b362d")
                ),
                CountryCodeModule.tryCreate("NOR").Value)
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
            Hill.Create(HillId.NewHillId(Guid.Parse("bdb53387-47ee-4180-9fbb-e2e6c8fd6001")),
                HillModule.Location.NewLocation("Zakopane"), HillModule.Name.NewName("Wielka Krokiew"),
                GetCountry("POL"), HillModule.KPointModule.tryCreate(125).Value,
                HillModule.HSPointModule.tryCreate(140).Value),
            Hill.Create(HillId.NewHillId(Guid.Parse("41c03a25-9d24-4ec0-8237-94df208e1642")),
                HillModule.Location.NewLocation("Oberstdorf"), HillModule.Name.NewName("Orlen Arena"),
                GetCountry("POL"), HillModule.KPointModule.tryCreate(120).Value,
                HillModule.HSPointModule.tryCreate(137).Value),
            Hill.Create(HillId.NewHillId(Guid.Parse("a4ffb647-5023-4418-adac-d0ad07346eb8")),
                HillModule.Location.NewLocation("Vikersund"), HillModule.Name.NewName("Vikersundbakken"),
                GetCountry("NOR"), HillModule.KPointModule.tryCreate(200).Value,
                HillModule.HSPointModule.tryCreate(240).Value),
        }.Select(result => result.ResultValue.Item1).ToReadOnlyCollection();
    }
}