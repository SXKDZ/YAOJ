using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.IO.Compression;
using hasher;

namespace localjudge
{
    public class DataConfig
    {
        public string input;
        public string output;
        public uint time;
        public uint memory;
        public double score;
    }

    public class Dataset
    {
        public string problem;
        public DataConfig[] data;

        public static Dataset CreateFromJson(string path)
        {
            Dataset dataset;

            using (var sr = new StreamReader(path))
            {
                var configContent = sr.ReadToEnd();
                dataset = JsonConvert.DeserializeObject<Dataset>(configContent);
            }
            return dataset;
        }
    }

    public class DatasetHash
    {
        public int ID { get; set; }
        public string ProblemID { get; set; }
        public string Hash { get; set; }

        public static string DownloadDataset(JudgeContext judgeContext,
            string problemID, string webKey, string webServer, int webPort)
        {
            var url = $"http://{webServer}:{webPort}/Problems/DownloadDataset/{problemID}?key={webKey}";
            var fileName = $"{Utility.GetRandomString(8)}.zip";
            var filePath = Path.Combine(judgeContext.tempDirectory, fileName);
            using (var wc = new WebClient())
            {
                wc.DownloadFile(url, filePath);
            }
            Md5Hash md5Hash=new Md5Hash();
            var hash = md5Hash.GetMd5Hash(File.ReadAllBytes(filePath));

            UnzipDataset(judgeContext, problemID, filePath);
            return hash;
        }

        public static void UnzipDataset(JudgeContext judgeContext,
            string problemID, string filePath)
        {
            var dataDirectory = Path.Combine(judgeContext.dataDirectory, problemID);
            if (Directory.Exists(dataDirectory))
            {
                Directory.Delete(dataDirectory, recursive: true);
            }
            ZipFile.ExtractToDirectory(filePath, dataDirectory);
            File.Delete(filePath);
        }
    }
}
