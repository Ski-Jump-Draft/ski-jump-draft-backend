namespace App.Domain.Matchmaking

open System.Text.RegularExpressions

type PlayerId = PlayerId of System.Guid

module Player =
    type Nick = private Nick of string

    module Nick =
        let suffixRegex = Regex(@"\(\d+\)$", RegexOptions.Compiled)

        let create (v: string) =
            if v.Contains("(") || v.Contains(")") then
                None
            else if
                v.Length <= 25
            then
                Some(Nick v)
            else
                None

        let createWithSuffix (v: string) =
            if suffixRegex.IsMatch(v) then Some(Nick v) else create v

        let value (Nick v) = v

type Player = { Id: PlayerId; Nick: Player.Nick }
