using MicroComms;
using MicroComms.Core.Abstractions;
using WebApiHttpEcho.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 1) Configure MicroComms
builder.Services.AddMicroComms(opts =>
{
    opts.UseJsonSerializer();

    // in-process echo endpoint at /internal-echo
    opts.AddHttpTransport(
        name: "local",
        baseAddress: "http://localhost:5195",
        route: "internal-echo",
        method: HttpMethod.Post
    );

    opts.MapRequest<EchoRequest, EchoResponse>()
        .ToHttp("local");
});

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

// 2) Internal echo endpoint (so we control envelope round-trip)
app.MapPost("/internal-echo", async (HttpContext ctx) =>
{
    // copy raw bytes back to response
    ctx.Response.StatusCode = 200;
    ctx.Response.ContentType = "application/octet-stream";
    await ctx.Request.Body.CopyToAsync(ctx.Response.Body);
});

// 3) Public API endpoint
app.MapPost("/api/echo", async (
    EchoRequest req,
    IRequestMediator mediator
) =>
{
    // sends req via MicroComms→internal echo and returns result
    var resp = await mediator.SendAsync(req);
    return Results.Ok(resp);
});

await app.RunAsync();