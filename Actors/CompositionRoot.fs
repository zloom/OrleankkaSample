namespace Actors

open Ninject.Modules

open Infrastructure

type IBillingActorsMark = interface end

module CompositionRoot =      
    

    type ActorsModule() =
        inherit NinjectModule()

        override this.Load() =            
            for t in this.GetType().Assembly.GetTypes() do
                if typeof<EventSourcedActor>.IsAssignableFrom t then
                    this.Bind(t).ToSelf() |> ignore