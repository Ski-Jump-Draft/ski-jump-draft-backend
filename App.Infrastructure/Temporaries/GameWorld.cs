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
                Hill.Create(
                    HillTypes.Id.NewId(Guid.Parse("bdb53387-47ee-4180-9fbb-e2e6c8fd6001")),
                    HillTypes.Location.NewLocation("Zakopane"),
                    HillTypes.Name.NewName("Wielka Krokiew"),
                    GetCountry("POL"),
                    HillTypes.KPointModule.tryCreate(125).Value,
                    HillTypes.HsPointModule.tryCreate(140).Value,
                    new HillTypes.Record(
                        HillTypes.RecordModule.SetterReference.NewSimple("Yukiya Sato"),
                        HillTypes.RecordModule.DistanceModule.tryCreate(147).ResultValue
                    )
                ),
                Hill.Create(
                    HillTypes.Id.NewId(Guid.Parse("41c03a25-9d24-4ec0-8237-94df208e1642")),
                    HillTypes.Location.NewLocation("Oberstdorf"),
                    HillTypes.Name.NewName("Orlen Arena"),
                    GetCountry("POL"),
                    HillTypes.KPointModule.tryCreate(120).Value,
                    HillTypes.HsPointModule.tryCreate(137).Value,
                    new HillTypes.Record(
                        HillTypes.RecordModule.SetterReference.NewSimple("Sigurd Pettersen"),
                        HillTypes.RecordModule.DistanceModule.tryCreate(143).ResultValue
                    )
                ),
                Hill.Create(
                    HillTypes.Id.NewId(Guid.Parse("a4ffb647-5023-4418-adac-d0ad07346eb8")),
                    HillTypes.Location.NewLocation("Vikersund"),
                    HillTypes.Name.NewName("Vikersundbakken"),
                    GetCountry("NOR"),
                    HillTypes.KPointModule.tryCreate(200).Value,
                    HillTypes.HsPointModule.tryCreate(240).Value,
                    new HillTypes.Record(
                        HillTypes.RecordModule.SetterReference.NewSimple("Daniel Huber"),
                        HillTypes.RecordModule.DistanceModule.tryCreate(147.5).ResultValue
                    )
                ),
            }
            .Select(result => result.ResultValue.Item1)
            .ToReadOnlyCollection();
    }
}