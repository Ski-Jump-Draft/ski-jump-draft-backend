module TestHelpers

let getOk = function
  | Ok v    -> v
  | Error e -> failwithf $"Expected Ok, got Error: %A{e}"
