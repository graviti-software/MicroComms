using FluentAssertions;
using MicroComms.Client.Models;
using MicroComms.Core.Models;
using MicroComms.Serialization.Adapters;

namespace MicroComms.Client.Tests;

public class MessageFrameTests
{
    [Fact]
    public void MessageFrame_MessagePack_Roundtrip()
    {
        var frame = new MessageFrame(default)
        {
            Id = Guid.NewGuid(),
            Type = "My.Type.Name",
            Payload = [10, 20, 30]
        };

        var serializer = new MessagePackSerializerAdapter();
        var raw = serializer.Serialize(frame);
        var copy = serializer.Deserialize<MessageFrame>(raw);

        copy.Id.Should().Be(frame.Id);
        copy.Type.Should().Be(frame.Type);
        copy.Payload.Should().Equal(frame.Payload);
    }

    [Fact]
    public void MessageFrame_Json_Roundtrip()
    {
        var frame = new MessageFrame(default)
        {
            Id = Guid.NewGuid(),
            Type = "Another.Type",
            Payload = [1, 2, 3, 4]
        };

        var serializer = new JsonSerializerAdapter();
        var raw = serializer.Serialize(frame);
        var copy = serializer.Deserialize<MessageFrame>(raw);

        copy.Id.Should().Be(frame.Id);
        copy.Type.Should().Be(frame.Type);
        copy.Payload.Should().Equal(frame.Payload);
    }
}