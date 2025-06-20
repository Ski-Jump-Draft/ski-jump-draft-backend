namespace App.Domain.Game

open App.Domain.Shared.Ids

module Server =
    type Region = private Region of string
    module Region =
        let create (s: string) =
            if s |> System.String.IsNullOrWhiteSpace then
                failwith "Region empty"
            else
                Region s

    type Label = private Label of string
    module Label =
        let create(s: string)=
            if s |> System.String.IsNullOrWhiteSpace then
                failwith "Server.Label empty"
            elif s.Length > 20 then
                failwith "Too long"
            else Some(Label s)

type Server =
    {
        Id: ServerId
        Region: Server.Region
        Label: Server.Label
    }
    
    // static member [...]