using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Console = Colorful.Console;

namespace localjudge
{
    public class CompilerConfig
    {
        public string execution;
        public string parameter;
        public string language;
        public string extension;
        public uint time;
        public uint memory;
    }
    
    public class JudgeContext
    {
        public Dictionary<string, CompilerConfig> compilers = new Dictionary<string, CompilerConfig>();
        public string workingDirectory;
        public string dataDirectory;
        public string tempDirectory;
        public int parallelJudge;
        public string judgeName;
        public string secureUser;
        public string securePassword;

        public static JudgeContext CreateFromJson(string json)
        {
            var jc = new JudgeContext();

            var config = JObject.Parse(json);
            jc.judgeName = config["judgeName"].ToString();
            jc.dataDirectory = config["dataDirectory"].ToString();
            jc.tempDirectory = config["tempDirectory"].ToString();
            jc.workingDirectory = config["workingDirectory"].ToString();
            jc.parallelJudge = int.Parse(config["parallelJudge"].ToString());
            jc.secureUser = config["secureUser"].ToString();
            jc.securePassword = config["securePassword"].ToString();

            CompilerConfig[] pcompilers;
            pcompilers = JsonConvert.DeserializeObject<CompilerConfig[]>(config["compilers"].ToString());
            foreach (var c in pcompilers)
            {
                jc.compilers[c.language] = c;
            }

            return jc;
        }
    }

    public class JudgeStatusColorAttribute : Attribute
    {
        internal JudgeStatusColorAttribute(string c) => color = c;
        private string color;
        public Color Color { get => Color.FromName(color); }
    }

    public enum JudgeStatus
    {
        [Description("Time Limit Exceeded"), JudgeStatusColor("Blue")]
        TLE,
        [Description("Memory Limit Exceeded"), JudgeStatusColor("Yellow")]
        MLE,
        [Description("Runtime Error"), JudgeStatusColor("Gold")]
        RE,
        [Description("Compilation Error"), JudgeStatusColor("Red")]
        CE,
        [Description("Accepted"), JudgeStatusColor("Green")]
        AC,
        [Description("Wrong Answer"), JudgeStatusColor("Red")]
        WA,
        [Description("Not Available"), JudgeStatusColor("DimGray")]
        NA
    };

    public class JudgeResult
    {
        public JudgeStatus judgeStatus;
        public double usedTime;
        public double usedMemory;
    }

    public class JudgeCase
    {
        public string sourceCode;
        public string language;
        public JudgeContext judgeContext;
        public Dataset dataset;
        public JudgeStatus finalJudgeStatus = JudgeStatus.NA;
        public JudgeResult[] judgeResults;
        public double totalUsedTime = 0;
        public double totalUsedMemory = 0;
        public string judgeText;

        public int DatasetLength { get => dataset.data.Length; }

        public JudgeCase(string sourceCode, string language, JudgeContext judgeContext, Dataset dataset) {
            this.sourceCode = sourceCode;
            this.language = language;
            this.judgeContext = judgeContext;
            this.dataset = dataset;
        }

        public string ReportFinalResult()
        {
            var sb = new StringBuilder();
            sb.AppendLine(new string('=', 45));
            sb.AppendLine($"LocalJudge v{Assembly.GetEntryAssembly().GetName().Version}");
            sb.AppendLine($"Judge: {judgeContext.judgeName}");
            sb.AppendLine(new string('=', 45));

            if (finalJudgeStatus != JudgeStatus.CE)
            {
                for (var i = 0; i < DatasetLength; ++i)
                {
                    Console.Write($"[{i}] ", Color.Gray);
                    Console.Write(Utility.GetAttribute<DescriptionAttribute>(judgeResults[i].judgeStatus).Description,
                        Utility.GetAttribute<JudgeStatusColorAttribute>(judgeResults[i].judgeStatus).Color);

                    Console.WriteLine($" ({judgeResults[i].usedTime:F2}s, {judgeResults[i].usedMemory:F2}MB)");

                    sb.AppendLine($"[{i}] " +
                        $"{Utility.GetAttribute<DescriptionAttribute>(judgeResults[i].judgeStatus).Description}" +
                        $" ({judgeResults[i].usedTime:F2}s, {judgeResults[i].usedMemory:F2}MB)");
                }

                Console.Write("[Result] ");
                Console.Write(Utility.GetAttribute<DescriptionAttribute>(finalJudgeStatus).Description,
                    Utility.GetAttribute<JudgeStatusColorAttribute>(finalJudgeStatus).Color);
                Console.WriteLine($" ({totalUsedTime:F2}s, {totalUsedMemory:F2}MB)");
            }

            sb.AppendLine(new string('=', 45));
            sb.AppendLine("Result: " +
                $"{Utility.GetAttribute<DescriptionAttribute>(finalJudgeStatus).Description}" +
                $" ({totalUsedTime:F2}s, {totalUsedMemory:F2}MB)");
            sb.AppendLine(new string('=', 45));
            return sb.ToString();
        }

        private bool Compile(string exePath)
        {
            var sourceFilename = $"{Utility.GetRandomString(8)}.{judgeContext.compilers[language].extension}";
            var sourcePath = Path.Combine(judgeContext.tempDirectory, sourceFilename);

            if (!judgeContext.compilers.ContainsKey(language))
            {
                Console.WriteLine("Compiler does not exist!", Color.Red);
                return false;
            }
            var compiler = judgeContext.compilers[language];

            File.WriteAllText(sourcePath, sourceCode);

            var result = new WinRunnerResult();
            var compilerParameter = compiler.parameter.Replace("{src}", sourcePath).Replace("{out}", exePath);
            var compilerCommand = $"{compiler.execution} {compilerParameter}";

            var compilerOutputFilename = $"{Utility.GetRandomString(8)}.txt";
            var compilerOutputPath = Path.Combine(judgeContext.tempDirectory, compilerOutputFilename);

            Console.WriteLine($"Compiling {sourceFilename}...", Color.Coral);
            WinRunner.StartCompiler(result, compilerCommand, compilerOutputPath, compiler.time, compiler.memory);

            File.Delete(sourcePath);

            if (result.status != WinRunnerStatus.OK)
            {
                Console.WriteLine($"Compilation failed ({result.usedTime:F2}s, {result.usedMemory}MB)...",
                    Color.Red);
                
                if (result.status != WinRunnerStatus.TLE)
                {
                    Thread.Sleep(50);
                    var errorMessage = string.Join("\r\n", File.ReadLines(compilerOutputPath).Take(10));

                    Console.WriteLine("Compiler's output (first 10 lines):");
                    Console.WriteLine(errorMessage);
                }
                else
                {
                    Console.WriteLine($"{compiler.language} compiler exceeded the time limit");
                }

                finalJudgeStatus = JudgeStatus.CE;
                judgeText = ReportFinalResult();
                return false;
            }
            Console.WriteLine($"Compilation succeeded ({result.usedTime:F2}s, {result.usedMemory}MB)...",
                Color.Green);

            Thread.Sleep(50);
            File.Delete(compilerOutputPath);
            return true;
        }

        private void Execute(int i, string datasetBasePath, string exePath)
        {
            judgeResults[i] = new JudgeResult();

            var result = new WinRunnerResult();
            var userOutputFilename = $"{Utility.GetRandomString(8)}.txt";
            var userOutputPath = Path.Combine(judgeContext.tempDirectory, userOutputFilename);
            var standardInputPath = Path.Combine(datasetBasePath, dataset.data[i].input);
            var standardOutputPath = Path.Combine(datasetBasePath, dataset.data[i].output);
            
            WinRunner.StartProcess(result, exePath,
                judgeContext.secureUser, judgeContext.securePassword,
                standardInputPath, userOutputPath,
                dataset.data[i].time, dataset.data[i].memory, 0);
            if (result.status != WinRunnerStatus.OK)
            {
                // convert from JudgeStatus to WinRunnerStatus
                judgeResults[i].judgeStatus = (JudgeStatus)((int)result.status - 1);
            }
            else
            {
                var stdOutputFile = new FileInfo(standardOutputPath);
                var usrOutputFile = new FileInfo(userOutputPath);

                Thread.Sleep(50);
                if (Comparer.Comparer.FilesAreEqual(stdOutputFile, usrOutputFile))
                {
                    judgeResults[i].judgeStatus = JudgeStatus.AC;
                }
                else
                {
                    judgeResults[i].judgeStatus = JudgeStatus.WA;
                }
            }

            judgeResults[i].usedMemory = result.usedMemory;
            judgeResults[i].usedTime = result.usedTime;

            Thread.Sleep(50);
            File.Delete(userOutputPath);
        }

        public void Judge()
        {
            Console.WriteLine($"Judging {dataset.problem}...", Color.Coral);

            var exeFilename = $"{Utility.GetRandomString(8)}.exe";
            var exePath = Path.Combine(judgeContext.tempDirectory, exeFilename);
            if (!Compile(exePath))
            {
                return;
            }

            var datasetBasePath = Path.Combine(judgeContext.dataDirectory, dataset.problem);
            judgeResults = new JudgeResult[DatasetLength];

            Console.WriteLine($"Running {exeFilename}...", Color.Coral);
            var po = new ParallelOptions
            {
                MaxDegreeOfParallelism = judgeContext.parallelJudge
            };
            Parallel.For(0, DatasetLength, po, i => { Execute(i, datasetBasePath, exePath); });
            File.Delete(exePath);

            totalUsedMemory = judgeResults.Select(i => i.usedMemory).Sum();
            totalUsedTime = judgeResults.Select(i => i.usedTime).Sum();
            if (judgeResults.Select(i => i.judgeStatus).All(i => i == JudgeStatus.AC))
            {
                finalJudgeStatus = JudgeStatus.AC;
            }
            else
            {
                var candidate = judgeResults.Select(i => i.judgeStatus).Where(i => i != JudgeStatus.AC);
                finalJudgeStatus = Utility.MostCommon(candidate);
            }
            judgeText = ReportFinalResult();
        }
    }
}
