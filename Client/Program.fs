namespace Client

open Orleans.Providers.Streams.AzureQueue
open Orleans.Runtime.Configuration

open Infrastructure

open Actors

open System
open System.Reflection
open System.Diagnostics

open Orleankka
open Orleankka.Client
open Orleankka.Core
open Orleankka.Meta

module Program =    

    let actorIds = [for i in 1 .. 10 -> i.ToString()]

    let random = new Random()

    let pushToStream (client: IActorSystem) id message =       
        let stream = client.StreamOf(ProviderName, id)
        let envelope = createEnvelope message
        stream.Push(envelope) |> ignore

    let ask<'res> (client: IActorSystem) id message =
        let actorRef = client.ActorOf<MessageAccumulator>(id)
        let envelope = createEnvelope message
        actorRef.Ask<'res>(envelope)
        |> Async.AwaitTask

    let describe description action =
        let stopWatch = Stopwatch()
        printf "\r\n%s ... " description
        stopWatch.Start(); 
        action()
        stopWatch.Stop();
        printf "\r\nDone. Elapsed %d ms." stopWatch.Elapsed.Milliseconds
        
       

    [<EntryPoint>]
    let main argv = 
       
        let clientConfig = ClientConfiguration().LoadFromEmbeddedResource(Assembly.GetExecutingAssembly(), "client.xml")
        clientConfig.DataConnectionString <- connectionString AzureStorageAccount 
        clientConfig.DeploymentId <- connectionString ClusterId        
      
        let mutable client = null

        let start() = 
            client <- ActorSystem
                .Configure()
                .Client()
                .From(clientConfig)
                .Serializer<BinarySerializer>()
                .Register<AzureQueueStreamProvider>(ProviderName, streamSettings)
                .Register(typeof<IBillingActorsMark>.Assembly)
                .Done()        

        let send id = 
            pushToStream client id {AppendMessage.Message = sprintf "Message - %i" (random.Next 1000)}

        let ask id = async {                
            let! placement = ask<string> client id (GetPlacement() :> Query)
            let! messages = ask<string list> client id (GetMessages() :> Query)
            return (id, placement, messages)
        }

        let print (id, p, m) = 
            printf "\r\nid %s - %s - total messages %i" id p (Seq.length m);
            Seq.iter (printf "\r\n%s") m

        let sendAsk id = async {
            try
                do! Async.Sleep 2000 
                let! id, p, m = ask id
                print(id, p, m)
                printf "\r\nPing done stamp: %i." DateTime.Now.Millisecond
            with error ->                 
                printf "\r\nPing failed stamp: %i." DateTime.Now.Millisecond
        }
        
        let wait = Async.Sleep >> Async.RunSynchronously                    

        let sendMessages() =            
            Seq.iter send actorIds                                  

        let showPlacementAndMessages() = 
            actorIds
            |> Seq.map ask
            |> Async.Parallel
            |> Async.RunSynchronously
            |> Seq.iter print

        let pingActor() =
            actorIds
            |> Seq.head
            |> Seq.replicate 1000 
            |> Seq.map sendAsk
            |> Seq.iter Async.RunSynchronously
        
        
        describe "Start client" start  
        
        describe "Send random message to each actor" sendMessages     
        
        wait 3000           

        describe "Show placements and messages" showPlacementAndMessages

        wait 1000      

        describe "Ping first actor 1000 times" pingActor

        
        printf "\r\nPress any key to terminate client..."
        Console.ReadKey() 
        |> ignore

        0
        