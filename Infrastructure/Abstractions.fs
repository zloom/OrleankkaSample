namespace Infrastructure

open System
open System.Linq
open System.Threading.Tasks
open System.Collections.Generic

open Orleankka
open Orleankka.FSharp
open Orleankka.Meta

open Contracts
open Literals

[<AutoOpen>]
module Abstractions =

    [<AbstractClass>]
    type SingleDeliveryActor(id, runtime) =
        inherit Actor(id, runtime)

        let maxCount = 200

        let buffer = new Queue<Guid>(maxCount)

        let enqueue (envelope: Envelope) = 
            if buffer.Count >= maxCount then 
                buffer.Dequeue() |> ignore
            buffer.Enqueue envelope.Id
            envelope.Message 
            
        abstract member OnFirstReceive: obj -> Task<obj>
        abstract member OnRepeatReceive: obj -> Task<obj>
        abstract member OnUnknownReceive: obj -> Task<obj>

        abstract StreamId : string with get
        default this.StreamId with get() = this.Id

        abstract member PushWithId: string -> obj -> Task
        default this.PushWithId streamId message =
            this.System.StreamOf(ProviderName, streamId).Push {Id = Guid.NewGuid(); Message = message} 

        override this.OnReceive message = 
            match message with 
            | :? Envelope as envelope -> if (buffer.Contains >> not) envelope.Id 
                                            then (enqueue >> this.OnFirstReceive) envelope
                                            else this.OnRepeatReceive envelope.Message
            | _                       -> this.OnUnknownReceive message
        
        
        member __.Handle (_: Envelope) = ()
        

    [<AbstractClass>]
    type EventSourcedActor(id, runtime) = 
        inherit SingleDeliveryActor(id, runtime)
        
        abstract member BeforeHandleEvent: Event -> Event 
        default __.BeforeHandleEvent event = event

        abstract member HandleEvent: Event -> Task<obj>
        default __.HandleEvent event = base.Dispatch(event, Task.FromResult)

        abstract member HandleCommand: Command -> Task<obj>
        default this.HandleCommand(command) = task {
            let! events = this.Dispatch<seq<Event>>(command, (fun _ -> Task.FromResult (Enumerable.Empty<Event>() :> obj)))
            Seq.iter ((this.PushWithId this.StreamId) >> ignore) events
            return box events
        }

        abstract member HandleQuery: Query -> Task<obj>
        default __.HandleQuery(query) =  base.Dispatch(query, Task.FromResult)

        override __.OnRepeatReceive _ = 
            Task.FromResult(obj())

        override __.OnUnknownReceive _ =
            Task.FromResult(obj())

        override this.OnFirstReceive message =
            match message with 
            | :? Command as command -> this.HandleCommand command
            | :? Query as query -> this.HandleQuery query
            | :? Event as event -> (this.BeforeHandleEvent >> this.HandleEvent) event
            | _ -> failwith (sprintf "Unknown message type: %s" (message.GetType().Name))


    [<AbstractClass>]
    type DurableEventSourcedActor(id, runtime, environment: IEnvironment) = 
        inherit EventSourcedActor(id, runtime)
              
        abstract TableName : string with get
        default this.TableName = this.GetType().Name

        abstract member GetStream: unit -> IEventStream
        default this.GetStream() = environment.GetStream(this.TableName, this.Id)
        
        override this.OnActivate() = 
            this.GetStream().ReadAll() 
                |> Seq.iter (this.HandleEvent >> Async.AwaitTask >> Async.RunSynchronously >> ignore)
            base.OnActivate()

        override this.BeforeHandleEvent(event) = 
            this.GetStream().Write(event) |> ignore
            base.BeforeHandleEvent(event)
        
       

       

        
            




            