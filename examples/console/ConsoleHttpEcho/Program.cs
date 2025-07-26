// 1) Define your request/response
using MicroComms.Core.Abstractions;
using MicroComms.Fluent;
using MicroComms.Serialization.Json;
using MicroComms.Transport.Http;
using System.Net;

// 2) Start a tiny HTTP echo server on localhost:5000/echo/
var listener = new HttpListener();
listener.Prefixes.Add("http://localhost:5000/echo/");
listener.Start();
_ = Task.Run(async () =>
{
    while (true)
    {
        var ctx = await listener.GetContextAsync();
        using var ms = new MemoryStream();
        await ctx.Request.InputStream.CopyToAsync(ms);
        var data = ms.ToArray();
        ctx.Response.StatusCode = 200;
        ctx.Response.ContentType = "application/octet-stream";
        await ctx.Response.OutputStream.WriteAsync(data);
        ctx.Response.Close();
    }
});

// 3) Build the mediator with HTTP transport + JSON serializer
var httpClient = new HttpClient();
var mediator = MicroCommsBuilder.Create()
    .MapTransport<EchoRequest, EchoResponse>(
        transport: new HttpTransport(httpClient, new HttpTransportMetadata
        {
            BaseAddress = "http://localhost:5000",
            Route = "echo",
            Method = HttpMethod.Post
        }),
        serializer: new JsonSerializerAdapter()
    )
    .Build();

// 4) Prompt, send, receive, and display
string? msg = null;
while (msg != "exit")
{
    Console.Write("Enter message (or 'exit' to quit): ");
    msg = Console.ReadLine() ?? "";
    if (msg.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;
    var resp = await mediator.SendAsync<EchoRequest, EchoResponse>(
        new EchoRequest(msg)
    );
    Console.WriteLine($"Echoed back: {resp.Message}");
}

// 1) Define your request/response types
public record EchoRequest(string Message) : IRequest<EchoResponse>;
public record EchoResponse(string Message) : IResponse;