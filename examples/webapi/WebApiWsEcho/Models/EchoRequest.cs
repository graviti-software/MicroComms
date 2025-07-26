using MicroComms.Core.Abstractions;

namespace WebApiHttpEcho.Models;

public record WsEchoRequest(string Message) : IRequest<WsEchoRequest>, IResponse;