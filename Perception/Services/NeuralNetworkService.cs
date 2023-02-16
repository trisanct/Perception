using Microsoft.AspNetCore.Components.Forms;
using Perception.Models;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Perception.Services
{
 
    public class NeuralNetworkService : BackgroundService
    {
        private readonly SemaphoreSlim semaphore=new SemaphoreSlim(4,4);
        private TaskQueue Tasks { get; }
        public NeuralNetworkService(TaskQueue tasks)
        {
            Tasks = tasks;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();
            while (!stoppingToken.IsCancellationRequested)
            {
                await semaphore.WaitAsync(stoppingToken);
                _ = Task.Run(async () => 
                {
                    Console.WriteLine(semaphore.CurrentCount);
                    var t = await Tasks.DequeueAsync();
                    t.Start();
                    await t.WaitAsync(stoppingToken);
                    semaphore.Release();
                });
            }
        }


        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("Queued Hosted Service is stopping.");

            await base.StopAsync(stoppingToken);
        }
    }
}
