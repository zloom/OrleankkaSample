namespace Infrastructure

open System
open System.Linq
open System.Configuration    
open System.Threading.Tasks

open Orleankka

open Contracts


module Seq = 
    let join innerSeq outerKey innerKey map (outerSeq:seq<_>) =
        outerSeq.Join(innerSeq, Func<_, _>(outerKey), Func<_, _>(innerKey), Func<_, _, _>(map))
        
    let groupJoin innerSeq outerKey innerKey map (outerSeq:seq<_>) =
        outerSeq.GroupJoin(innerSeq, Func<_, _>(outerKey), Func<_, _>(innerKey), Func<_, _, _>(map))

module String = 
    let empty = String.IsNullOrEmpty

    let notEmpty = String.IsNullOrEmpty >> not 

    let equals s1 s2 = String.Compare(s1, s2, StringComparison.InvariantCultureIgnoreCase) = 0

    let notEquals s1 s2 = String.Compare(s1, s2, StringComparison.InvariantCultureIgnoreCase) <> 0

[<AutoOpen>]
module Utils =

    let connectionString(name:string) =
        ConfigurationManager.ConnectionStrings.[name].ConnectionString

    let streamSettings =
        dict([("DataConnectionString", connectionString AzureStorageAccount)
              ("DeploymentId", connectionString ClusterId)])

    let createEnvelope message = 
        {Id = Guid.NewGuid(); Message = message}
 
    let toOption value = 
        if isNull(box value) then None else Some(value)

    let toNullable = function
    | None -> new System.Nullable<_>()
    | Some x -> new System.Nullable<_>(x)

    let inline cond p t e =
        if p then t else e

    let deleteBy list predicate = 
        let rec innerDelete list tail =
            match list with 
            | (x::xs)   -> cond (predicate x) (tail@xs) (innerDelete xs tail@[x])
            | []        -> tail
        innerDelete list []

    let delete list value = 
        deleteBy list ((=) value)

    let replaceBy list value predicate =
        let rec innerReplace list tail =
            match list with 
            | (x::xs)   -> cond (predicate x) (tail@(value::xs)) (innerReplace xs tail@[x])
            | []        -> tail
        innerReplace list []

    type Result<'TSuccess,'TFailure> = 
    | Success of 'TSuccess
    | Failure of 'TFailure
   
    let succeed x = 
        Success x
    
    let fail x = 
        Failure x
   
    let either successFunc failureFunc twoTrackInput =
        match twoTrackInput with
        | Success s -> successFunc s
        | Failure f -> failureFunc f
   
    let bind f = 
        either f fail
   
    let (>>=) x f = 
        bind f x

    let (>=>) s1 s2 = 
        s1 >> bind s2
   
    let switch f = 
        f >> succeed
   
    let map f = 
        either (f >> succeed) fail
    
    let tee f x = 
        f x; x 
    
    let tryCatch f exnHandler x =
        try
            f x |> succeed
        with
        | ex -> exnHandler ex |> fail
   
    let doubleMap successFunc failureFunc =
        either (successFunc >> succeed) (failureFunc >> fail)
    
    let plus addSuccess addFailure switch1 switch2 x = 
        match (switch1 x),(switch2 x) with
        | Success s1,Success s2 -> Success (addSuccess s1 s2)
        | Failure f1,Success _  -> Failure f1
        | Success _ ,Failure f2 -> Failure f2
        | Failure f1,Failure f2 -> Failure (addFailure f1 f2)

    let pass v x = 
        match x with
        | Failure x -> fail x
        | Success _ -> succeed v
        