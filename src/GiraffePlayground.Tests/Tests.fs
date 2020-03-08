module Tests

open System
open Xunit
open Giraffe
open System.Threading.Tasks
open NSubstitute
open Microsoft.AspNetCore.Http
open Giraffe.Serialization
open System.IO
open Program
open System.Text
open Newtonsoft.Json
open FSharp.Control.Tasks.V2

//https://github.com/samueleresca/Blog.FSharpOnWeb/blob/master/test/Blog.FSharpWebAPI.Tests/Fixtures.fs
let next : HttpFunc = Some >> Task.FromResult

let buildMockContext () =
    let context = Substitute.For<HttpContext>()
    context.RequestServices.GetService(typeof<INegotiationConfig>).Returns(DefaultNegotiationConfig()) |> ignore
    context.RequestServices.GetService(typeof<Json.IJsonSerializer>).Returns(NewtonsoftJsonSerializer(NewtonsoftJsonSerializer.DefaultSettings)) |> ignore
    context.Request.Headers.ReturnsForAnyArgs(new HeaderDictionary()) |> ignore
    context.Response.Body <- new MemoryStream()
    context
    
let getBody (ctx : HttpContext) =
    ctx.Response.Body.Position <- 0L
    use reader = new StreamReader(ctx.Response.Body, System.Text.Encoding.UTF8)
    reader.ReadToEnd()

[<Fact>]
let ``My test`` () =
    let fakeDbInsert (user: User) : Result<User, string> =
        Ok user
        
    let handler =
        Program.InsertUserHandler fakeDbInsert
    
    let user : User = { Email = "test@gmail.com"; Password = "test1234"; Username = "test" }
    let postData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(user))
    
    let context = buildMockContext()
    context.Request.Body <- new MemoryStream(postData)    
        
    task {
        let! response = handler next context
        Assert.True(response.IsSome)
        let context = response.Value
        let body = getBody context
        Assert.Equal("{\"email\":\"test@gmail.com\",\"password\":\"test1234\",\"username\":\"test\"}", body)
    }
