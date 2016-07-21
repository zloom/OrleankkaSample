namespace Actors

open Ninject.Modules

open Infrastructure

open Orleankka

open System

[<StreamSubscription(Source = ProviderName + ":/(?<id>(.*))/", Target="{id}")>]
type MessageAccumulator(id, runtime, env) = 
    inherit DurableEventSourcedActor(id, runtime, env)

    let mutable messages = []

    member __.On(event: MessageAppended) = 
        messages <- event.Message :: messages
        
    member __.Handle(command: AppendMessage) = 
        [{MessageAppended.Message = command.Message}]         
        
    member __.Answer(query: GetMessages) =
        messages   

    member __.Answer(query: GetPlacement) =
        Environment.MachineName  
        