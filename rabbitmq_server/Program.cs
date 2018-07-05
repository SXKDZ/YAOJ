using System;
using RabbitMQ.Client;
using System.Text;
using System.IO;
using Newtonsoft.Json;

namespace rabbitmq_server
{
    class Program
    {
        public class Request
        {
            public string problemId;
            public string language;
            public string userId;
            public string sourceCode;
        }

        public static void Main(string[] args)
        {
            var factory = new ConnectionFactory() {
                HostName = "10.211.55.2",
                UserName = "user",
                Password = "123456rabbitmq",
                Port = 5672
            };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "yaoj_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);

                var request = GetRequest();
                var jsonifiedMessage = JsonConvert.SerializeObject(request);
                var body = Encoding.UTF8.GetBytes(jsonifiedMessage);

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                channel.BasicPublish(exchange: "", routingKey: "yaoj_queue", basicProperties: properties, body: body);
                Console.WriteLine(" [x] Request Sent");
            }

            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }

        private static Request GetRequest()
        {
            var sourceCode = File.ReadAllText("test.cxx");
            return new Request
            {
                problemId = "P1000",
                language = "C++",
                userId = "1000",
                sourceCode = sourceCode
            };
        }
    }
}
