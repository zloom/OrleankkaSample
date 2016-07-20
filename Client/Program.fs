namespace Client

open Orleans.Providers.Streams.AzureQueue
open Orleans.Runtime.Configuration

open Infrastructure

open Actors

open System
open System.Reflection

open Orleankka
open Orleankka.Client
open Orleankka.Core


module Program =       
    

    [<EntryPoint>]
    let main argv = 
       
        let clientConfig = ClientConfiguration().LoadFromEmbeddedResource(Assembly.GetExecutingAssembly(), "client.xml")
        clientConfig.DataConnectionString <- connectionString AzureStorageAccount 
        clientConfig.DeploymentId <- connectionString ClusterId        
      
        let client = 
            ActorSystem
                .Configure()
                .Client()
                .From(clientConfig)
                .Serializer<BinarySerializer>()
                .Register<AzureQueueStreamProvider>(ProviderName, streamSettings)
                .Register(typeof<IBillingActorsMark>.Assembly)
                .Done()


        while true do ()
                   

        Console.ReadKey() |> ignore

        0
        