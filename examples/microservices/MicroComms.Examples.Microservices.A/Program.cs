using MicroComms.Examples.Microservices.Dto;
using MicroComms.Serialization.Adapters;
using MicroComms.Server;

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

// 1) Create the WebSocket‐based host on port 5001
var messageHost = new MessageHost(
    urlPrefix: "http://localhost:5001",                      // listen on this address
    serializer: new JsonSerializerAdapter()                  // use JSON for payloads
);

// 2) Subscribe server‐side to all incoming Product messages
messageHost.Subscribe<Product>(async product =>
{
    // simple handling: log to console or store in memory
    Console.WriteLine($"[A] Received Product {{ Id={product.Id}, Name={product.Name} }}");
    // (…insert your business logic here…)
});

// 3) Fire up the host in the background
_ = messageHost.StartAsync();  // don’t await here, so web‐API can still start

await app.RunAsync();