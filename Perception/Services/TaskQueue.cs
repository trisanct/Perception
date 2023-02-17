using Microsoft.AspNetCore.Components.Forms;
using Perception.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Channels;

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
        public TaskQueue(IServiceScopeFactory scopeFactory)
        {
            Tasks = Channel.CreateUnbounded<Task>();
            ScopeFactory = scopeFactory;
        }
        public async Task QueueTaskAsync(Record record, List<FileMap> files)
        {
            Task task;
            if (record.Mode == Record.RecordMode.Predict) task = PredictModeTask(record, files);
            else task = TestPredict(record, files);
            await Tasks.Writer.WriteAsync(task);
        }

        public async Task<Task> DequeueAsync()
        {
            return await Tasks.Reader.ReadAsync();
        }
        private async Task PredictModeTask(Record record, List<FileMap> files)
        {
            try
            {
                Console.WriteLine($"进入task{record.Id}");
                var workpath = $@"{baseworkpath}\{record.Id}";
                Directory.CreateDirectory(workpath);
                File.Copy($@"{inputpath}\{files[0].NodeId}{files[0].Node.Extension}", $@"{workpath}\{record.Id}{files[0].Node.Extension}", true);
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
                        Arguments = $@"{networkpath} predict {record.Id}{files[0].Node.Extension} {record.Id}_out{files[0].Node.Extension}"
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
                            var outstrings = p.StandardOutput.ReadToEnd().TrimEnd(new char[] { '\r', '\n' }).Split(',');
                            var pclass = outstrings[0].Replace(" ", "");
                            var score = outstrings[1];
                            Console.WriteLine($"class:{pclass} score:{score}");
                            var result = new Result()
                            {
                                Class = (Result.PredictedClass)Enum.Parse(typeof(Result.PredictedClass), pclass),
                                Score = float.Parse(score),
                                RecordId = record.Id
                            };
                            record.State = Record.RecordState.Completed;
                            context.Entry(record).Property("State").IsModified=true;
                            await context.Results.AddAsync(result);
                        }
                        await context.SaveChangesAsync();
                    }
                }
            }
            catch(Exception e) { Console.WriteLine(e.Message); }
        }
        private async Task Directory(Record record, List<FileMap> files)
        {

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
                            Class = (Result.PredictedClass)Enum.Parse(typeof(Result.PredictedClass), pclass),
                            Score = float.Parse(score),
                            RecordId = record.Id
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
