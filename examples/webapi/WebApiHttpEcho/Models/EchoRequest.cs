using MicroComms.Core.Abstractions;

namespace WebApiHttpEcho.Models;

public record EchoRequest(string Message) : IRequest<EchoResponse>;