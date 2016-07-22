namespace Host

open System
open System.Reflection

open Orleans.Runtime
open Orleans.Runtime.Configuration
open Orleans.Providers.Streams.AzureQueue

open Infrastructure
open Actors

open Orleankka
open Orleankka.Core
open Orleankka.Cluster

module Program =    
    

    [<EntryPoint>]
    let main argv = 
        let clusterConfig = ClusterConfiguration().LoadFromEmbeddedResource(Assembly.GetExecutingAssembly(), "host.xml")   
        clusterConfig.Globals.DataConnectionString <- connectionString AzureStorageAccount
        clusterConfig.Globals.DeploymentId <- connectionString ClusterId          
           
        let system = 
            ActorSystem
                .Configure()
                .Cluster()                                 
                .From(clusterConfig)
                .Serializer<BinarySerializer>()
                .Register(typeof<IActorsAssemblyMark>.Assembly)
                .Register<AzureQueueStreamProvider>(ProviderName, streamSettings)
                .Activator<NinjectActivator>()                                
                .Done()
                
        
        Console.ForegroundColor <- ConsoleColor.Green
        Console.WriteLine "Press any key to terminate host..."
        Console.ReadKey() |> ignore

        0
        