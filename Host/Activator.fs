namespace Host

open Orleankka.Core
open Ninject

open Infrastructure.CompositionRoot
open Actors.CompositionRoot

open Ninject.Parameters
open Ninject.Modules

open Orleankka

[<AutoOpen>]
module Activator =    

    type HostActivator() =
        inherit ActorActivator()

        let container = new StandardKernel(new InfrastructureModule(), new ActorsModule())

        override __.Activate (actorType, id, runtime) =
            let idParameter = ConstructorArgument("id", id)
            let runtimeParameter = ConstructorArgument("runtime",runtime)
            container.Get(actorType, idParameter, runtimeParameter) :?> Actor

