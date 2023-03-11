using System.Diagnostics;

namespace Perception.Services
{
    public class NeuralNetworkService : BackgroundService
    {
        private readonly SemaphoreSlim predictsemaphore = new SemaphoreSlim(4, 4);
        private PredictTaskQueue Tasks { get; }
        private TrainService trainService;
        public NeuralNetworkService(PredictTaskQueue tasks, TrainService trainService)
        {
            Tasks = tasks;
            this.trainService = trainService;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine(Thread.CurrentThread.ManagedThreadId);
            //stoppingToken.ThrowIfCancellationRequested();
            while (!stoppingToken.IsCancellationRequested)
            {
                Console.WriteLine(Tasks.WaitingCount());
                await predictsemaphore.WaitAsync(stoppingToken);
                _ = Task.Run(async () => 
                {
                    Console.WriteLine("等待预测任务");
                    var t = await Tasks.DequeuePredictTaskAsync();
                    Console.WriteLine("开始预测任务");
                    await t(stoppingToken);
                    predictsemaphore.Release();
                    Console.WriteLine("预测任务完成");
                }, stoppingToken);
            }
            Console.WriteLine("退出死循环");
        }


        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("Queued Hosted Service is stopping.");
            trainService.Stop();
            //var ps = Process.GetProcessesByName("python");
            //foreach (var p in ps) { p.Kill(); }
            await base.StopAsync(stoppingToken);
        }
    }
}
