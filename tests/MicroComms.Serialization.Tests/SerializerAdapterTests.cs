using FluentAssertions;
using MicroComms.Core.Abstractions;
using MicroComms.Serialization.Adapters;

namespace MicroComms.Serialization.Tests;

public class SerializerAdapterTests
{
    private class TestDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(42)]
    [InlineData(-7)]
    public void MessagePack_Generic_Roundtrip_Primitive(int value)
    {
        ISerializer sut = new MessagePackSerializerAdapter();
        var bytes = sut.Serialize(value);
        var result = sut.Deserialize<int>(bytes);

        result.Should().Be(value);
    }

    [Fact]
    public void MessagePack_NonGeneric_Roundtrip_Object()
    {
        var dto = new TestDto { Id = 5, Name = "Hello" };
        ISerializer sut = new MessagePackSerializerAdapter();

        var bytes = sut.Serialize(dto);
        var obj = sut.Deserialize(typeof(TestDto), bytes);

        obj.Should().BeOfType<TestDto>()
           .Which.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public void Json_Generic_Roundtrip_Object()
    {
        var dto = new TestDto { Id = 99, Name = "World" };
        ISerializer sut = new JsonSerializerAdapter();

        var bytes = sut.Serialize(dto);
        var result = sut.Deserialize<TestDto>(bytes);

        result.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public void Json_NonGeneric_Roundtrip_Primitive()
    {
        const string text = "foo bar";
        ISerializer sut = new JsonSerializerAdapter();

        var bytes = sut.Serialize(text);
        var obj = sut.Deserialize(typeof(string), bytes);

        obj.Should().BeOfType<string>().Which.Should().Be(text);
    }
}