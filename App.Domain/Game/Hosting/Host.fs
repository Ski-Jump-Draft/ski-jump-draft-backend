namespace App.Domain.Game.Hosting

open App.Domain
open App.Domain.Shared

module Host =
    [<Struct>]
    type Id = Id of System.Guid

    type Permissions =
        { Region: Game.Server.Region.Id
          AllowedServers: Game.Server list }

type Host =
    { Id: Host.Id
      Permissions: Host.Permissions }

    static member create (idGen: IGuid) (permissions: Host.Permissions) =
        let invalid =
            permissions.AllowedServers
            |> List.filter (fun s -> s.Region <> permissions.Region)

        if not invalid.IsEmpty then
            failwith "One or more allowed servers do not belong to the given region"

        else
            { Id = Host.Id(idGen.NewGuid())
              Permissions = permissions }
