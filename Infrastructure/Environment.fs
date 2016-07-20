namespace Infrastructure


[<AutoOpen>]
module Environment =

    type IEnvironment =
        abstract member GetStream: string * string -> IEventStream

    type ActorEnvironment(azureTableStorageConnectionString) =

        interface IEnvironment with

            member __.GetStream(table, partition) = 
                new AzureEventStream(azureTableStorageConnectionString, table, partition) :> IEventStream
    