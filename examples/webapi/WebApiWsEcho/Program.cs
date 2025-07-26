using MicroComms;
using MicroComms.Core.Abstractions;
using Microsoft.AspNetCore.Mvc;
using WebApiHttpEcho.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 1) Configure MicroComms for WebSocket echo
builder.Services.AddMicroComms(opts =>
{
    opts.UseJsonSerializer();

    // public echo.websocket.events endpoint
    opts.AddWebSocketTransport(
        name: "wsEcho",
        endpoint: new Uri("wss://echo.websocket.events")
    );

    opts.MapRequest<WsEchoRequest, WsEchoRequest>()
        .ToWebSocket("wsEcho");
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// 2) Public API endpoint that triggers WebSocket echo
app.MapGet("/api/ws-echo", async (
    [FromQuery] string message,
    IRequestMediator mediator
) =>
{
    var resp = await mediator.SendAsync(new WsEchoRequest(message));
    return Results.Ok(resp);
});

await app.RunAsync();