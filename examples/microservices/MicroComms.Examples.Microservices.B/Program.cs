// 1) Create the WebSocket transport pointing at A’s host
using MicroComms.Client.Services;
using MicroComms.Core.Abstractions;
using MicroComms.Examples.Microservices.Dto;
using MicroComms.Serialization.Adapters;
using MicroComms.Transport;
using Microsoft.Extensions.Logging.Abstractions;

var transport = new ClientTransport(new Uri("ws://localhost:5001/ws"));

// 2) Create the MessageClient with JSON serialization, a no-op logger, and 1s reconnect delay
var client = new MessageClient(
    transport,
    new JsonSerializerAdapter(),
    NullLogger<MessageClient>.Instance,
    reconnectDelay: 1000
);

// 3) Subscribe to Product messages
client.Subscribe<Product>(product =>
{
    Console.WriteLine($"[B] Received Product {{ Id={product.Id}, Name={product.Name} }}");
    return Task.CompletedTask;
});

// 4) Connect the transport (starts receive loop and fires OnConnected)
await transport.ConnectAsync();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () =>
{
    return Results.Ok();
})
.WithName("Health")
.WithOpenApi();

app.MapPost("/publish-product", async (Product product) =>
{
    // Send the product, wait for Ack
    Ack ack = await client.RequestAsync(product);

    return ack.StatusCode == 200
        ? Results.Created($"/products/{product.Id}", product)
        : Results.Problem(ack.ErrorMessage ?? "Unknown error", statusCode: ack.StatusCode);
});

await app.RunAsync();