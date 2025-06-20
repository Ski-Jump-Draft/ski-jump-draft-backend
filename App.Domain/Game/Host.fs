namespace App.Domain.Game

open App.Domain
open App.Domain.Shared
open App.Domain.Shared.Ids

module Host =
    type Permissions =
        { Region: Server.Region
          AllowedServers: Server list }

open Host
type Host =
    { Id: HostId
      Permissions: Permissions }

    static member create (idGen: IGuid) (permissions: Permissions) =
        let invalid =
            permissions.AllowedServers
            |> List.filter (fun s -> s.Region <> permissions.Region)

        if not invalid.IsEmpty then
            failwith "One or more allowed servers do not belong to the given region"

        else
            { Id = Id.newHostId (idGen: IGuid)
              Permissions = permissions }
