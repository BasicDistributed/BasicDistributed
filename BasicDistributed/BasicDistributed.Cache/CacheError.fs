namespace BasicDistributed.Cache

open System 

type ICacheError =
    
    abstract member Message : string

[<AbstractClass>]
type CacheError(message) =
    let mutable message = message

    new() =
        CacheError(String.Empty)

    member private this.message : string = message

    interface ICacheError with
        member this.Message = this.message

type NotFound(key) =
    inherit CacheError(sprintf "There is no item in the store associated with the provided key %O" key)

module CacheError =
    
    let GetNotFound(key) =
        NotFound(key) :> ICacheError
