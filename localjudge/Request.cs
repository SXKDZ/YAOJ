using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using Console = Colorful.Console;

namespace localjudge
{
    public class Request
    {
        public int userID;
        public int recordID;
        public string problemID;
        public string language;
        public string sourceCode;
        public string dataHash;

        public static async void SendResult(HttpClient client, Request judgeRequest, JudgeCase judgeCase,
            string webKey, string webServer, int webPort)
        {
            var payload = new Dictionary<string, string>
            {
                { "text", judgeCase.judgeText },
                { "status", judgeCase.finalJudgeStatus.ToString() },
                { "usedTime", judgeCase.totalUsedTime.ToString() },
                { "usedMemory", judgeCase.totalUsedMemory.ToString() },
                { "key", webKey }
            };
            var content = new FormUrlEncodedContent(payload);

            var url = $"http://{webServer}:{webPort}/Records/Update/{judgeRequest.recordID}";
            var response = await client.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();

            Console.Write($"Report results...", Color.Coral);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                Console.WriteLine("OK!", Color.Coral);
            }
            else
            {
                Console.WriteLine("Failed!", Color.Coral);
                Console.WriteLine("Response:");
                Console.WriteLine(responseString.Substring(0, 200));
            }
        }
    }
}
