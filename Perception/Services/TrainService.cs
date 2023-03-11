using Microsoft.AspNetCore.Components.Forms;
using Perception.Data;
using Perception.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Perception.Services
{
    public class TrainService
    {
        private readonly string testinputpath = Directory.GetCurrentDirectory() + $@"\wwwroot\input";
        private readonly string inputpath = Directory.GetCurrentDirectory() + $@"\wwwroot\upload";
        private readonly string outputpath = Directory.GetCurrentDirectory() + $@"\wwwroot\output";
        private readonly string basedatasetpath = Directory.GetCurrentDirectory() + $@"\Neural\datasets";
        private readonly string trainpath = Directory.GetCurrentDirectory() + @"\Neural\train.py";
        private readonly string annotationpath = Directory.GetCurrentDirectory() + @"\Neural\voc_annotation.py";
        private Channel<Func<CancellationToken, Task>> TrainTasks { get; }
        private IServiceScopeFactory ScopeFactory { get; }
        private CancellationTokenSource singleTokenSource;
        private CancellationToken singleToken;
        private CancellationTokenSource? currentTokenSource;
        private CancellationToken currentToken;
        public TrainService(IServiceScopeFactory scopeFactory)
        {
            TrainTasks = Channel.CreateUnbounded<Func<CancellationToken, Task>>();
            ScopeFactory = scopeFactory;
            singleTokenSource = new CancellationTokenSource();
            singleToken = singleTokenSource.Token;
            Console.WriteLine("训练服务构造");
            _ = Task.Run(async () =>
            {
                Console.WriteLine("等待训练任务");
                while (!singleToken.IsCancellationRequested)
                {
                    currentTokenSource = new CancellationTokenSource();
                    currentToken = currentTokenSource.Token;
                    try
                    {
                        var t = await TrainTasks.Reader.ReadAsync();
                        await t(currentToken);
                    }
                    catch (Exception ex)
                    {
                        if (ex is TaskCanceledException)
                        {
                            Console.WriteLine("当前训练任务已被取消");
                        }
                        else
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                    finally
                    {
                        currentTokenSource.Dispose();
                    }
                }
            });
        }
        public async Task QueueTaskAsync(Dataset dataset)
        {
            await TrainTasks.Writer.WriteAsync(st => Traintask(dataset, st));
        }
        public void StopCurrent()
        {
            currentTokenSource?.Cancel();
        }
        public void Stop()
        {
            singleTokenSource.Cancel();
        }

        private async Task<Func<CancellationToken, Task>> DequeueAsync()
        {
            return await TrainTasks.Reader.ReadAsync();
        }
        private async Task Traintask(Dataset dataset, CancellationToken stoppingToken)
        {
            var datasetpath = $@"{basedatasetpath}\{dataset.Id}";
            var p1 = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WorkingDirectory = datasetpath,
                    FileName = "python",
                    Arguments = $@"{annotationpath}"
                }
            };
            p1.Start();
            await p1.WaitForExitAsync();
            if (p1.ExitCode != 0)
            {
                Console.WriteLine(await p1.StandardError.ReadToEndAsync());
                return;
            }
            Console.WriteLine("数据集分割完毕");
            var p2 = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WorkingDirectory = datasetpath,
                    FileName = "python",
                    Arguments = $@"{trainpath} {dataset.Epoch}"
                }
            };
            p2.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
            {
                Console.WriteLine(e.Data);
            });
            p2.Start();
            p2.BeginOutputReadLine();
            p2.PriorityClass = ProcessPriorityClass.RealTime;
            try
            {
                await p2.WaitForExitAsync(stoppingToken);
                if (p2.ExitCode != 0)
                {
                    Console.WriteLine(p2.StandardError.ReadToEnd());
                }
            }
            catch (TaskCanceledException ex)
            {
                p2.Kill(true);
                Console.WriteLine(ex.Message);
            }
            finally
            {
                p2.Close();
                p2.Dispose();
            }
        }
    }
}
