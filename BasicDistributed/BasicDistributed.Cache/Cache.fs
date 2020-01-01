namespace BasicDistributed.Cache

open BasicDistributed.Util.FSharp

open Chessie.ErrorHandling

open System     

type TimeOutInSeconds = int
type ExpirationInSeconds = float
type GetItemToCache<'T> = unit -> 'T

type ICache<'CacheKey, 'Message> =
    
    abstract member Get<'CacheItem> : 'CacheKey * ?TimeOutInSeconds : int -> AsyncResult<'CacheItem option, 'Message>

    abstract member GetOrSet<'CacheItem> : 'CacheKey * GetItemToCache<'CacheItem> * ExpirationInSeconds * ?TimeOutInSeconds : int -> AsyncResult<'CacheItem, 'Message>

    abstract member Set<'CacheItem> : 'CacheKey * 'CacheItem * ExpirationInSeconds -> unit

    abstract member Remove : 'CacheKey -> unit


type Cache<'CacheKey when 'CacheKey : comparison>() =
    
    let agent = CacheAgent.createAgent<'CacheKey, obj>()

    let handle timeOutInSeconds map buildMessage =
        match timeOutInSeconds with
        | Some t -> agent.PostAndAsyncReply(buildMessage, t)
        | None -> agent.PostAndAsyncReply(buildMessage)
        |> AsyncTrial.singleton
        |> AsyncTrial.lift map
        
    let upcastCommand command = command :> CacheCommand<'CacheKey>

    interface ICache<'CacheKey, ICacheError> with
            
        member this.Get<'CacheItem>(cacheKey, ?timeOutInSeconds) =
            let f = Option.map(fun o -> box o :?> 'CacheItem)

            let buildMessage replyChannel =
                GetCommand(cacheKey, replyChannel) 
                |> upcastCommand

            handle timeOutInSeconds f buildMessage
            
        member this.GetOrSet<'CacheItem>(cacheKey, getItemToCache, expirationInSeconds, ?timeOutInSeconds) =
            let f = box >> (fun o  -> o :?> 'CacheItem)

            let buildMessage replyChannel =
                GetOrSetCommand<'CacheKey, 'CacheItem>(cacheKey, getItemToCache, expirationInSeconds, replyChannel) 
                |> upcastCommand

            handle timeOutInSeconds f buildMessage

        member this.Set<'CacheItem>(cacheKey, item, expirationInSeconds) =
            SetCommand<'CacheKey, 'CacheItem>(cacheKey, item, expirationInSeconds) 
            |> agent.Post

        member this.Remove(cacheKey) =
            RemoveCommand(cacheKey)
            |> agent.Post