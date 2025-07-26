using MicroComms.Core.Abstractions;
using MicroComms.Fluent;
using MicroComms.Serialization.Json;
using MicroComms.Transport.WebSocket;

// 2) Build the mediator against a public WebSocket echo endpoint
var mediator = MicroCommsBuilder.Create()
    .MapTransport<WsEchoRequest, WsEchoRequest>(
        transport: new WebSocketTransport(
            new Uri("wss://echo.websocket.events"),
            new JsonSerializerAdapter()
        ),
        serializer: new JsonSerializerAdapter()
    )
    .Build();

// 3) Get input and send
Console.Write("Enter message: ");
var input = Console.ReadLine() ?? "";

var resp = await mediator.SendAsync(new WsEchoRequest(input));

// 4) Print the echoed Message
Console.WriteLine($"Echoed back: {resp.Message}");

// 1) Define request/response (since echo, they’re the same type)
public record WsEchoRequest(string Message) : IRequest<WsEchoRequest>, IResponse;