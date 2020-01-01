namespace BasicDistributed.Util.FSharp

open Chessie.ErrorHandling

module Trial =
    
    let failIfNoneF f opt =
        let fail() =
            f()
            |> Trial.fail

        opt
        |> Option.map Trial.ok
        |> Option.defaultWith fail

