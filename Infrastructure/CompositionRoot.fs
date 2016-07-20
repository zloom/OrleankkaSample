namespace Infrastructure
  
open Ninject.Modules
open Environment

module CompositionRoot =

    type InfrastructureModule() =
        inherit NinjectModule()
        override this.Load() =
            this.Bind<IEnvironment>()
                .To<ActorEnvironment>()
                .WithConstructorArgument("azureTableStorageConnectionString", connectionString AzureStorageAccount) 
                |> ignore
