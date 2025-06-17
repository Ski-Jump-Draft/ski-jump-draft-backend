namespace Game.Core.Domain

module Server =
    open Ids

    type Server =
        { Id: ServerId
          Location: string // free‑form, e.g. "pl‑warsaw‑1"
          Host: string // DNS or IP
          Games: Game list } // includes history; prune in infra layer

    let empty location host =
        { Id = Id.newServerId()
          Location = location
          Host = host
          Games = [] }