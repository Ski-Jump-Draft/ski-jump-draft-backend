using App.Util;
using App.Domain.GameWorld;
using App.Domain.Shared;
using CountryModule = App.Domain.GameWorld.CountryModule;

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
                CountryCodeModule.tryCreate("NOR").Value),
            new Country(CountryModule.Id.NewId(Guid.Parse("89656a12-5c5f-4871-8814-20e9956452d0")
                ),
                CountryCodeModule.tryCreate("DEU").Value),
            new Country(CountryModule.Id.NewId(Guid.Parse("b324d807-7cdc-4db4-aa38-4d1ea576862b")
                ),
                CountryCodeModule.tryCreate("AUT").Value)
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
                    HillTypes.Status.Operational,
                    HillTypes.Location.NewLocation("Zakopane"),
                    HillTypes.Name.NewName("Wielka Krokiew"),
                    GetCountry("POL"),
                    HillTypes.KPointModule.tryCreate(125).Value,
                    HillTypes.HsPointModule.tryCreate(140).Value,
                    new HillTypes.RealRecords(summer: new HillTypes.Record(
                        HillTypes.RecordModule.Setter.NewSimple("Yukiya Sato"),
                        HillTypes.RecordModule.DistanceModule.tryCreate(145).ResultValue
                    ), winter: new HillTypes.Record(
                        HillTypes.RecordModule.Setter.NewSimple("Yukiya Sato"),
                        HillTypes.RecordModule.DistanceModule.tryCreate(147).ResultValue
                    )),
                    HillTypes.InGameRecords.Empty
                ),
                Hill.Create(
                    HillTypes.Id.NewId(Guid.Parse("41c03a25-9d24-4ec0-8237-94df208e1642")),
                    HillTypes.Status.Operational,
                    HillTypes.Location.NewLocation("Oberstdorf"),
                    HillTypes.Name.NewName("Orlen Arena"),
                    GetCountry("POL"),
                    HillTypes.KPointModule.tryCreate(120).Value,
                    HillTypes.HsPointModule.tryCreate(137).Value,
                    new HillTypes.RealRecords(summer: new HillTypes.Record(
                        HillTypes.RecordModule.Setter.NewSimple("Gregor Schlierenzauer"),
                        HillTypes.RecordModule.DistanceModule.tryCreate(145.5).ResultValue
                    ), winter: new HillTypes.Record(
                        HillTypes.RecordModule.Setter.NewSimple("Sigurd Pettersen"),
                        HillTypes.RecordModule.DistanceModule.tryCreate(143.5).ResultValue
                    )),
                    HillTypes.InGameRecords.Empty
                ),
                Hill.Create(
                    HillTypes.Id.NewId(Guid.Parse("a4ffb647-5023-4418-adac-d0ad07346eb8")),
                    HillTypes.Status.Operational,
                    HillTypes.Location.NewLocation("Vikersund"),
                    HillTypes.Name.NewName("Vikersundbakken"),
                    GetCountry("NOR"),
                    HillTypes.KPointModule.tryCreate(200).Value,
                    HillTypes.HsPointModule.tryCreate(240).Value,
                    new HillTypes.RealRecords(summer: null, winter: new HillTypes.Record(
                        HillTypes.RecordModule.Setter.NewSimple("Daniel Huber"),
                        HillTypes.RecordModule.DistanceModule.tryCreate(247.5).ResultValue
                    )),
                    HillTypes.InGameRecords.Empty
                ),
            }
            .Select(result => result.ResultValue.Item1)
            .ToReadOnlyCollection();
    }
}