open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Giraffe
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2

[<CLIMutable>]
type User =
    {
        Email : string
        Password : string
        Username : string
    }

let dbUserInsert (user : User) : Result<User, string> =
    //fake a db call or something
    Ok user

let InsertUserHandler =
    fun  (insertUser : User -> Result<User, string>) (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! user = ctx.BindJsonAsync<User>()
         
            let insertUserResult = insertUser user
         
            match insertUserResult with
            | Ok u ->
                return! Successful.OK u next ctx
            | Error msg ->
                return! ServerErrors.INTERNAL_ERROR msg next ctx
        }

let InsertUserHandlerBuilder =
    InsertUserHandler dbUserInsert

let webApp =
    choose [
        POST >=>
            choose [
                route "/users" >=> InsertUserHandlerBuilder
            ]
    ]

let configureApp (app : IApplicationBuilder) =
    // Add Giraffe to the ASP.NET Core pipeline
    app.UseGiraffe webApp

let configureServices (services : IServiceCollection) =
    // Add Giraffe dependencies
    services.AddGiraffe() |> ignore

[<EntryPoint>]
let main _ =
    WebHostBuilder()
        .UseKestrel()
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .Build()
        .Run()
    0