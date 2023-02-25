using Microsoft.AspNetCore.Components.Forms;
using Perception.Data;
using Perception.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Channels;
using System.Text.Json;

namespace Perception.Services
{
    public class TaskQueue
    {
        private readonly string testinputpath = Directory.GetCurrentDirectory() + $@"\wwwroot\input";
        private readonly string inputpath = Directory.GetCurrentDirectory() + $@"\wwwroot\upload";
        private readonly string outputpath = Directory.GetCurrentDirectory() + $@"\wwwroot\output";
        private readonly string baseworkpath = Directory.GetCurrentDirectory() + $@"\wwwroot\work";
        private readonly string networkpath = Directory.GetCurrentDirectory() + @"\Neural\predict.py";
        private Channel<Task> Tasks { get; }
        private IServiceScopeFactory ScopeFactory { get; }
        private class PythonResult
        {
            public string Class { get; set; }
            public float Score { get; set; }
        }
        private class PythonResults
        {
            public string Name { get; set; }
            public List<PythonResult> Results { get; set; }
        }
        public TaskQueue(IServiceScopeFactory scopeFactory)
        {
            Tasks = Channel.CreateUnbounded<Task>();
            ScopeFactory = scopeFactory;
        }
        public int WaitingCount()
        {
            return Tasks.Reader.Count;
        }
        public async Task QueueTaskAsync(Record record)
        {
            Task task;
            if (record.Mode == Record.RecordMode.Predict) task = PredictModeTask(record);
            else if (record.Mode == Record.RecordMode.Directory) task = DirectoryModeTask(record);
            else task = TestPredict(record, record.Files);
            await Tasks.Writer.WriteAsync(task);
        }

        public async Task<Task> DequeueAsync()
        {
            return await Tasks.Reader.ReadAsync();
        }
        private async Task PredictModeTask(Record record)
        {
            try
            {
                Console.WriteLine($"进入task{record.Id}");
                var workpath = $@"{baseworkpath}\{record.Id}";
                Directory.CreateDirectory(workpath);
                File.Copy($@"{inputpath}\{record.Files[0].NodeId}{record.Files[0].Node.Extension}", $@"{workpath}\{record.Files[0].Id}{record.Files[0].Node.Extension}", true);
                var p = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = false,
                        UseShellExecute = false,
                        WorkingDirectory = workpath,
                        FileName = "python",
                        Arguments = $@"{networkpath} predict {record.Files[0].Id}{record.Files[0].Node.Extension} {record.Files[0].Id}_out{record.Files[0].Node.Extension}"
                    }
                };
                p.Start();
                //Console.WriteLine($"运行python程序{record.Id}");
                await p.WaitForExitAsync();
                //Console.WriteLine($"python程序退出{record.Id}");
                using (var scope = ScopeFactory.CreateScope())
                {
                    using (var context = scope.ServiceProvider.GetService<PerceptionContext>())
                    {
                        if (context == null) throw new Exception("未知错误");

                        if (p.ExitCode != 0)
                        {
                            record.State = Record.RecordState.Error;
                            Console.WriteLine(p.StandardError.ReadToEnd());
                        }
                        else
                        {
                            var pso = p.StandardOutput.ReadToEnd();
                            var pythonresults = JsonSerializer.Deserialize<PythonResults>(pso);
                            if (pythonresults != null)
                            {
                                foreach (var pythonresult in pythonresults.Results)
                                {
                                    var result = new Result()
                                    {
                                        Class = pythonresult.Class,
                                        Score = pythonresult.Score,
                                        FileId = record.Files[0].Id
                                    };
                                    await context.Results.AddAsync(result);
                                }
                                record.State = Record.RecordState.Completed;
                            }
                            else record.State = Record.RecordState.Error;
                        }
                        context.Entry(record).Property("State").IsModified = true;
                        await context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception e) { Console.WriteLine(e.Message); }
        }
        private async Task DirectoryModeTask(Record record)
        {
            try
            {
                Console.WriteLine($"进入task{record.Id}");
                var workpath = $@"{baseworkpath}\{record.Id}";
                Directory.CreateDirectory(workpath);
                foreach (var file in record.Files)
                {
                    File.Copy($@"{inputpath}\{file.NodeId}{file.Node.Extension}", $@"{workpath}\{file.Id}{file.Node.Extension}", true);
                }
                var p = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = false,
                        UseShellExecute = false,
                        WorkingDirectory = workpath,
                        FileName = "python",
                        Arguments = $@"{networkpath} directory ./ ./out"
                    }
                };
                p.Start();
                Console.WriteLine($"运行python程序{record.Id}");
                await p.WaitForExitAsync();
                Console.WriteLine($"python程序退出{record.Id}");
                using (var scope = ScopeFactory.CreateScope())
                {
                    using (var context = scope.ServiceProvider.GetService<PerceptionContext>())
                    {
                        if (context == null) throw new Exception("未知错误");

                        if (p.ExitCode != 0) record.State = Record.RecordState.Error;
                        else
                        {
                            var rows = p.StandardOutput.ReadToEnd().TrimEnd(new char[] { '\r', '\n' }).Split("\r\n");
                            foreach (var row in rows)
                            {
                                var pythonresults = JsonSerializer.Deserialize<PythonResults>(row);
                                if (pythonresults != null)
                                {
                                    foreach (var pythonresult in pythonresults.Results)
                                    {
                                        var result = new Result()
                                        {
                                            Class = pythonresult.Class,
                                            Score = pythonresult.Score,
                                            FileId = int.Parse(pythonresults.Name.Substring(0, pythonresults.Name.LastIndexOf('.')))
                                        };
                                        await context.Results.AddAsync(result);
                                    }
                                }
                            }
                            record.State = Record.RecordState.Completed;
                            context.Entry(record).Property("State").IsModified = true;
                        }
                        await context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception e) { Console.WriteLine(e.Message); }
        }
        private async Task TestPredict(Record record, List<FileMap> files)
        {
            Console.WriteLine($"进入task{record.Id}");
            var workpath = $@"{baseworkpath}\{record.Id}";
            Directory.CreateDirectory(workpath);
            var p = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = false,
                    UseShellExecute = false,
                    WorkingDirectory = workpath,
                    FileName = "python",
                    Arguments = $@"{networkpath} predict {testinputpath}\{record.Id}.jpg {record.Id}_out.jpg"
                }
            };
            p.Start();
            Console.WriteLine($"运行python程序{record.Id}");
            await p.WaitForExitAsync();
            Console.WriteLine($"python程序退出{record.Id}");
            if (p.ExitCode == 0)
            {
                var outstrings = p.StandardOutput.ReadToEnd().TrimEnd(new char[] { '\r', '\n' }).Split(',');
                var pclass = outstrings[0].Replace(" ", "");
                var score = outstrings[1];
                using (var scope = ScopeFactory.CreateScope())
                {
                    using (var context = scope.ServiceProvider.GetService<PerceptionContext>())
                    {
                        //var record = await context!.Records.Where(r => r.Id == id).FirstOrDefaultAsync();
                        var result = new Result()
                        {
                            Class = pclass,
                            Score = float.Parse(score),
                            FileId = record.Files[0].Id
                        };
                        Console.WriteLine($"class:{result.Class} score:{result.Score}");
                        await context!.Results.AddAsync(result);
                        await context.SaveChangesAsync();
                    }
                }
            }
            else
            {
                Console.WriteLine("error");
                Console.WriteLine(p.StandardError.ReadToEnd());
            }
        }

    }
}
