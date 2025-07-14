using System.Text;
using Confluent.Kafka;

namespace MatchMaking.Kafka;

public class KafkaJsonDeserializer<T> : IAsyncDeserializer<T> where T : class
{
    public Task<T> DeserializeAsync(ReadOnlyMemory<byte> data, bool isNull, SerializationContext context)
    {
        string json = Encoding.ASCII.GetString(data.Span);
        return Task.FromResult(System.Text.Json.JsonSerializer.Deserialize<T>(json))!;
    }
}