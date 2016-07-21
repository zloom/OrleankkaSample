namespace Infrastructure

open System

open Orleankka.Meta

[<AutoOpen>]
module Contracts =
    

    type Envelope = 
        {Id: Guid; Message: obj}

    type StoreBox() =
        member val Data = String.Empty with get, set
        member val CreatedAt = DateTime.UtcNow with get, set
        member val Id = Guid.Empty with get, set
        member val Type = String.Empty with get, set


    type AppendMessage = 
        {Message: string}
        interface Command

    type MessageAppended =
        {Message: string}
        interface Event

    type GetMessages() = 
        class interface Query end

    type GetPlacement() = 
        class interface Query end