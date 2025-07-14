using System.Text;
using Confluent.Kafka;

namespace MatchMaking.Kafka;

public class KafkaJsonSerializer<T> : IAsyncSerializer<T> where T : class
{
    Task<byte[]> IAsyncSerializer<T>.SerializeAsync(T data, SerializationContext context)
    {
        string jsonString = System.Text.Json.JsonSerializer.Serialize(data);
        return Task.FromResult(Encoding.ASCII.GetBytes(jsonString));
    }
}