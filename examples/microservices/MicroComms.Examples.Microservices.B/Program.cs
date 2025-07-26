// 1) Create the WebSocket transport pointing at A’s host
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

await app.RunAsync();