using Colorful;
using Newtonsoft.Json;
using System.Drawing;
using System.IO;
using Console = Colorful.Console;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Threading;

namespace localjudge
{
    class Program
    {
        static void Welcome(string judgeName)
        {
            var alternatorFactory = new ColorAlternatorFactory();
            var alternator = alternatorFactory.GetAlternator(2, Color.Plum, Color.PaleVioletRed);
            Console.WriteAsciiAlternating("Y A O J", alternator);
            Console.WriteLine("YAOJ - Yet Another Online Judge", Color.LightBlue);

            Console.WriteLine($"LocalJudge" +
                $" v{Assembly.GetEntryAssembly().GetName().Version}" +
                $" {judgeName}", Color.SlateGray);
            Console.WriteLine();
        }

        static void Main(string[] args)
        {
            var exit = new ThreadSafeBoolean() { V = false };

            string configContent;
            using (var sr = new StreamReader("config.json"))
            {
                configContent = sr.ReadToEnd();
            }
            var config = JObject.Parse(configContent);

            Welcome(config["judgeName"].ToString());

            var rabbitServer = config["rabbitServer"].ToString();
            var rabbitUsername = config["rabbitUsername"].ToString();
            var rabbitPassword = config["rabbitPassword"].ToString();
            var rabbitPort = int.Parse(config["rabbitPort"].ToString());

            var judgeContext = JudgeContext.CreateFromJson(config["judgeConfig"].ToString());

            var connectionFactory = new ConnectionFactory()
            {
                HostName = rabbitServer,
                UserName = rabbitUsername,
                Password = rabbitPassword,
                Port = rabbitPort
            };
            var connection = connectionFactory.CreateConnection();
            var channel = connection.CreateModel();

            channel.QueueDeclare(queue: "yaoj_queue", durable: true, exclusive: false,
                autoDelete: false, arguments: null);

            channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            Console.WriteLine("Connected to the server...", Color.Coral);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body;
                var message = Encoding.UTF8.GetString(body);
                var request = JsonConvert.DeserializeObject<Request>(message);
                Console.WriteLine("Received judge request...", Color.Coral);

                // get judge task from the message queue
                var sourceCode = request.sourceCode;
                var language = request.language;
                var problemId = request.problemId;
                var dataset = Dataset.CreateFromJson(Utility.GetDatasetConfigPath(judgeContext, problemId));

                var judgeCase = new JudgeCase(sourceCode, language, judgeContext, dataset);
                judgeCase.Judge();

                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            };

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                exit.V = true;
                eventArgs.Cancel = true;
            };

            while (!exit.V)
            {
                channel.BasicConsume(queue: "yaoj_queue", autoAck: false, consumer: consumer);
            }

            channel.Close();
            connection.Close();
            Console.WriteLine("Goodbye!", Color.LightGoldenrodYellow);
        }
    }
}
