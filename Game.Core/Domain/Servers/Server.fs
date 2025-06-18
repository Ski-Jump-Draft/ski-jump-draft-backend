namespace Game.Core.Domain.Servers

open Game.Core.Domain.Games
open Game.Core.Domain.Shared
open Game.Core.Domain.Shared.Ids

module Server =
    type Definition =
        { Id: ServerId
          Location: string // free‑form, e.g. "pl‑warsaw‑1"
          Host: string // DNS or IP
          Games: Game.Definition list } // includes history; prune in infra layer

    let empty location host =
        { Id = Id.newServerId()
          Location = location
          Host = host
          Games = [] }