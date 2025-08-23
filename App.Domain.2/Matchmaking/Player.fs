namespace App.Domain._2.Matchmaking

type PlayerId = PlayerId of System.Guid
module Player =
    type Nick = private Nick of string
    module Nick =
        let create (v: string) =
            if v.Length < 25 then
                Some(Nick(v))
            else
                None
                
        let value (Nick v) : string = v

type Player =
    {
        Id: PlayerId
        Nick: Player.Nick
    }

