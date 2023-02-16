using Microsoft.AspNetCore.Components.Forms;
using Perception.Models;
using System.Diagnostics;
using System.Threading.Channels;

namespace Perception.Services
{
    public class TaskQueue
    {
        private readonly string inputpath = Directory.GetCurrentDirectory() + $@"\wwwroot\input";
        private readonly string baseworkpath = Directory.GetCurrentDirectory() + $@"\wwwroot\work";
        private readonly string networkpath = Directory.GetCurrentDirectory() + @"\Neural\predict.py";
        private Channel<Task> Tasks { get; }
        private IServiceScopeFactory ScopeFactory { get; }
        public TaskQueue(IServiceScopeFactory scopeFactory)
        {
            Tasks = Channel.CreateUnbounded<Task>();
            ScopeFactory = scopeFactory;
        }
        public async Task QueueTaskAsync(Record record)
        {
            var task = PredictionPredict(record);
            await Tasks.Writer.WriteAsync(task);
        }

        public async Task<Task> DequeueAsync()
        {
            return await Tasks.Reader.ReadAsync();
        }

        private Task PredictionPredict(Record record)
        {
            return new Task(async () =>
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
                        Arguments = $@"{networkpath} predict {inputpath}\1.jpg"
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
                            //await context!.Results.AddAsync(result);
                            //await context.SaveChangesAsync();
                        }
                    }
                }
                else
                {
                    Console.WriteLine("error");
                    Console.WriteLine(p.StandardError.ReadToEnd());
                }
            });
        }
    }
}
