using MicroComms.Core.Abstractions;

namespace WebApiHttpEcho.Models;

public record EchoResponse(string Message) : IResponse;