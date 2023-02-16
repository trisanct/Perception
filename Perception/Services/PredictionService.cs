using Microsoft.EntityFrameworkCore;
using Perception.Models;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace Perception.Services
{
    public class PredictionService: IDisposable
    {
        private Task Main { get; }
        private Queue<Task> tasks;
        private readonly string inputpath;
        private readonly string baseworkpath;
        private readonly string networkpath;
        //private CancellationTokenSource TokenSource { get; }
        //private CancellationToken Token { get; }
        private IServiceScopeFactory ScopeFactory { get; }
        public PredictionService(IServiceScopeFactory scopeFactory)
        {
            ScopeFactory = scopeFactory;
            inputpath = Directory.GetCurrentDirectory() + $@"\wwwroot\input";
            baseworkpath = Directory.GetCurrentDirectory() + $@"\wwwroot\work";
            networkpath = Directory.GetCurrentDirectory() + @"\Neural\predict.py";
            tasks = new Queue<Task>();
            //TokenSource = new CancellationTokenSource();
            //Token=TokenSource.Token;
            Main = Task.Run(() =>
            {
                while (true/*!Token.IsCancellationRequested*/)
                {
                    if (tasks.Count != 0)
                    {
                        var t = tasks.Dequeue();
                        t.Start();
                    }
                }
                //Token.ThrowIfCancellationRequested();
            }/*,Token*/);
        }
        public void QueueTask(int id)
        {
            tasks.Enqueue(new Task(async () =>
            {
                Console.WriteLine($"进入task{id}");
                var workpath =  $@"{baseworkpath}\{id}";
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
                Console.WriteLine($"运行python程序{id}");
                await p.WaitForExitAsync();
                Console.WriteLine($"python程序退出{id}");
                if (p.ExitCode == 0)
                {
                    var outstrings = p.StandardOutput.ReadToEnd().TrimEnd(new char[]{ '\r','\n'}).Split(',');
                    var pclass = outstrings[0].Replace(" ","");
                    var score = outstrings[1];
                    using(var scope = ScopeFactory.CreateScope())
                    {
                        using (var context = scope.ServiceProvider.GetService<PerceptionContext>())
                        {
                            //var record = await context!.Records.Where(r => r.Id == id).FirstOrDefaultAsync();
                            var result = new Result()
                            {
                                Class = (Result.PredictedClass)Enum.Parse(typeof(Result.PredictedClass), pclass),
                                Score = float.Parse(score),
                                RecordId = id
                            };
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
            }));
        }
        public void Dispose()
        {
            Main.Dispose();
            //tasks.Clear();
        }
    }
}
