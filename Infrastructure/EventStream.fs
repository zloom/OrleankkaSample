namespace Infrastructure

open Streamstone

open System

open Microsoft.WindowsAzure.Storage

open Newtonsoft.Json 

open Orleankka.Meta

open Contracts


[<AutoOpen>]
module EventStream =
   
    type IEventStream = 
        abstract member ReadAll : unit -> seq<Event>
        abstract member Write : 'a -> StreamWriteResult
        abstract member WriteSeq : seq<'a> -> StreamWriteResult

    type AzureEventStream(azureTableStorageConnectionString, tableName, partitionName) =

        let table() = 
            let table = CloudStorageAccount.Parse(azureTableStorageConnectionString).CreateCloudTableClient().GetTableReference(tableName)
            table.CreateIfNotExists() |> ignore
            let partition = new Partition(table, partitionName) 
            if Stream.Exists partition then Stream.Open partition else Stream.Provision partition

        let wrap event =
            let storeBox = StoreBox()
            let eventType = event.GetType()
            storeBox.CreatedAt <- DateTime.UtcNow
            storeBox.Data <- JsonConvert.SerializeObject(event)
            storeBox.Id <- Guid.NewGuid()
            storeBox.Type <- eventType.AssemblyQualifiedName
                    
            EventData(EventId.From storeBox.Id, EventProperties.From storeBox) 
            
        let unwrap (storeBox: StoreBox) = 
            succeed storeBox.Type
            >>= switch (Type.GetType >> Option.ofObj)
            >>= function 
                Some(t) -> succeed (JsonConvert.DeserializeObject(storeBox.Data, t)) 
                | None -> fail (sprintf "Type not %s found." storeBox.Type) 
            >>= function 
                :? Event as event -> succeed event 
                | _ -> fail (sprintf "Type %s is not event." storeBox.Type)
            |> either Some failwith
          

        let readAll() =
            let mutable lastSlice = false
            let mutable nextSliceStart = 1
            let mutable events = Seq.empty
            let table = table()

            while not lastSlice do
              let slice = Stream.Read<StoreBox>(table.Partition, nextSliceStart)
              events <- events |> Seq.append slice.Events
              lastSlice <- slice.IsEndOfStream
              nextSliceStart <- slice.Events.Length

            events 
                |> Seq.map unwrap
                |> Seq.choose id
            
        let write events =
            let data = 
                events 
                |> Seq.map wrap 
                |> Seq.toArray
                             
            Stream.Write(table(), data)

        interface IEventStream with
            member __.WriteSeq events = write events
            
            member __.Write event = write [event]
                          
            member __.ReadAll() = readAll()