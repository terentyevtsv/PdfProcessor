using Domain.Db;
using Domain.Rabbit;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using UglyToad.PdfPig;

namespace BackgroundWorker
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Фоновый процесс запущен. Ожидание сообщений...");

            // Загрузка конфигурации БД и Брокера сообщений
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var rabbitConfig = config.GetSection("RabbitMQ");

            var factory = new ConnectionFactory() 
            {
                HostName = rabbitConfig["HostName"],
                Port = int.Parse(rabbitConfig["Port"]),
                UserName = rabbitConfig["UserName"],
                Password = rabbitConfig["Password"]
            };

            string conStr = config.GetSection("ConnectionStrings")["DefaultConnection"];

            // Подключение
            using (var connection = await factory.CreateConnectionAsync())
            {
                using (var channel = await connection.CreateChannelAsync())
                {
                    // Создание очереди
                    await channel.QueueDeclareAsync(
                        rabbitConfig["QueueName"], 
                        true, false, false);

                    // Создаём потребителя
                    var consumer = new AsyncEventingBasicConsumer(channel);
                    consumer.ReceivedAsync += async (object sender, BasicDeliverEventArgs e) =>
                    {
                        // Получаем данные сервиса
                        var body = e.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        var task = JsonSerializer.Deserialize<DocumentTask>(message);

                        Console.WriteLine($"Принятый документ: {task?.DocumentId}");

                        var documentsDbService = new DocumentsDbService(conStr);

                        // Проверка - может уже обработан?
                        var currentStatus = await documentsDbService.GetStatusAsync(task.DocumentId);
                        if (currentStatus == Status.Completed)
                        {
                            Console.WriteLine($"Документ {task.DocumentId} уже обработан, пропускаем");
                            await channel.BasicAckAsync(e.DeliveryTag, false);
                            return;
                        }

                        try
                        {
                            // Извлекаем текст из PDF
                            var pdfService = new PdfService(task.FilePath);
                            string extractedText = pdfService.GetText();

                            await documentsDbService.SaveTextAsync(task.DocumentId, extractedText);

                            Console.WriteLine($"Документ {task.DocumentId} успешно обработан. Длина текста: {extractedText.Length}");

                            await channel.BasicAckAsync(e.DeliveryTag, false);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка: {ex.Message}");

                            await documentsDbService.UpdateStatusAsync(task.DocumentId, Status.Failed);

                            await channel.BasicNackAsync(e.DeliveryTag, false, true);
                        }
                    };

                    await channel.BasicConsumeAsync(rabbitConfig["QueueName"], false, consumer);

                    Console.WriteLine(" Нажмите [enter] для завершения работы.");
                    Console.ReadLine();

                }
            }
        }
    }
}
