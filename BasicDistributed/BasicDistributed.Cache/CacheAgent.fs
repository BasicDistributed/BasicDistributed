namespace BasicDistributed.Cache

open Microsoft.Extensions.Caching.Memory

open System

type internal CacheCommand<'CacheKey>(cacheKey) =

    member this.CacheKey: 'CacheKey = cacheKey

type internal GetCommand<'CacheKey, 'CacheItem>(cacheKey, replyChannel) =
    inherit CacheCommand<'CacheKey>(cacheKey)

    member this.ReplyChannel: AsyncReplyChannel<'CacheItem option> = replyChannel

type internal GetOrSetCommand<'CacheKey, 'CacheItem>(cacheKey, getItemToCache, expirationInSeconds, replyChannel) =
    inherit CacheCommand<'CacheKey>(cacheKey)

    member this.GetItemToCache: unit -> 'CacheItem = getItemToCache
    member this.ExpirationInSeconds: float = expirationInSeconds
    member this.ReplyChannel: AsyncReplyChannel<'CacheItem> = replyChannel

type internal SetCommand<'CacheKey, 'CacheItem>(cacheKey, item, expirationInSeconds) =
    inherit CacheCommand<'CacheKey>(cacheKey)

    member this.Item : 'CacheItem = item
    member this.ExpirationInSeconds: float = expirationInSeconds

type internal RemoveCommand<'CacheKey>(cacheKey) =
    inherit CacheCommand<'CacheKey>(cacheKey)

module internal CacheAgent =
    
    let createAgent<'CacheKey, 'CacheItem>() =
        MailboxProcessor<CacheCommand<'Cachekey>>.Start(fun inbox ->
            //ToDo: Create an interface for Store and pass that instance to this create function to avoid dependencies
            let options = MemoryCacheOptions()
            let mutable store =  new MemoryCache(options)

            let tryGet cacheKey =
                let mutable t = null
                if store.TryGetValue(cacheKey, &t) then 
                    Some (t :?> 'CacheItem)
                else 
                    None
             
            let handleCommand (command : CacheCommand<'CacheKey>) =
                match command with
                | :? GetCommand<'CacheKey, 'CacheItem> as command -> 
                    tryGet command.CacheKey
                    |> command.ReplyChannel.Reply
                | :? GetOrSetCommand<'CacheKey, 'CacheItem> as command ->
                    match tryGet() with
                    | Some item -> command.ReplyChannel.Reply item
                    | None ->
                        //Run this asynchronously so other messages are not blocked. 
                        //After getting the item, send a message back so it's added to the cache.
                        async {
                            let item = command.GetItemToCache()

                            SetCommand<'CacheKey, 'CacheItem>(command.CacheKey, item, command.ExpirationInSeconds)
                            |> inbox.Post
                            
                            command.ReplyChannel.Reply item
                        }
                        |> Async.Start   
                | :? SetCommand<'CacheKey, 'CacheItem> as command -> 
                    let ts = DateTime.Now.AddSeconds(command.ExpirationInSeconds).TimeOfDay
                    store.Set(command.CacheKey, command.Item, ts)
                    |> ignore
                | :? RemoveCommand<'CacheKey> as command -> 
                    store.Remove command.CacheKey
                | _ -> ()
                         
            let rec processCommand() = 
                async {
                    let! command = inbox.Receive()
                    do handleCommand command
                    return! processCommand()
                }

            processCommand())

