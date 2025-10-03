using Confluent.Kafka;
using System.Text.Json;

namespace AuthService.Services
{
    public class KafkaProducerService
    {
        private readonly string _bootstrapServers;

        public KafkaProducerService(IConfiguration configuration)
        {
            _bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        }

        public async Task PublishAsync<T>(string topic, T message)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = _bootstrapServers
            };

            using var producer = new ProducerBuilder<string, string>(config).Build();

            var json = JsonSerializer.Serialize(message);

            await producer.ProduceAsync(topic,
                new Message<string, string> { Key = Guid.NewGuid().ToString(), Value = json });
        }
    }
}
