using Colorful;
using LiteDB;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using Console = Colorful.Console;

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
            var judgeName = config["judgeConfig"]["judgeName"].ToString();

            Welcome(judgeName);

            var rabbitServer = config["rabbitServer"].ToString();
            var rabbitUsername = config["rabbitUsername"].ToString();
            var rabbitPassword = config["rabbitPassword"].ToString();
            var rabbitPort = int.Parse(config["rabbitPort"].ToString());

            var webKey = config["webKey"].ToString();
            var webServer = config["webServer"].ToString();
            var webPort = int.Parse(config["webPort"].ToString());

            var judgeContext = JudgeContext.CreateFromJson(config["judgeConfig"].ToString());

            var client = new HttpClient();

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
                Console.WriteLine();
                Console.WriteLine($"Received judge request...", Color.Coral);

                // get judge task from the message queue
                var sourceCode = request.sourceCode;
                var language = request.language;
                var problemID = request.problemID;

                using (var db = new LiteDatabase(@"datahash.db"))
                {
                    var pairs = db.GetCollection<DatasetHash>("datasetHashPairs");

                    var pair = pairs.FindOne(s => s.ProblemID == problemID);
                    if (pair == null || pair.Hash != request.dataHash)
                    {
                        Console.WriteLine($"Updating obsolete or non-existing dataset {problemID}...",
                            Color.Coral);
                        var hash = DatasetHash.DownloadDataset(judgeContext,
                            problemID, webKey, webServer, webPort);
                        Console.WriteLine($"Updated dataset {hash}...", Color.Coral);
                        if (pair == null)
                        {
                            pairs.Insert(new DatasetHash
                            {
                                Hash = request.dataHash,
                                ProblemID = problemID
                            });
                        }
                        else
                        {
                            pair.Hash = request.dataHash;
                            pairs.Update(pair);
                        }
                    }
                    pairs.EnsureIndex(x => x.ProblemID);
                }

                var dataset = Dataset.CreateFromJson(Utility.GetDatasetConfigPath(judgeContext, problemID));

                var judgeCase = new JudgeCase(sourceCode, language, judgeContext, dataset);
                judgeCase.Judge();
                Request.SendResult(client, request, judgeCase, webKey, webServer, webPort);

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
