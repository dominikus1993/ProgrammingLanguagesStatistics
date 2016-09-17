// Before running any code, invoke Paket to get the dependencies.
//
// You can either build the project (Ctrl + Alt + B in VS) or run
// '.paket/paket.bootstrap.exe' and then '.paket/paket.exe install'
// (if you are on a Mac or Linux, run the 'exe' files using 'mono')
//
// Once you have packages, use Alt+Enter (in VS) or Ctrl+Enter to
// run the following in F# Interactive. You can ignore the project
// (running it doesn't do anything, it just contains this script)
#load "..\..\packages/FsLab/FsLab.fsx"
#r "../../packages/Octokit/lib/net45/Octokit.dll"

open Octokit
open Octokit.Internal
open Deedle
open System
open FSharp.Data
open XPlot.GoogleCharts
open XPlot.GoogleCharts.Deedle

let optional el =
    match el with
    | null -> None
    | element -> Some element

let getRepositoriesByLanguage (client: GitHubClient) (lang: Language) =
    let makeRequest(page) =
        let request = SearchRepositoriesRequest()
        request.Language <- new Nullable<Language>(lang)
        request.Page <- page
        request |> client.Search.SearchRepo |> Async.AwaitTask

    let response = makeRequest(1) |> Async.RunSynchronously |> optional
    
    match response with
    | Some res when res.TotalCount > 100 -> 
        let pages = (float res.TotalCount / 100.) |> Math.Ceiling |> int
        let allReposRequest = [2..pages] |> List.map(makeRequest)
        async { 
          let! result = allReposRequest |> Async.Parallel
          return result |> Seq.collect(fun x -> x.Items) |> Seq.append(res.Items) |> Seq.toList
        }
    | Some res -> 
        async { return res.Items |> Seq.toList }
    | None -> 
        async { return [] }

    
