using Domain.Rabbit;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Infrastructure.RabbitMQ
{
    public class RabbitMqProducer : IAsyncDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly string _queueName;

        public RabbitMqProducer(RabbitMqSettings rabbitMqSettings)
        {
            _queueName = rabbitMqSettings.QueueName;

            var factory = new ConnectionFactory
            {
                HostName = rabbitMqSettings.HostName,
                UserName = rabbitMqSettings.UserName,
                Password = rabbitMqSettings.Password,
                Port = rabbitMqSettings.Port
            };

            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            _channel.QueueDeclareAsync(
                _queueName,
                true,
                false,
                false).GetAwaiter().GetResult();
        }

        public async Task PublishAsync(DocumentTask task)
        {
            // Переводим объект в массив байт для отправки в RabbitMQ
            var json = JsonSerializer.Serialize(task);
            var body = Encoding.UTF8.GetBytes(json);

            // Передача сообщения в очередь
            await _channel.BasicPublishAsync(
                string.Empty,
                _queueName,
                body);
        }

        public async ValueTask DisposeAsync()
        {
            await _channel.CloseAsync();
            await _connection.CloseAsync();
        }
    }
}
