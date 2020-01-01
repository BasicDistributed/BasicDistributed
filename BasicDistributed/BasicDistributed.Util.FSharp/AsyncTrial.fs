namespace BasicDistributed.Util.FSharp

open Chessie.ErrorHandling
open Chessie.ErrorHandling.AsyncTrial

module AsyncTrial =
    
    let singleton t =
        t
        |> Async.map Trial.ok
        |> AsyncResult.AR

    let lift f t =
        Async.ofAsyncResult t
        |> Async.map(Trial.lift f)
        |> AsyncResult.AR

    let bind f t =
        Async.ofAsyncResult t
        |> Async.map(Trial.bind f)
        |> AsyncResult.AR

    let bind2 f t =
        Async.ofAsyncResult t
        |> Async.map(function
            | Result.Ok (t, _) -> f t
            | Result.Bad msgs -> Result.Bad msgs)
        |> AsyncResult.AR



