namespace App.Domain.GameWorld

module Country =
    [<Struct>]
    type Id = Id of System.Guid

    type Code = App.Domain.Shared.CountryCode

open Country
type Country = { Id: Country.Id; Code: Code }
