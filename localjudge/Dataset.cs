using Newtonsoft.Json;
using System.IO;

namespace localjudge
{
    class DataConfig
    {
        public string input;
        public string output;
        public uint time;
        public uint memory;
        public double score;
    }

    class Dataset
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
}
