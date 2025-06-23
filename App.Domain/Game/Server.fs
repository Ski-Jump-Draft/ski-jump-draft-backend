namespace App.Domain.Game

module Server =
    [<Struct>]
    type Id = Id of System.Guid
    
    module Region =
        [<Struct>]
        type Id = Id of System.Guid
        
        type Name = private Name of string
        module Name =
            let create(s: string)=
                if s |> System.String.IsNullOrWhiteSpace then
                    Error "Region.Label empty"
                elif s.Length > 20 then
                    Error "Too long"
                else Ok(Name s)
                
    type Region= {
        Id: Region.Id
        Name: Region.Name
    }

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
        Id: Server.Id
        Region: Server.Region.Id
        Label: Server.Label
    }
    
    // static member [...]